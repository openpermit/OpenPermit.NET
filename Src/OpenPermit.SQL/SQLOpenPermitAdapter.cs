using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

using Newtonsoft.Json;

using PetaPoco;
using RestSharp;

namespace OpenPermit.SQL
{
    public class SQLOpenPermitAdapter : IOpenPermitAdapter
    {
        private string connectionString;
        private string provider = "System.Data.SqlClient";

        public SQLOpenPermitAdapter()
        {
            // db = new Database("openpermit");
            this.connectionString = ConfigurationManager.AppSettings.Get("OP.Agency.Connection");
        }

        public SQLOpenPermitAdapter(OpenPermitContext context)
        {
            this.connectionString = context.Agency.ConnectionString;
        }

        public List<Permit> SearchPermits(PermitFilter filter)
        {
            string fields = "*";
            string conditions = string.Empty;

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

            if (filter.Status != null && filter.Status.Count > 0)
            {
                conditions += "StatusCurrentMapped IN (";

                foreach (StatusChoices status in filter.Status)
                {
                    switch (status)
                    {
                        case StatusChoices.Applied:
                            conditions += "'Application Accepted',";
                            break;
                        case StatusChoices.PlanReview:
                            conditions += "'In Review',";
                            break;
                        case StatusChoices.Issued:
                            conditions += "'Permit Issued',";
                            break;
                        case StatusChoices.Inspections:
                            conditions += "'Inspection Phase',";
                            break;
                        case StatusChoices.Closed:
                            conditions += "'Permit Finaled',";
                            break;
                        case StatusChoices.Expired:
                            conditions += "'Permit Cancelled',";
                            break;
                    }
                }

                conditions = conditions.Remove(conditions.Length - 1);
                conditions += ") and ";
            }

            if (filter.TimeFrame != null)
            {
                switch (filter.TimeFrame.Item1)
                {
                    case StatusChoices.Applied:
                        string range = "AppliedDate > '{0}' and AppliedDate < '{1}' and ";
                        conditions += string.Format(
                                                     range, 
                                                     filter.TimeFrame.Item2.ToString(),
                                                     filter.TimeFrame.Item3.ToString());
                        break;
                    case StatusChoices.Issued:
                        range = "IssuedDate > '{0}' and IssuedDate < '{1}' and ";
                        conditions += string.Format(
                                                     range, 
                                                     filter.TimeFrame.Item2.ToString(),
                                                     filter.TimeFrame.Item3.ToString());
                        break;
                    case StatusChoices.Closed:
                        range = "CompletedDate > '{0}' and CompletedDate < '{1}' and ";
                        conditions += string.Format(
                                                     range, 
                                                     filter.TimeFrame.Item2.ToString(),
                                                     filter.TimeFrame.Item3.ToString());
                        break;
                    case StatusChoices.Expired:
                        range = "ExpiresDate > '{0}' and ExpiresDate < '{1}' and ";
                        conditions += string.Format(
                                                     range, 
                                                     filter.TimeFrame.Item2.ToString(),
                                                     filter.TimeFrame.Item3.ToString());
                        break;
                }
            }

            if (filter.PermitNumber != null)
            {
                using (var db = new Database(this.connectionString, this.provider))
                {
                    return db.Fetch<Permit>("SELECT * FROM Permit WHERE PermitNum=@0", filter.PermitNumber);
                }
            }
            else if (filter.Address != null)
            {
                dynamic addr = this.ParseAddress(filter.Address);
                if (addr == null)
                {
                    return null;
                }

                string addressLine = addr.addressLine;
                string city = addr.locality;
                string state = addr.adminDistrict;
                string zip = addr.postalCode;

                using (var db = new Database(this.connectionString, this.provider))
                {
                    string queryText = "SELECT {0} FROM Permit WHERE {1} OriginalAddress1=@0 AND " +
                        "OriginalCity=@1 AND OriginalState=@2 AND OriginalZip=@3";
                    queryText = string.Format(queryText, fields, conditions);
                    return db.Fetch<Permit>(
                                             queryText, 
                                             addressLine, 
                                             city,
                                             state, 
                                             zip);
                }
            }
            else if (filter.BoundingBox != null)
            {
                string wkt = this.BoundingBoxToWkt(filter.BoundingBox);
                using (var db = new Database(this.connectionString, this.provider))
                {
                    string queryBase = "SELECT {0} FROM Permit " +
                        "WHERE {1} Location.Filter(geography::STGeomFromText('" + wkt + "', 4326))=1";

                    return db.Fetch<Permit>(string.Format(queryBase, fields, conditions));
                }
            }
            else
            {
                // No Filter means get all permits
                using (var db = new Database(this.connectionString, this.provider))
                {
                    string queryText = "SELECT {0} FROM Permit {1}";
                    string whereText = string.Empty;

                    if (conditions != string.Empty)
                    {
                        if (conditions.EndsWith(" and "))
                        {
                            conditions = conditions.Substring(0, conditions.Length - 5);
                        }

                        whereText = string.Format("WHERE {0}", conditions);
                    }

                    queryText = string.Format(queryText, fields, whereText);
                    return db.Fetch<Permit>(queryText);
                }
            }
        }

        public Permit GetPermit(string permitNumber)
        {
            using (var db = new Database(this.connectionString, this.provider))
            {
                return db.SingleOrDefault<Permit>("SELECT * FROM Permit WHERE PermitNum=@0", permitNumber);
            }
        }

        public List<PermitStatus> GetPermitTimeline(string permitNumber)
        {
            using (var db = new Database(this.connectionString, this.provider))
            {
                return db.Fetch<PermitStatus>("SELECT * FROM PermitStatus WHERE PermitNum=@0", permitNumber);
            }
        }

        public List<Inspection> GetInspections(string permitNumber)
        {
            using (var db = new Database(this.connectionString, this.provider))
            {
                return db.Fetch<Inspection>("SELECT * FROM Inspection WHERE PermitNum=@0", permitNumber);
            }
        }

        public Inspection GetInspection(string permitNumber, string inspectionId)
        {
            using (var db = new Database(this.connectionString, this.provider))
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

        private object ParseAddress(string address)
        {
            string addressUrl = ConfigurationManager.AppSettings.Get("OP.SQL.Bing.Map.Url");
            string key = ConfigurationManager.AppSettings.Get("OP.SQL.Bing.Map.Key");
            RestClient client = new RestClient();
            string encodedAddress = WebUtility.UrlEncode(address);
            string baseUrl = string.Format("{0}?q={1}&key={2}", addressUrl, encodedAddress, key);
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

            return resourceSets.resources[0].address;
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
    }
}
