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

namespace OpenPermit.SQL
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

    public class SQLOpenPermitAdpater: IOpenPermitAdapter
    {
        private UsAddress ParseAddress(string address)
        {
            string usAddressUrl = ConfigurationManager.AppSettings.Get("OP.SQL.Usaddress.Url");
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
            Database db = new Database("openpermit");
            if (filter.PermitNumber != null)
            {
                return db.Fetch<Permit>("SELECT * FROM Permit WHERE PermitNum=@0", filter.PermitNumber);
            }
            else if(filter.Address != null)
            {
                UsAddress addr = this.ParseAddress(filter.Address);
                string addressLine = String.Format("{0} {1} {2} {3}", addr.AddressNumber, 
                    addr.StreetNamePreDirectional, addr.StreetName, addr.StreetNamePostType);
                return db.Fetch<Permit>("SELECT * FROM Permit WHERE OriginalAddress1=@0 AND " + 
                    "OriginalCity=@1 AND OriginalState=@2 AND OriginalZip=@3", 
                    addressLine, addr.PlaceName, addr.StateName, addr.ZipCode);
            }

            throw new Exception("Bad Permit Filter Format. Either Permit Number or Address must be entered");
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
