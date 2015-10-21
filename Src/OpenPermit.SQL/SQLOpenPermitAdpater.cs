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
        private string connectionString;
        private string provider = "System.Data.SqlClient";

        public SQLOpenPermitAdpater()
        {
            //db = new Database("openpermit");
            connectionString = ConfigurationManager.AppSettings.Get("OP.Agency.Connection");
        }

        public SQLOpenPermitAdpater(OpenPermitContext context)
        {
            connectionString = context.Agency.ConnectionString;
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

        private string BoundingBoxToWkt(Box box)
        {
            StringBuilder wkt = new StringBuilder("POLYGON((");
            wkt.Append(box.MinX);
            wkt.Append(' ');
            wkt.Append(box.MinY);
            wkt.Append(',');
            wkt.Append(box.MaxX);
            wkt.Append(' ');
            wkt.Append(box.MinY);
            wkt.Append(',');
            wkt.Append(box.MaxX);
            wkt.Append(' ');
            wkt.Append(box.MaxY);
            wkt.Append(',');
            wkt.Append(box.MinX);
            wkt.Append(' ');
            wkt.Append(box.MaxY);
            wkt.Append(',');
            wkt.Append(box.MinX);
            wkt.Append(' ');
            wkt.Append(box.MinY);
            wkt.Append("))");
            return wkt.ToString();
        }

        public List<Permit> SearchPermits(PermitFilter filter)
        {
            if (filter.PermitNumber != null)
            {
                using (var db = new Database(connectionString, provider))
                {
                    return db.Fetch<Permit>("SELECT * FROM Permit WHERE PermitNum=@0", filter.PermitNumber);
                }
            }
            else if(filter.Address != null)
            {
                UsAddress addr = this.ParseAddress(filter.Address);
                if (addr == null)
                {
                    return null;
                }

                using (var db = new Database(connectionString, provider))
                {
                    return db.Fetch<Permit>("SELECT * FROM Permit WHERE OriginalAddress1=@0 AND " +
                        "OriginalCity=@1 AND OriginalState=@2 AND OriginalZip=@3",
                        addr.addressLine, addr.locality, addr.adminDistrict, addr.postalCode);
                }
            }
            else if(filter.BoundingBox != null)
            {
                string wkt = BoundingBoxToWkt(filter.BoundingBox);
                using (var db = new Database(connectionString, provider))
                {
                    string fields = "*";
                    string conditions = "";
                    string queryBase = "SELECT {0} FROM Permit " +
                        "WHERE {1} Location.Filter(geography::STGeomFromText('" + wkt + "', 4326))=1";

                    if (filter.Fields != null)
                    {
                        switch (filter.Fields)
                        {
                            case FieldChoices.Geo:
                                fields = "PermitNum,Latitude,Longitude";
                                break;
                            case FieldChoices.Recommended:
                                fields = "PermitNum,MasterPermitNum,Location,Description,IssuedDate,CompletedDate" +
                                    ",StatusCurrent,OriginalAddress1,OriginalAddress2,OriginalCity,OriginalState" +
                                    ",OriginalZip,Jurisdiction,PermitClass,PermitClassMapped,StatusCurrentMapped" +
                                    ",AppliedDate,WorkClass,WorkClassMapped,PermitType,PermitTypeMapped,PermitTypeDesc" +
                                    ",StatusDate,TotalSqFt,Link,Latitude,Longitude,EstProjectCost,HousingUnits" +
                                    ",PIN,ContractorCompanyName,ContractorTrade,ContractorTradeMapped,ContractorLicNum" +
                                    ",ContractorStateLic";
                                break;
                            case FieldChoices.Optional:
                                fields = "PermitNum,MasterPermitNum,Location,Description,IssuedDate,CompletedDate" +
                                    ",StatusCurrent,OriginalAddress1,OriginalAddress2,OriginalCity,OriginalState" +
                                    ",OriginalZip,Jurisdiction,PermitClass,PermitClassMapped,StatusCurrentMapped" +
                                    ",AppliedDate,WorkClass,WorkClassMapped,PermitType,PermitTypeMapped,PermitTypeDesc" +
                                    ",StatusDate,TotalSqFt,Link,Latitude,Longitude,EstProjectCost,HousingUnits" +
                                    ",PIN,ContractorCompanyName,ContractorTrade,ContractorTradeMapped,ContractorLicNum" +
                                    ",ContractorStateLic ,ProposedUse,AddedSqFt,RemovedSqFt,ExpiresDate,COIssuedDate" +
                                    ",HoldDate,VoidDate,ProjectName,ProjectId,TotalFinishedSqFt,TotalUnfinishedSqFt" +
                                    ",TotalHeatedSqFt,TotalUnheatedSqFt,TotalAccSqFt,TotalSprinkledSqFt,ExtraFields" +
                                    ",Publisher,Fee,ContractorFullName,ContractorCompanyDesc,ContractorPhone" +
                                    ",ContractorAddress1,ContractorAddress2,ContractorCity,ContractorState,ContractorZip" +
                                    ",ContractorEmail";
                                break;
                        }
                    }

                    if (filter.Types != null)
                    {
                        foreach (TypeChoices type in filter.Types)
                        {
                            switch (type)
                            {
                                case TypeChoices.Master:
                                    conditions += "MasterPermitNum = '0' and ";
                                    break;
                                case TypeChoices.Electrical:
                                    conditions += "PermitType in ('ELEC', 'MELE') and ";
                                    break;
                                case TypeChoices.Plumbing:
                                    conditions += "PermitType in ('PLUM', 'MPLU') and ";
                                    break;
                                case TypeChoices.Mechanical:
                                    conditions += "PermitType in ('MECH', 'MMEC') and ";
                                    break;
                                case TypeChoices.Fire:
                                    conditions += "PermitType in ('FIRE', 'MFIR') and ";
                                    break;
                                case TypeChoices.Building:
                                    conditions += "PermitType in ('BLDG', 'MBLD') and ";
                                    break;
                            }
                        }
                    }

                    return db.Fetch<Permit>(String.Format(queryBase, fields, conditions));
                }
            }
            else
            {
                //No Filter means get all permits
                using (var db = new Database(connectionString, provider))
                {
                    return db.Fetch<Permit>("SELECT * FROM Permit");
                }

            }
        }

        public Permit GetPermit(string permitNumber)
        {
            using (var db = new Database(connectionString, provider))
            {
                return db.SingleOrDefault<Permit>("SELECT * FROM Permit WHERE PermitNum=@0", permitNumber);
            }
        }

        public List<PermitStatus> GetPermitTimeline(string permitNumber)
        {
            using (var db = new Database(connectionString, provider))
            {
                return db.Fetch<PermitStatus>("SELECT * FROM PermitStatus WHERE PermitNum=@0", permitNumber);
            }
        }

        public List<Inspection> GetInspections(string permitNumber)
        {
            using (var db = new Database(connectionString, provider))
            {
                return db.Fetch<Inspection>("SELECT * FROM Inspection WHERE PermitNum=@0", permitNumber);
            }
        }

        public Inspection GetInspection(string permitNumber, string inspectionId)
        {
            using (var db = new Database(connectionString, provider))
            {
                return db.SingleOrDefault<Inspection>("SELECT * FROM Inspection WHERE PermitNum=@0 AND Id=@1", permitNumber, inspectionId);
            }
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
