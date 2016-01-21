using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

using Accela.Web.SDK;
using Accela.Web.SDK.Models;

using Newtonsoft.Json;
using OpenPermit;
using RestSharp;

using AccelaSDKModels = Accela.Web.SDK.Models;

namespace OpenPermit.Accela
{
    public class AccelaOpenPermitAdapter : IOpenPermitAdapter
    {
        private static string scope = "records get_user_profile inspections settings documents";
        private static IConfigurationProvider appConfig = new AppConfigurationProvider();
        private readonly string agencyAppId;
        private readonly string agencyAppSecret;
        
        // TODO move token management to separate class and handle expiration?
        private AccessToken backgroundToken;
        private OpenPermitContext context;

        // TODO create new object to put Accela handler into lazy factory or bypass accela sdk altogether and create our own class
        private RecordHandler recApi;
        private InspectionHandler inspectionApi;
        private DocumentHandler documentApi;

        private Dictionary<string, List<string>> lists = new Dictionary<string, List<string>>();

        private AgencyConfiguration config;
        private NameValueCollection connection;

        public AccelaOpenPermitAdapter(OpenPermitContext context)
        {
            this.agencyAppId = ConfigurationManager.AppSettings["OP.Accela.App.Id"];
            this.agencyAppSecret = ConfigurationManager.AppSettings["OP.Accela.App.Secret"];
            if (this.agencyAppId == null || this.agencyAppSecret == null)
            {
                throw new ConfigurationErrorsException("OP.Accela.App.Id and OP.Accela.App.Secret are required configuration settings.");
            }

            this.context = context;

            // TODO see if it makes sense to provide a default config, otherwise make sure the AccelaConfig is provided
            this.config = JsonConvert.DeserializeObject<AgencyConfiguration>(context.Agency.Configuration);
                
            // TODO using agency app only, should we change this to citizen app?
            this.recApi = new RecordHandler(this.agencyAppId, this.agencyAppSecret, ApplicationType.Agency, string.Empty, appConfig);
            this.inspectionApi = new InspectionHandler(this.agencyAppId, this.agencyAppSecret, ApplicationType.Agency, string.Empty, appConfig);
            this.documentApi = new DocumentHandler(this.agencyAppId, this.agencyAppSecret, ApplicationType.Agency, string.Empty, appConfig);

            this.connection = HttpUtility.ParseQueryString(context.Agency.ConnectionString);
            this.recApi.AgencyId = this.connection["id"];
            this.recApi.Environment = this.connection["env"];
            this.inspectionApi.AgencyId = this.connection["id"];
            this.inspectionApi.Environment = this.connection["env"];
            this.documentApi.AgencyId = this.connection["id"];
            this.documentApi.Environment = this.connection["env"];
        }

        private string BackgroundToken
        {
            get
            {
                // TODO cache these agency tokens and handle expiration?
                if (this.backgroundToken == null)
                {
                    this.backgroundToken = this.RefreshBackgroundToken();

                    if (this.backgroundToken == null)
                    {
                        throw new Exception("Unable to refresh background token.");
                    }
                }

                return this.backgroundToken.Token;
            }
        }

        private AccessToken RefreshBackgroundToken()
        {
            // TODO cache these agency tokens and handle expiration
            var civicId = new CivicIDOAuthClient(this.agencyAppId, this.agencyAppSecret, scope, this.connection["env"]);
            civicId.AgencyName = this.connection["id"];

            // TODO handle token retrive error
            return civicId.QueryAccessToken(this.connection["u"], this.connection["p"]);
        }

        private string AccelaIdFromLocalId(string localId)
        {
            int index = localId.IndexOf('-');
            return localId.Substring(index + 1);
        }

        private string AccelaIdToLocalId(string accelaId)
        {
            return this.context.Agency.Id + "-" + accelaId;
        }

        private void ParseAttachmentId(string id, out string documentId)
        {
            // ID format = AGENCY-DOCUMENT
            int agencyIdIndex = id.IndexOf('-');
            documentId = id.Substring(agencyIdIndex + 1);
        }

        private Record GetRecord(string recordCustomId)
        {
            var recordFilter = new RecordFilter
            {
                customId = recordCustomId
            };
            ResultDataPaged<Record> page = this.recApi.SearchRecords(null, recordFilter, null, -1, -1, null, null, "addresses");
            return page.Data.First();
        }

        private string GetRecordIdByNumber(string number)
        {
            // TODO cache these record ids to avoid call to backend
            Record record = this.GetRecord(number);
            return record.id;
        }

        #region Permits

        public List<Permit> SearchPermits(PermitFilter filter)
        {
            var recordFilter = new RecordFilter();

            if (filter.PermitNumber != null)
            {
                recordFilter.customId = filter.PermitNumber;
            }
            else if (filter.Address != null)
            {
                Address address = new Address();
                UsAddress parsedAddress = this.ParseAddress(filter.Address);
                int strNo;
                int.TryParse(parsedAddress.AddressNumber, out strNo);
                address.streetStart = strNo;
                address.streetName = parsedAddress.StreetName;
                
                // address.xCoordinate = -82.458328247070312;
                // address.yCoordinate = 27.938003540039063;
                recordFilter.address = address;
            }

            ResultDataPaged<Record> page = this.recApi.SearchRecords(null, recordFilter, null, 0, 1000, null, null, "addresses");

            var result = new List<Permit>();
            if (page.Data != null)
            {
                foreach (var record in page.Data)
                {
                    Permit permit = this.ToPermit(record);
                    result.Add(permit);
                }
            }

            return result;
        }

        public Permit GetPermit(string permitNumber)
        {
            if (permitNumber == null)
            {
                throw new ArgumentNullException("Permit number is required.");
            }

            Record record = this.GetRecord(permitNumber);
            return this.ToPermit(record);
        }

        private PermitType ToPermitType(RecordType recordType)
        {
            return new PermitType
            {
                Id = this.AccelaIdToLocalId(recordType.id),
                Name = recordType.text,
                Agency = new Agency
                {
                    Name = this.context.Agency.Name,
                    Id = this.context.Agency.Id
                }
            };
        }

        private Permit ToPermit(Record record)
        {
            Permit permit = new Permit
            {
                PermitNum = record.customId,
                Fee = record.totalFee,
                ProjectName = record.name,
                EstProjectCost = record.estimatedTotalJobCost,

                Jurisdiction = this.context.Agency.Id,
                Publisher = this.context.Agency.Name
            };

            if (record.type != null)
            {
                permit.PermitType = record.type.group;
                permit.PermitTypeDesc = record.type.text;
                permit.PermitClass = record.type.type;
                permit.WorkClass = record.type.subType;
            }

            if (record.completeDate != null)
            {
                permit.CompletedDate = DateTime.Parse(record.completedDate);
            }

            if (record.status != null)
            {
                permit.StatusCurrent = record.status.text;

                if (this.config.Status != null)
                {
                    var mapping = this.config.Status.SingleOrDefault<StatusMapping>(m => m.Status == record.status.text);
                    if (mapping.Status != null)
                    {
                        permit.StatusCurrentMapped = mapping.StatusMapped;
                    }
                }
            }

            if (record.addresses != null)
            {
                Address address = record.addresses.FirstOrDefault<Address>();

                if (address != null)
                {
                    StringBuilder originalBuilder = new StringBuilder();
                    if (address.streetStart != 0)
                    {
                        originalBuilder.Append(address.streetStart);
                    }

                    if (address.streetPrefix != null)
                    {
                        originalBuilder.Append(" ");
                        originalBuilder.Append(address.streetPrefix);
                    }

                    if (address.streetName != null)
                    {
                        originalBuilder.Append(" ");
                        originalBuilder.Append(address.streetName);
                    }

                    if (address.streetSuffix != null)
                    {
                        originalBuilder.Append(" ");
                        originalBuilder.Append(address.streetSuffix.text);
                    }

                    permit.OriginalAddress1 = originalBuilder.ToString();
                    permit.OriginalCity = address.city;
                    permit.OriginalState = (address.state != null) ? address.state.text : string.Empty;
                    permit.OriginalZip = address.postalCode;
                }
            }

            return permit;
        }

        private UsAddress ParseAddress(string address)
        {
            string addressUrl = ConfigurationManager.AppSettings["OP.Accela.UsAddress.Url"];
            RestClient client = new RestClient();
            client.BaseUrl = new Uri(addressUrl);

            RestRequest request = new RestRequest();
            request.RequestFormat = DataFormat.Json;
            request.AddBody(new { address = address });
            request.Method = Method.POST;

            IRestResponse response = client.Execute(request);
            if (response.StatusCode != HttpStatusCode.Created)
            {
                return null;
            }

            Dictionary<string, UsAddress> addResults = JsonConvert.DeserializeObject<Dictionary<string, UsAddress>>(response.Content);
            return addResults["address"];
        }

        private List<PermitStatus> GetPermitTimelineInternal(string permitNumber, string internalId)
        {
            var timeline = new List<PermitStatus>();
            List<WorkflowTask> tasks = this.recApi.GetWorkflowTasks(internalId, this.BackgroundToken);

            if (tasks != null)
            {
                foreach (var task in tasks)
                {
                    if (task.isActive == "Y" || task.isCompleted == "Y")
                    {
                        var status = new PermitStatus
                        {
                            PermitNum = permitNumber,
                            StatusPrevious = task.description
                        };

                        if (task.status != null)
                        {
                            status.Comments = task.status.text;
                        }

                        if (task.statusDate != null)
                        {
                            status.StatusPreviousDate = DateTime.Parse(task.statusDate);
                        }

                        var mapping = this.config.Timeline.FirstOrDefault<TimelineMapping>(t => t.StatusPrevious == task.description);
                        status.StatusPreviousMapped = mapping.StatusPreviousMapped;

                        timeline.Add(status);
                    }
                }
            }
            else
            {
                var status = new PermitStatus
                {
                    PermitNum = permitNumber,
                    StatusPrevious = "Application Submitted"
                };
            }

            return timeline;
        }

        public List<PermitStatus> GetPermitTimeline(string permitNumber)
        {
            if (permitNumber == null)
            {
                throw new ArgumentNullException("Permit number is required.");
            }

            Record record = this.GetRecord(permitNumber);
            if (record.status != null)
            {
                return this.GetPermitTimelineInternal(permitNumber, record.id);
            }
            else
            {
                return new List<PermitStatus>();
            }
        }

        #endregion

        #region Inspections
        public List<Inspection> GetInspections(string permitNumber)
        {
            if (permitNumber == null)
            {
                throw new ArgumentNullException("Permit number is required.");
            }
            
            string id = this.GetRecordIdByNumber(permitNumber);
            return this.GetPermitInspectionsInternal(id);
        }

        private Inspection ToInspection(AccelaSDKModels.Inspection inspectionRecord)
        {
            var inspection = new Inspection
            {
                Id = inspectionRecord.id.ToString(),
                Inspector = inspectionRecord.inspectorFullName,
                Result = inspectionRecord.status.text,
                InspType = inspectionRecord.type.text,
                PermitNum = inspectionRecord.recordId.customId,
                
                // TODO verify this
                ScheduledDate = inspectionRecord.scheduledDate,
                DesiredDate = inspectionRecord.scheduleDate,
                InspectionNotes = inspectionRecord.resultComment,
                RequestDate = inspectionRecord.requestDate,
                InspectedDate = inspectionRecord.completedDate,
            };

            if (this.config.InspectionType != null)
            {
                var mapping = this.config.InspectionType.SingleOrDefault<InspectionTypeMapping>(m => m.InspectionType == inspection.InspType);
                inspection.InspTypeMapped = mapping.InspectionTypeMapped;
            }

            if (this.config.InspectionResult != null)
            {
                var mapping = this.config.InspectionResult.SingleOrDefault<InspectionResultMapping>(m => m.Result == inspection.Result);
                inspection.ResultMapped = mapping.ResultMapped;
            }

            return inspection;
        }

        private List<Inspection> GetPermitInspectionsInternal(string internalId)
        {
            List<AccelaSDKModels.Inspection> inspectionRecords = this.recApi.GetRecordInspections(internalId, this.BackgroundToken, null, 0, 1000);

            List<Inspection> inspections = new List<Inspection>();
            if (inspectionRecords != null)
            {
                foreach (var inspectionRecord in inspectionRecords)
                {
                    inspections.Add(this.ToInspection(inspectionRecord));
                }
            }

            return inspections;
        }

        public Inspection GetInspection(string permitNumber, string inspectionId)
        {
            AccelaSDKModels.Inspection inspectionRecord = this.inspectionApi.GetInspection(inspectionId, this.BackgroundToken);
            return this.ToInspection(inspectionRecord);
        }

        public Attachment GetInspectionAttachment(string permitNumber, string inspectionId, string attachmentId)
        {
            string documentId;
            this.ParseAttachmentId(attachmentId, out documentId);
            var attachment = this.documentApi.DownloadDocument(documentId, this.BackgroundToken);
            return new Attachment
            {
                Content = attachment.Content,
                ContentType = attachment.ContentType
            };
        }

        #endregion

        #region Contractors

        public List<Contractor> GetContractors(string permitNumber)
        {
            throw new NotImplementedException();
        }

        public Contractor GetContractor(string permitNumber, string contractorId)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    internal class UsAddress
    {
        public string AddressNumber { get; set; }

        public string PlaceName { get; set; }

        public string StateName { get; set; }

        public string StreetName { get; set; }

        public string StreetNamePostType { get; set; }

        public string StreetNamePreDirectional { get; set; }

        public string ZipCode { get; set; }
    }   
}
