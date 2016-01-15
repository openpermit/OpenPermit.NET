using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace OpenPermit.Socrata
{
    public class JobAddress
    {
        public string address { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string zip { get; set; }
    }

    public class SocrataOpenPermitAdapter: IOpenPermitAdapter
    {
        private JObject mappingJson;

        public SocrataOpenPermitAdapter()
        {
            string mappingFile = "mapping.json";
            mappingFile = System.IO.Path.Combine(System.Environment.CurrentDirectory, mappingFile);
            this.SetMappingJson(mappingFile);
        }

        public SocrataOpenPermitAdapter(string mappingFile)
        {
            this.SetMappingJson(mappingFile);
        }

        private void SetMappingJson(string mappingFile)
        {
            mappingJson = JObject.Parse(File.ReadAllText(mappingFile));
        }

        private string GetMapped(string property)
        {
            return mappingJson.GetValue(property).ToString();
        }

        private List<Permit> getSocrataPermits()
        {
            int offset = 0;
            int limit = 10000;
            int pageSize = 0;
            List<Permit> result = new List<Permit>();
            do
            {
                List<Permit> permitPage = getSocrataPermits(limit, offset);
                offset = offset + limit;
                pageSize = permitPage.Count;
                result.AddRange(permitPage);
            } while (pageSize == limit);

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

        private List<Permit> getSocrataPermits(int limit, int offset)
        {
            string addressUrl = ConfigurationManager.AppSettings.Get("OP.Socrata.Permit.Url");
            addressUrl = addressUrl + "?$limit={0}&$offset={1}";
            addressUrl = String.Format(addressUrl, limit, offset);
            string content = DoGet(addressUrl);

            if (content == null)
            {
                return null;
            }

            dynamic dynPermits = JsonConvert.DeserializeObject(content);

            List<Permit> permits = new List<Permit>();

            foreach (dynamic dynPermit in dynPermits)
            {
                Permit permit = new Permit();
                permit.PermitNum = dynPermit[GetMapped("PermitNum")];
                permit.MasterPermitNum = dynPermit[GetMapped("MasterPermitNum")];
                permit.AppliedDate = Convert.ToDateTime(dynPermit[GetMapped("AppliedDate")]);
                permit.IssuedDate = Convert.ToDateTime(dynPermit[GetMapped("IssuedDate")]);

                if (dynPermit[GetMapped("StatusDate")] != null)
                {
                    permit.StatusDate = Convert.ToDateTime(dynPermit[GetMapped("StatusDate")]);
                }

                //Move these to MDC Adapter
                permit.Jurisdiction = "12086";
                permit.Publisher = "Miami-Dade County, FL";

                permit.PermitType = dynPermit[GetMapped("PermitType")];

                if (dynPermit[GetMapped("JobLocation")] != null)
                {
                    dynamic dynLocation = dynPermit[GetMapped("JobLocation")];
                    string OriginalAddressJson = dynLocation[GetMapped("JobAddress")];
                    JobAddress jobAdd = JsonConvert.DeserializeObject<JobAddress>(OriginalAddressJson);
                    permit.OriginalAddress1 = jobAdd.address;
                    permit.OriginalCity = jobAdd.city;
                    permit.OriginalState = jobAdd.state;
                    permit.OriginalZip = jobAdd.zip;

                    if (dynLocation[GetMapped("JobLatitude")] != null)
                    {
                        permit.Latitude = dynLocation[GetMapped("JobLatitude")];
                        if (permit.Latitude < -90)
                            permit.Latitude = -90;
                        else if (permit.Latitude > 90)
                            permit.Latitude = 90;
                    }

                    if (dynLocation[GetMapped("JobLongitude")] != null)
                    {
                        permit.Longitude = dynLocation[GetMapped("JobLongitude")];
                        if (permit.Longitude < -180)
                            permit.Longitude = -180;
                        else if (permit.Longitude > 180)
                            permit.Longitude = 180;
                    }
                }

                permit.ProjectId = dynPermit[GetMapped("ProjectId")];
                permit.ContractorAddress1 = dynPermit[GetMapped("ContractorAddress1")];
                permit.ContractorCity = dynPermit[GetMapped("ContractorCity")];
                permit.ContractorState = dynPermit[GetMapped("ContractorState")];
                permit.ContractorZip = dynPermit[GetMapped("ContractorZip")];

                permit.ContractorFullName = dynPermit[GetMapped("ContractorFullName")];
                permit.ContractorPhone = dynPermit[GetMapped("ContractorPhone")];
                permit.ContractorLicNum = dynPermit[GetMapped("ContractorLicNum")];
                if (dynPermit[GetMapped("TotalSqFt")] != null)
                    permit.TotalSqFt = dynPermit[GetMapped("TotalSqFt")];
                permit.Description = dynPermit[GetMapped("Description")];
                if (dynPermit[GetMapped("EstProjectCost")] != null)
                    permit.EstProjectCost = dynPermit[GetMapped("EstProjectCost")];
                permit.PermitTypeDesc = dynPermit[GetMapped("PermitTypeDesc")];
                if (dynPermit[GetMapped("HousingUnits")] != null)
                    permit.HousingUnits = dynPermit[GetMapped("HousingUnits")];

                permits.Add(permit);

            }

            return permits;
        }

        public List<Permit> SearchPermits(PermitFilter filter)
        {
            //Only Page Filter support at the moment
            if ( filter.Page == null)
            {
                return this.getSocrataPermits(10, 0);
            }

            return this.getSocrataPermits(filter.Page.limmit, filter.Page.offset);
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
    }
}
