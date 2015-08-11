﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Web;
using System.IO;
using System.Net;
using System.Configuration;

using OpenPermit;

using Newtonsoft.Json;
using RestSharp;

using Accela.Web.SDK;
using Accela.Web.SDK.Models;
using AccelaSDKModels = Accela.Web.SDK.Models;

namespace OpenPermit.Accela
{
    public class UsAddress
    {
        public string AddressNumber { get; set; }
        public string PlaceName { get; set; }
        public string StateName { get; set; }
        public string StreetName { get; set; }
        public string StreetNamePostType { get; set; }
        public string StreetNamePreDirectional { get; set; }
        public string ZipCode { get; set; }
    }

    public class AccelaOpenPermitAdapter : IOpenPermitAdapter
    {
        private readonly string agencyAppId;
        private readonly string agencyAppSecret;
        private static string scope = "records get_user_profile inspections settings documents";
        private static IConfigurationProvider appConfig = new AppConfigurationProvider();

        // TODO move token management to separate class and handle expiration?
        private AccessToken backgroundToken;

        private OpenPermitContext context;
        // TODO create new object to put Accela handler into lazy factory or bypass accela sdk altogether and create our own class
        private RecordHandler recApi;
        private InspectionHandler inspectionApi;
        private DocumentHandler documentApi;

        private Dictionary<string, List<string>> lists = new Dictionary<string, List<string>>();

        private AccelaConfig config;
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
            this.config = JsonConvert.DeserializeObject<AccelaConfig>(context.Agency.Configuration);
                
            // TODO using agency app only, should we change this to citizen app?
            recApi = new RecordHandler(agencyAppId, agencyAppSecret, ApplicationType.Agency, string.Empty, appConfig);
            inspectionApi = new InspectionHandler(agencyAppId, agencyAppSecret, ApplicationType.Agency, string.Empty, appConfig);
            documentApi = new DocumentHandler(agencyAppId, agencyAppSecret, ApplicationType.Agency, string.Empty, appConfig);

            connection = HttpUtility.ParseQueryString(context.Agency.ConnectionString);
            recApi.AgencyId = connection["id"];
            recApi.Environment = connection["env"];
            inspectionApi.AgencyId = connection["id"];
            inspectionApi.Environment = connection["env"];
            documentApi.AgencyId = connection["id"];
            documentApi.Environment = connection["env"];
        }

        private AccessToken RefreshBackgroundToken()
        {
            //TODO cache these agency tokens and handle expiration
            var civicId = new CivicIDOAuthClient(agencyAppId, agencyAppSecret, scope, connection["env"]);
            civicId.AgencyName = connection["id"];

            // TODO handle token retrive error
            return civicId.QueryAccessToken(connection["u"], connection["p"]);
        }

        private string BackgroundToken
        {
            get
            {
                //TODO cache these agency tokens and handle expiration?
                if (backgroundToken == null)
                {
                    backgroundToken = RefreshBackgroundToken();

                    if (backgroundToken == null)
                    {
                        throw new Exception("Unable to refresh background token.");
                    }
                }
                return backgroundToken.Token;
            }
        }

        private struct FieldModel
        {
            public Field Field { get; set; }
            public string FeeIndicator { get; set; }
        }

        private struct TaskStatusMapping
        {
            public string TaskStatus { get; set; }
            public string Status { get; set; }
            public string Comments { get; set; }
            public string Action { get; set; }
        }

        private struct TaskMapping
        {
            public string Name { get; set; }
            public string Task { get; set; }
            public string SubstateOf { get; set; }
            public List<TaskStatusMapping> Content { get; set; }
        }

        private struct AppStatusMapping
        {
            public string AppStatus { get; set; }
            public string Status { get; set; }
            public string State { get; set; }
            public string Action { get; set; }
        }

        private class AccelaConfig
        {
            public string[] Modules { get; set; }
            public Dictionary<string, List<Field>> Elements { get; set; }
            public List<TaskMapping> History { get; set; }
            public List<AppStatusMapping> Status { get; set; }
        }

        private string AccelaIdFromLocalId(string localId)
        {
            int index = localId.IndexOf('-');
            return localId.Substring(index + 1);
        }

        private string AccelaIdToLocalId(string accelaId)
        {
            return context.Agency.Id + "-" + accelaId;
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
            ResultDataPaged<Record> page = recApi.SearchRecords(null, recordFilter, null, -1, -1, null, null, "addresses");
            return page.Data.First();
        }

        private string GetRecordIdByNumber(string number)
        {
            // TODO cache these record ids to avoid call to backend
            Record record = GetRecord(number);
            return record.id;
        }

        #region Permits
        private PermitType ToPermitType(RecordType recordType)
        {
            return new PermitType
            {
                Id = AccelaIdToLocalId(recordType.id),
                Name = recordType.text,
                Agency = new Agency
                {
                    Name = context.Agency.Name,
                    Id = context.Agency.Id
                }
            };
        }

        private Permit ToPermit(Record record)
        {
            Permit permit = new Permit
            {
                PermitNum = record.customId,
                PermitType = record.type.group,
                PermitTypeDesc = record.type.text,
                PermitClass = record.type.type,
                WorkClass = record.type.subType,
                StatusCurrent = record.status.text,
                Fee = record.totalFee,
                ProjectName = record.name,
                EstProjectCost = record.estimatedTotalJobCost,
                                
                Jurisdiction = context.Agency.Id,
                Publisher = context.Agency.Name
            };

            if(record.completeDate != null)
            {
                permit.CompletedDate = DateTime.Parse(record.completedDate);
            }

            if (record.status != null)
            {
                var statusConfig = config.Status;
                //TODO what happens if there is not configuration for status?
                var mapping = statusConfig.SingleOrDefault<AppStatusMapping>(m => m.AppStatus == record.status.text);
                if (mapping.AppStatus != null)
                {
                    permit.StatusCurrentMapped = (mapping.Status != null) ? mapping.Status : record.status.text;
                }
                else
                {
                    permit.StatusCurrentMapped = record.status.text;
                }
            }
            else
            {
                permit.StatusCurrentMapped = "Draft";
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
                    permit.OriginalState = address.state.text;
                    permit.OriginalZip = address.postalCode;
                }
            }

            return permit;
        }

        
        private UsAddress ParseAddress(string address)
        {
            string usAddressUrl = ConfigurationManager.AppSettings["OP.Accela.UsAddress.Url"];
            RestClient client = new RestClient();
            client.BaseUrl = new Uri(usAddressUrl);

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

        public List<Permit> SearchPermits(PermitFilter filter)
        {
            var recordFilter = new RecordFilter();

            if (filter.PermitNumber != null)
            {
                recordFilter.customId = filter.PermitNumber;
            }
            else if(filter.Address != null)
            {
                Address address = new Address();
                UsAddress usAdd = this.ParseAddress(filter.Address);
                int strNo;
                int.TryParse(usAdd.AddressNumber, out strNo);
                address.streetStart = strNo;
                address.streetName = usAdd.StreetName;
                //address.xCoordinate = -82.458328247070312;
                //address.yCoordinate = 27.938003540039063;
                recordFilter.address = address;
            }

            ResultDataPaged<Record> page = recApi.SearchRecords(null, recordFilter, null, 0, 1000, null, null, "addresses");

            var result = new List<Permit>();
            if (page.Data != null)
            {
                foreach (var record in page.Data)
                {
                    Permit permit = ToPermit(record);
                    result.Add(permit);
                }
            }

            return result;
        }

        public Permit GetPermit(string permitNumber)
        {
            Record record = GetRecord(permitNumber);
            return ToPermit(record);
        }

        #endregion

        #region Inspections
        public List<Inspection> GetInspections(string permitNumber)
        {
            string id = GetRecordIdByNumber(permitNumber);
            return GetPermitInspectionsInternal(id);
        }

        private List<Inspection> GetPermitInspectionsInternal(string internalId)
        {
            List<AccelaSDKModels.Inspection> recordInspections = recApi.GetRecordInspections(internalId, BackgroundToken, null, 0, 1000);

            List<Inspection> inspections = new List<Inspection>();
            if (recordInspections != null)
            {
                foreach (var recordInspection in recordInspections)
                {
                    // TODO read status to resultype mapping from backend config to map to open permit
                    // Mapping can be created from /v4/settings/inspections/statuses ?

                    var inspection = new Inspection
                    {
                        Id = recordInspection.id.ToString(),
                        Inspector = recordInspection.inspectorFullName,
                        Result = recordInspection.status.text, 
                        //TODO do the mapping from config
                        //ResultMapped = from config
                        InspType = recordInspection.type.text,
                        //TODO do the mapping from config
                        //InspTypeMapped = from config
                        PermitNum = recordInspection.recordId.customId,
                        // TODO verify this
                        ScheduledDate = recordInspection.scheduledDate,
                        DesiredDate = recordInspection.scheduleDate,
                        InspectionNotes = recordInspection.resultComment,
                        RequestDate = recordInspection.requestDate,
                        InspectedDate = recordInspection.completedDate,

                    };
                    inspections.Add(inspection);
                }
            }

            return inspections;
        }

        public Inspection GetInspection(string permitNumber, string inspectionId)
        {
            throw new NotImplementedException();
        }

        public Attachment GetInspectionAttachment(string permitNumber, string inspectionId, string attachmentId)
        {
            string documentId;
            ParseAttachmentId(attachmentId, out documentId);
            var attachment = documentApi.DownloadDocument(documentId, BackgroundToken);
            return new Attachment
            {
                Content = attachment.Content,
                ContentType = attachment.ContentType
            };
        }

        #endregion
    }
}