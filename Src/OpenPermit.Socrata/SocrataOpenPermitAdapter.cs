using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace OpenPermit.Socrata
{
    public class JobAddress
    {
        public string Address { get; set; }

        public string City { get; set; }

        public string State { get; set; }

        public string Zip { get; set; }
    }

    public class SocrataOpenPermitAdapter : IOpenPermitAdapter
    {
        private readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings();
        private JObject mappingJson;
        
        public SocrataOpenPermitAdapter()
        {
            string mappingFile = "mapping.json";
            mappingFile = System.IO.Path.Combine(System.Environment.CurrentDirectory, mappingFile);
            this.SetMappingJson(mappingFile);

            this.serializerSettings.ContractResolver = new LowercaseContractResolver();
        }

        public SocrataOpenPermitAdapter(string mappingFile)
        {
            this.SetMappingJson(mappingFile);
            this.serializerSettings.ContractResolver = new LowercaseContractResolver();
        }

        public List<Permit> SearchPermits(PermitFilter filter)
        {
            // Only Page Filter support at the moment
            if (filter.Page == null)
            {
                return this.GetSocrataPermits();
            }

            return this.GetSocrataPermits(filter.Page.Limmit, filter.Page.Offset);
        }

        public Permit GetPermit(string permitNumber)
        {
            throw new NotImplementedException();
        }

        public List<PermitStatus> GetPermitTimeline(string permitNumber)
        {
            throw new NotImplementedException();
        }

        public List<Inspection> GetInspections(string permitNumber)
        {
            throw new NotImplementedException();
        }

        public Inspection GetInspection(string permitNumber, string inspectionId)
        {
            throw new NotImplementedException();
        }

        public Attachment GetInspectionAttachment(string permitNumber, string inspectionId, string attachmentId)
        {
            throw new NotImplementedException();
        }

        public List<Contractor> GetContractors(string permitNumber)
        {
            throw new NotImplementedException();
        }

        public Contractor GetContractor(string permitNumber, string contractorId)
        {
            throw new NotImplementedException();
        }

        private void SetMappingJson(string mappingFile)
        {
            this.mappingJson = JObject.Parse(File.ReadAllText(mappingFile));
        }

        private string GetMapped(string property)
        {
            return this.mappingJson.GetValue(property).ToString();
        }

        private List<Permit> GetSocrataPermits()
        {
            int offset = 0;
            int limit = 10000;
            int pageSize = 0;
            List<Permit> result = new List<Permit>();
            do
            {
                List<Permit> permitPage = this.GetSocrataPermits(limit, offset);
                offset = offset + limit;
                pageSize = permitPage.Count;
                result.AddRange(permitPage);
            }
            while (pageSize == limit);

            return result;
        }

        private string DoGet(string url)
        {
            RestClient client = new RestClient();
            client.BaseUrl = new Uri(url);

            RestRequest request = new RestRequest();
            request.RequestFormat = DataFormat.Json;
            request.Method = Method.GET;

            IRestResponse response = client.Execute(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                return null;
            }

            return response.Content;
        }

        private List<Permit> GetSocrataPermits(int limit, int offset)
        {
            string addressUrl = ConfigurationManager.AppSettings.Get("OP.Socrata.Permit.Url");
            addressUrl = addressUrl + "?$limit={0}&$offset={1}";
            addressUrl = string.Format(addressUrl, limit, offset);
            string content = this.DoGet(addressUrl);

            if (content == null)
            {
                return null;
            }

            dynamic dynPermits = JsonConvert.DeserializeObject(content);

            List<Permit> permits = new List<Permit>();

            foreach (dynamic dynPermit in dynPermits)
            {
                Permit permit = new Permit();
                permit.PermitNum = dynPermit[this.GetMapped("PermitNum")];
                permit.MasterPermitNum = dynPermit[this.GetMapped("MasterPermitNum")];
                permit.AppliedDate = Convert.ToDateTime(dynPermit[this.GetMapped("AppliedDate")]);
                permit.IssuedDate = Convert.ToDateTime(dynPermit[this.GetMapped("IssuedDate")]);

                if (dynPermit[this.GetMapped("StatusDate")] != null)
                {
                    permit.StatusDate = Convert.ToDateTime(dynPermit[this.GetMapped("StatusDate")]);
                }

                // Move these to MDC Adapter
                permit.Jurisdiction = "12086";
                permit.Publisher = "Miami-Dade County, FL";

                permit.PermitType = dynPermit[this.GetMapped("PermitType")];

                if (dynPermit[this.GetMapped("JobLocation")] != null)
                {
                    dynamic dynLocation = dynPermit[this.GetMapped("JobLocation")];
                    string originalAddressJson = dynLocation[this.GetMapped("JobAddress")];
                    JobAddress jobAdd = JsonConvert.DeserializeObject<JobAddress>(originalAddressJson, this.serializerSettings);
                    permit.OriginalAddress1 = jobAdd.Address;
                    permit.OriginalCity = jobAdd.City;
                    permit.OriginalState = jobAdd.State;
                    permit.OriginalZip = jobAdd.Zip;

                    if (dynLocation[this.GetMapped("JobLatitude")] != null)
                    {
                        permit.Latitude = dynLocation[this.GetMapped("JobLatitude")];
                        if (permit.Latitude < -90)
                        {
                            permit.Latitude = -90;
                        }
                        else if (permit.Latitude > 90)
                        {
                            permit.Latitude = 90;
                        }
                    }

                    if (dynLocation[this.GetMapped("JobLongitude")] != null)
                    {
                        permit.Longitude = dynLocation[this.GetMapped("JobLongitude")];
                        if (permit.Longitude < -180)
                        {
                            permit.Longitude = -180;
                        }
                        else if (permit.Longitude > 180)
                        {
                            permit.Longitude = 180;
                        }
                    }
                }

                permit.ProjectId = dynPermit[this.GetMapped("ProjectId")];
                permit.ContractorAddress1 = dynPermit[this.GetMapped("ContractorAddress1")];
                permit.ContractorCity = dynPermit[this.GetMapped("ContractorCity")];
                permit.ContractorState = dynPermit[this.GetMapped("ContractorState")];
                permit.ContractorZip = dynPermit[this.GetMapped("ContractorZip")];

                permit.ContractorFullName = dynPermit[this.GetMapped("ContractorFullName")];
                permit.ContractorPhone = dynPermit[this.GetMapped("ContractorPhone")];
                permit.ContractorLicNum = dynPermit[this.GetMapped("ContractorLicNum")];
                if (dynPermit[this.GetMapped("TotalSqFt")] != null)
                {
                    permit.TotalSqFt = dynPermit[this.GetMapped("TotalSqFt")];
                }

                permit.Description = dynPermit[this.GetMapped("Description")];
                if (dynPermit[this.GetMapped("EstProjectCost")] != null)
                {
                    permit.EstProjectCost = dynPermit[this.GetMapped("EstProjectCost")];
                }

                permit.PermitTypeDesc = dynPermit[this.GetMapped("PermitTypeDesc")];
                if (dynPermit[this.GetMapped("HousingUnits")] != null)
                {
                    permit.HousingUnits = dynPermit[this.GetMapped("HousingUnits")];
                }

                permits.Add(permit);
            }

            return permits;
        }
    }
}
