using HtmlAgilityPack;
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
            Console.WriteLine(DateTime.Now);
            Console.WriteLine("Bringin data from Socrata");
            List<Permit> permits = getSocrataPermits();
            Console.WriteLine(DateTime.Now);
            Console.WriteLine("Cleaning Data from DB");
            Database db = new Database("openpermit");
            //Cleanup DB to bring new points, this will change once we do overlay new socrata file
            db.Execute("DELETE FROM Permit");
            db.Execute("DELETE FROM Inspection");
            Console.WriteLine(DateTime.Now);
            Console.WriteLine("Inserting Data into DB");
            foreach (Permit permit in permits)
            {
                if (permit.PermitNum != null)
                {
                    db.Insert("Permit", "PermitNum", false, permit);
                    Console.WriteLine("Getting Inspections for Permit: " + permit.PermitNum);
                    List<Inspection> inspections = getMDCInspections(permit.PermitNum);
                    Console.WriteLine(DateTime.Now);
                    foreach (Inspection inspection in inspections)
                    {
                        db.Insert("Inspection", "UniqueId", true, inspection);
                    }
                }
            }
            Console.WriteLine(DateTime.Now);
            Console.WriteLine("Populating Geography Field");
            db.Execute("UPDATE Permit SET Location=geography::Point(Latitude, Longitude, 4326)");
            Console.WriteLine(DateTime.Now);
            Console.WriteLine("Done");

        }

        private static List<Inspection> getMDCInspections( string permitNum )
        {
            string relURL = String.Format("BNZAW962.DIA?PERM={0}", permitNum);
            return getMDCInspections(relURL, new List<Inspection>());

        }

        private static List<Inspection> getMDCInspections( string relURL, List<Inspection> current )
        {
            HtmlDocument doc = new HtmlDocument();

            string addressUrl = ConfigurationManager.AppSettings.Get("OP.MDC.Inspection.Url");
            addressUrl = addressUrl + relURL;

            string content = DoGet(addressUrl);
            if (content == null)
            {
                return current;
            }
            //This patch is need to fix html syntax error
            content = content.Replace("<b>Permit Status</b>           \r\n  <td",
                "<b>Permit Status</b></a></font></td>           \r\n  <td");
            doc.LoadHtml(content);

            List<Inspection> result = current;

            string permitNumber = "";
            string inspectorName = "";
            string inspectionType = "";
            string disposition = "";
            string clerkName = "";
            string requestDate = "";
            string inspectionDate = "";
            string resultDate = "";
            string inspectionTime = "";
            string comments = "";

            string prevText = "";

            foreach(HtmlNode td in doc.DocumentNode.SelectNodes("//table/tr/td"))
            {
                string inText = td.InnerText;
                inText = inText.Trim().Replace("&nbsp;", "");

                switch (prevText)
                {
                    case "Permit Number:":
                        permitNumber = inText;
                        break;
                    case "Inspector Name:":
                        inspectorName = inText;
                        break;
                    case "Inspection Type:":
                        inspectionType = inText;
                        break;
                    case "Disposition:":
                        disposition = inText;
                        break;
                    case "Clerk Name:":
                        clerkName = inText;
                        break;
                    case "Request Date:":
                        requestDate = inText;
                        break;
                    case "Inspection Date:":
                        inspectionDate = inText;
                        break;
                    case "Result Date:":
                        resultDate = inText;
                        break;
                    case "Inspection Time:":
                        inspectionTime = inText;
                        break;
                    case "Comments:":
                        comments = inText;
                        Inspection inspection = new Inspection();
                        inspection.PermitNum = permitNumber;
                        if (requestDate != "")
                            inspection.RequestDate = Convert.ToDateTime(requestDate);
                        inspection.InspType = inspectionType;
                        if (inspectionDate != "")
                            inspection.ScheduledDate = Convert.ToDateTime(inspectionDate);
                        if (resultDate != "")
                            inspection.InspectedDate = Convert.ToDateTime(resultDate);
                        inspection.Inspector = inspectorName;
                        inspection.Result = disposition;
                        inspection.InspectionNotes = comments;
                        inspection.ExtraFields = String.Format("ClerkName:{0},InspectionTime:{1}", clerkName, inspectionTime);
                        result.Add(inspection);
                        inspectorName = "";
                        inspectionType = "";
                        disposition = "";
                        clerkName = "";
                        requestDate = "";
                        inspectionDate = "";
                        resultDate = "";
                        inspectionTime = "";
                        comments = "";
                        break;                    
                }

                if (inText == "Next Page")
                {
                    string nextURL = td.FirstChild.LastChild.GetAttributeValue("href", "");
                    if (nextURL != "")
                    {
                        return getMDCInspections(nextURL, result);
                    }
                }

                prevText = inText;
            }

            return result;

        }

        private static List<Permit> getSocrataPermits()
        {
            int offset = 0;
            int limit = 5000;
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

        private static string DoGet( string url)
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

        private static List<Permit> getSocrataPermits(int limit, int offset)
        {
            string addressUrl = ConfigurationManager.AppSettings.Get("OP.MDC.OpenData.Url");
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
                    {
                        permit.Latitude = dynPermit.jobsite.latitude;
                        if (permit.Latitude < -90)
                            permit.Latitude = -90;
                        else if (permit.Latitude > 90)
                            permit.Latitude = 90;
                    }

                    if (dynPermit.jobsite.longitude != null)
                    {
                        permit.Longitude = dynPermit.jobsite.longitude;
                        if (permit.Longitude < -180)
                            permit.Longitude = -180;
                        else if (permit.Longitude > 180)
                            permit.Longitude = 180;
                    }
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
                    permit.TotalSqFt = dynPermit.squarefeet;
                permit.Description = dynPermit.proposedusedescription;
                if (dynPermit.estimatedvalue != null)
                    permit.EstProjectCost = dynPermit.estimatedvalue;
                permit.PermitTypeDesc = dynPermit.typecodedescription;
                if (dynPermit.units != null)
                    permit.HousingUnits = dynPermit.units;

                permits.Add(permit);
                
            }

            return permits;
        }
    }
}
