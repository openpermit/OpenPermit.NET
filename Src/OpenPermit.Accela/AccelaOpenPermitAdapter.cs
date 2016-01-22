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

        private void PopulateRequiredFields(Record record, Permit permit)
        {
            permit.PermitNum = record.customId;
            permit.Description = record.description;

            if (record.firstIssuedDate != null)
            {
                permit.IssuedDate = DateTime.Parse(record.firstIssuedDate);
            }
            else
            {
                // TODO Try to get from workflow, but this would require an API call, investigate what we can do... 
            }

            if (record.openedDate != null)
            {
                permit.AppliedDate = DateTime.Parse(record.openedDate);
            }

            if (record.completeDate != null)
            {
                permit.CompletedDate = DateTime.Parse(record.completedDate);
            }
            else if (
                     record.status != null &&
                     record.statusDate != null &&
                     (record.status.text == "Finaled" || record.status.text == "Completed"))
            {
                permit.CompletedDate = DateTime.Parse(record.statusDate);
            }

            // Populate address portion of the permit
            if (record.addresses != null && record.addresses.Count > 0)
            {
                Address primaryAddress = null;
                if (record.addresses.Count > 1)
                {
                    foreach (Address address in record.addresses)
                    {
                        if (address.isPrimary == "Y")
                        {
                            primaryAddress = address;
                            break;
                        }
                    }
                }

                if (primaryAddress == null)
                {
                    primaryAddress = record.addresses.FirstOrDefault<Address>();
                }

                StringBuilder originalBuilder = new StringBuilder();
                if (primaryAddress.streetStart != 0)
                {
                    originalBuilder.Append(primaryAddress.streetStart);
                }

                if (primaryAddress.streetPrefix != null)
                {
                    originalBuilder.Append(" ");
                    originalBuilder.Append(primaryAddress.streetPrefix);
                }

                if (primaryAddress.streetName != null)
                {
                    originalBuilder.Append(" ");
                    originalBuilder.Append(primaryAddress.streetName);
                }

                if (primaryAddress.streetSuffix != null)
                {
                    originalBuilder.Append(" ");
                    originalBuilder.Append(primaryAddress.streetSuffix.text);
                }

                permit.OriginalAddress1 = originalBuilder.ToString();
                permit.OriginalCity = primaryAddress.city;
                permit.OriginalState = (primaryAddress.state != null) ? primaryAddress.state.text : string.Empty;
                permit.OriginalZip = primaryAddress.postalCode;

                // TODO Most likely will need to geocode on the fly here as agencies do not store lat, lon (Seth Axthelm)
                permit.Longitude = primaryAddress.xCoordinate;
                permit.Latitude = primaryAddress.yCoordinate;
            }
        }

        private void PopulateRecommendedFields(Record record, Permit permit)
        {
            if (record.type != null)
            {
                permit.PermitType = record.type.text;
                permit.PermitTypeDesc = record.type.text;

                if (this.config.PermitType != null)
                {
                    var mapping = this.config.PermitType.SingleOrDefault<PermitTypeMapping>(m => m.PermitType == permit.PermitType);
                    if (mapping.PermitType != null)
                    {
                        permit.PermitTypeMapped = mapping.PermitTypeMapped;
                    }
                }

                // TODO Look into possibility of PermitClass coming from ASI field/custom Form (Seth Axthelm)
                switch (this.config.PermitClassField)
                {
                    case PermitClassField.RecordType:
                        permit.PermitClass = record.type.type;
                        break;
                    case PermitClassField.RecordSubType:
                        permit.PermitClass = record.type.subType;
                        break;
                }

                if (this.config.PermitClass != null)
                {
                    var mapping = this.config.PermitClass.SingleOrDefault<PermitClassMapping>(m => m.PermitClass == permit.PermitClass);
                    if (mapping.PermitClass != null)
                    {
                        permit.PermitClassMapped = mapping.PermitClassMapped;
                    }
                }

                // TODO Look into possibility of WorkClass coming from ASI field/custom Form (Seth Axthelm)
                switch (this.config.WorkClassField)
                {
                    case WorkClassField.RecordType:
                        permit.WorkClass = record.type.type;
                        break;
                    case WorkClassField.RecordSubType:
                        permit.WorkClass = record.type.subType;
                        break;
                    case WorkClassField.ConstructType:
                        if (record.constructionType != null)
                        {
                            permit.WorkClass = record.constructionType.text;
                        }

                        break;
                }

                if (this.config.WorkClass != null)
                {
                    var mapping = this.config.WorkClass.SingleOrDefault<WorkClassMapping>(m => m.WorkClass == permit.WorkClass);
                    if (mapping.WorkClass != null)
                    {
                        permit.WorkClassMapped = mapping.WorkClassMapped;
                    }
                }
            }

            if (record.status != null)
            {
                permit.StatusCurrent = record.status.text;

                if (this.config.Status != null)
                {
                    var mapping = this.config.Status.SingleOrDefault<StatusMapping>(m => m.Status == permit.StatusCurrent);
                    if (mapping.Status != null)
                    {
                        permit.StatusCurrentMapped = mapping.StatusMapped;
                    }
                }
            }

            // TODO This could be an ASI field (Seth Axthelm)
            permit.HousingUnits = record.housingUnits;

            // TODO TotalSqft, this is an ASI field (Seth Axthelm)
            if (record.parcels != null && record.parcels.Count > 0)
            {
                Parcel primaryParcel = null;
                if (record.parcels.Count > 1)
                {
                    foreach (Parcel parcel in record.parcels)
                    {
                        if (parcel.isPrimary == "Y")
                        {
                            primaryParcel = parcel;
                            break;
                        }
                    }
                }

                if (primaryParcel == null)
                {
                    primaryParcel = record.parcels.FirstOrDefault<Parcel>();
                }

                permit.PIN = primaryParcel.parcelNumber;
            }

            if (record.professionals != null && record.professionals.Count > 0)
            {
                Professional primaryContractor = null;
                if (record.professionals.Count > 1)
                {
                    foreach (Professional professional in record.professionals)
                    {
                        if (professional.isPrimary == "Y")
                        {
                            primaryContractor = professional;
                            break;
                        }
                    }
                }

                if (primaryContractor == null)
                {
                    primaryContractor = record.professionals.FirstOrDefault<Professional>();
                }

                permit.ContractorCompanyName = primaryContractor.businessName;
                permit.ContractorLicNum = primaryContractor.licenseNumber;
                permit.ContractorStateLic = (primaryContractor.licensingBoard != null) ? primaryContractor.licensingBoard.text : null;
                permit.ContractorTrade = (primaryContractor.licenseType != null) ? primaryContractor.licenseType.text : null;

                if (permit.ContractorTrade != null)
                {
                    if (this.config.ContractorTrade != null)
                    {
                        var mapping = this.config.ContractorTrade.SingleOrDefault<ContractorTradeMapping>(
                            m => m.ContractorTrade == permit.ContractorTrade);
                        if (mapping.ContractorTrade != null)
                        {
                            permit.ContractorTradeMapped = mapping.ContractorTradeMapped;
                        }
                    }
                }

                // TODO These are optional, perhaps move to different method
                permit.ContractorFullName = (primaryContractor.fullName != null) ? primaryContractor.fullName :
                    primaryContractor.firstName + " " + primaryContractor.lastName;
                permit.ContractorAddress1 = primaryContractor.address1;
                permit.ContractorAddress2 = primaryContractor.address2;
                permit.ContractorCity = primaryContractor.city;
                permit.ContractorState = (primaryContractor.state != null) ? primaryContractor.state.text : null;
                permit.ContractorZip = primaryContractor.postalCode;
                permit.ContractorEmail = primaryContractor.email;
                permit.ContractorPhone = primaryContractor.phone1;
            }
        }

        private void PopulateOptionalFields(Record record, Permit permit)
        {
            // TODO permit.ProposedUse this should come from ASI field
            permit.EstProjectCost = record.estimatedTotalJobCost;
            //// TODO permit.AddedSqFt this should come from ASI field
            //// TODO permit.MasterPermitNum this could be ASI or related record parent 
            //// TODO permit.ExpiresDate this should be ASI field
            //// TODO permit.COIssuedDate this should be ASI or from workflow history
            permit.ProjectName = record.name;

            // TODO should we reuse this for permit id instead?
            permit.ProjectId = permit.MasterPermitNum;
            //// TODO permit.TotalFinishedSqFt from ASI
            //// TODO permit.TotalHeatedSqFt from ASI
            //// TODO permit.TotalAccSqFt from ASI
            //// TODO permit.TotalSprinkledSqFt from ASI
            //// TODO permit.TotalUnfinishedSqFt from ASI
            // TODO permit.TotalUnheatedSqFt from ASI
            permit.Fee = record.totalFee;
            permit.Jurisdiction = this.context.Agency.Id;
            permit.Publisher = this.context.Agency.Name;
        }

        private Permit ToPermit(Record record)
        {
            Permit permit = new Permit();
            this.PopulateRequiredFields(record, permit);
            this.PopulateRecommendedFields(record, permit);
            this.PopulateOptionalFields(record, permit);
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
            List<WorkflowTask> tasks = this.recApi.GetWorkflowTasksHistory(internalId, this.BackgroundToken);

            if (tasks != null)
            {
                foreach (var task in tasks)
                {
                    if (task.isActive == "Y" || task.isCompleted == "Y")
                    {
                        var status = new PermitStatus
                        {
                            PermitNum = permitNumber,
                            StatusPrevious = task.description + " - " + ((task.status != null) ? task.status.text : string.Empty)
                        };

                        if (task.comment != null && task.commentDisplay == "Y")
                        {
                            // TODO How to deal correctly with comment permission via task.commentPublicVisible?
                            // status.Comments = task.comment
                        }

                        if (task.statusDate != null)
                        {
                            status.StatusPreviousDate = DateTime.Parse(task.statusDate);
                        }

                        var mapping = this.config.Timeline.FirstOrDefault<TimelineMapping>(t => t.StatusPrevious == status.StatusPrevious);
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
                PermitNum = inspectionRecord.recordId.customId,
                
                // TODO verify this
                ScheduledDate = inspectionRecord.scheduledDate,
                DesiredDate = inspectionRecord.scheduleDate,
                RequestDate = inspectionRecord.requestDate,
                InspectedDate = inspectionRecord.completedDate,
            };

            if (inspectionRecord.type != null)
            {
                inspection.InspType = inspectionRecord.type.text;
            }

            if (this.config.InspectionType != null)
            {
                var mapping = this.config.InspectionType.SingleOrDefault<InspectionTypeMapping>(m => m.InspectionType == inspection.InspType);
                inspection.InspTypeMapped = mapping.InspectionTypeMapped;
            }

            if (inspectionRecord.status != null)
            {
                inspection.Result = inspectionRecord.status.text;
            }

            if (this.config.InspectionResult != null)
            {
                var mapping = this.config.InspectionResult.SingleOrDefault<InspectionResultMapping>(m => m.Result == inspection.Result);
                inspection.ResultMapped = mapping.ResultMapped;
            }

            if (inspectionRecord.resultComment != null && inspectionRecord.commentDisplay == "Y")
            {
                // TODO How to deal correctly with comment permission via inspectionRecord.commentPublicVisible?
                inspection.InspectionNotes = inspectionRecord.resultComment;
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
