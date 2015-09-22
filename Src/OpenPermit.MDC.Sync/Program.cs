using Newtonsoft.Json;
using PetaPoco;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace OpenPermit.MDC.Sync
{
    public class JobAddress
    {
        public string address { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string zip { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            List<Permit> permits = getMDCPermits();
            Database db = new Database("openpermit");
            foreach (Permit permit in permits)
            {
                if (permit.PermitNum != null)
                    db.Insert("Permit", "PermitNum", false, permit);
            }
        }

        private static List<Permit> getMDCPermits()
        {
            string addressUrl = ConfigurationManager.AppSettings.Get("OP.MDC.OpenData.Url");
            RestClient client = new RestClient();
            client.BaseUrl = new Uri(addressUrl);

            RestRequest request = new RestRequest();
            request.RequestFormat = DataFormat.Json;
            request.Method = Method.GET;

            IRestResponse response = client.Execute(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                return null;
            }

            dynamic dynPermits = JsonConvert.DeserializeObject(response.Content);

            List<Permit> permits = new List<Permit>();

            foreach (dynamic dynPermit in dynPermits)
            {
                Permit permit = new Permit();
                permit.PermitNum = dynPermit.permitnumber;
                permit.PermitType = dynPermit.permittype;

                if (dynPermit.jobsite != null)
                {
                    string addressJson = dynPermit.jobsite.human_address;
                    JobAddress jobAdd = JsonConvert.DeserializeObject<JobAddress>(addressJson);
                    permit.OriginalAddress1 = jobAdd.address;
                    permit.OriginalCity = jobAdd.city;
                    permit.OriginalState = jobAdd.state;
                    permit.OriginalZip = jobAdd.zip;
                    if (dynPermit.jobsite.latitude != null)
                        permit.Latitude = dynPermit.jobsite.latitude;
                    if (dynPermit.jobsite.longitude != null)
                        permit.Longitude = dynPermit.jobsite.longitude;
                }

                if (dynPermit.contractoraddress != null)
                {
                    string addressJson = dynPermit.contractoraddress.human_address;
                    JobAddress contAdd = JsonConvert.DeserializeObject<JobAddress>(addressJson);
                    permit.ContractorAddress1 = contAdd.address;
                    permit.ContractorCity = contAdd.city;
                    permit.ContractorState = contAdd.state;
                    permit.ContractorZip = contAdd.zip;
                }

                permit.ContractorFullName = dynPermit.contractorname;
                permit.ContractorPhone = dynPermit.contractorphone;
                if (dynPermit.squarefeet != null)
                    permit.TotalAccSqFt = dynPermit.squarefeet;
                permit.Description = dynPermit.proposedusedescription;
                if (dynPermit.estimatedvalue != null)
                    permit.EstProjectCost = dynPermit.estimatedvalue;
                permit.PermitTypeDesc = dynPermit.typecodedescription;
                if (dynPermit.units != null)
                    permit.HousingUnits = dynPermit.units;

                //Figure out how to initialized dates
                permit.AppliedDate = DateTime.Now;
                permit.COIssuedDate = DateTime.Now;
                permit.CompletedDate = DateTime.Now;
                permit.ExpiresDate = DateTime.Now;
                permit.HoldDate = DateTime.Now;
                permit.IssuedDate = DateTime.Now;
                permit.StatusDate = DateTime.Now;
                permit.VoidDate = DateTime.Now;

                permits.Add(permit);
                
            }

            return permits;
        }
    }
}
