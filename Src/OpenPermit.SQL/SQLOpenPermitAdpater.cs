using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PetaPoco;
using RestSharp;
using System.Configuration;
using System.Net;
using Newtonsoft.Json;
using System.Web;

namespace OpenPermit.SQL
{
    public class UsAddress
    {
        public string addressLine { get; set; }
        public string adminDistrict { get; set; }
        public string adminDistrict2 { get; set; }
        public string countryRegion { get; set; }
        public string formattedAddress { get; set; }
        public string locality { get; set; }
        public string postalCode { get; set; }
    }

    public class SQLOpenPermitAdpater: IOpenPermitAdapter
    {
        private Database db;

        public SQLOpenPermitAdpater()
        {
            db = new Database("openpermit");

        }

        private UsAddress ParseAddress(string address)
        {
            string addressUrl = ConfigurationManager.AppSettings.Get("OP.SQL.Bing.Map.Url");
            string key = ConfigurationManager.AppSettings.Get("OP.SQL.Bing.Map.Key");
            RestClient client = new RestClient();
            string encodedAddress = WebUtility.UrlEncode(address);
            string baseUrl = String.Format("{0}?q={1}&key={2}", addressUrl, encodedAddress, key);
            client.BaseUrl = new Uri(baseUrl);
 
            RestRequest request = new RestRequest();
            request.RequestFormat = DataFormat.Json;
            request.Method = Method.GET;

            IRestResponse response = client.Execute(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                return null;
            }

            dynamic addResultsL1 = JsonConvert.DeserializeObject(response.Content);
            dynamic resourceSets = addResultsL1.resourceSets[0];

            if (resourceSets.estimatedTotal == 0)
            {
                return null;
            }

            string addressDyn = JsonConvert.SerializeObject(resourceSets.resources[0].address);
            UsAddress addResult = JsonConvert.DeserializeObject<UsAddress>(addressDyn);

            return addResult;
        }

        public List<Permit> SearchPermits(PermitFilter filter)
        {
            if (filter.PermitNumber != null)
            {
                return this.db.Fetch<Permit>("SELECT * FROM Permit WHERE PermitNum=@0", filter.PermitNumber);
            }
            else if(filter.Address != null)
            {
                UsAddress addr = this.ParseAddress(filter.Address);
                if (addr == null)
                {
                    return null;
                }

                return this.db.Fetch<Permit>("SELECT * FROM Permit WHERE OriginalAddress1=@0 AND " + 
                    "OriginalCity=@1 AND OriginalState=@2 AND OriginalZip=@3",
                    addr.addressLine, addr.locality, addr.adminDistrict, addr.postalCode);
            }

            throw new Exception("Bad Permit Filter Format. Either Permit Number or Address must be entered");
        }

        public Permit GetPermit(string permitNumber)
        {
            return this.db.SingleOrDefault<Permit>("SELECT * FROM Permit WHERE PermitNum=@0", permitNumber);
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
