using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using OpenPermit.Socrata;

namespace OpenPermit.MDC
{
    public class MDCOpenPermitAdapter : SocrataOpenPermitAdapter
    {
        protected override void SetJurisdiction(Permit permit)
        {
            permit.Jurisdiction = "12086";
            permit.Publisher = "Miami-Dade County, FL";
        }

        private bool CheckIfExpired(Permit permit)
        {
            string relURL = string.Format("BNZAW963.DIA?PERM={0}", permit.PermitNum);
            string addressUrl = ConfigurationManager.AppSettings.Get("OP.MDC.Web.Url");
            addressUrl = addressUrl + relURL;

            string content = DoGet(addressUrl);
            if (content == null)
            {
                return false;
            }

            HtmlDocument doc = new HtmlDocument();

            doc.LoadHtml(content);

            string expiredDate = string.Empty;
            string prevText = string.Empty;

            foreach (HtmlNode td in doc.DocumentNode.SelectNodes("//table/tr/td"))
            {
                string inText = td.InnerText;
                inText = inText.Trim().Replace("&nbsp;", string.Empty);

                if (prevText == "Expiration Date:")
                {
                    expiredDate = inText;
                }

                prevText = inText;
            }

            if (expiredDate != string.Empty)
            {
                permit.ExpiresDate = Convert.ToDateTime(expiredDate);
                permit.StatusCurrent = "EXPIRED";
                permit.StatusCurrentMapped = "Permit Cancelled";
                return true;
            }

            return false;
        }

        private List<Inspection> GetMDCInspections(string permitNum)
        {
            string relURL = string.Format("BNZAW962.DIA?PERM={0}", permitNum);
            return this.GetMDCInspections(relURL, new List<Inspection>());
        }

        private List<Inspection> GetMDCInspections(string relURL, List<Inspection> current)
        {
            HtmlDocument doc = new HtmlDocument();

            string addressUrl = ConfigurationManager.AppSettings.Get("OP.MDC.Web.Url");
            addressUrl = addressUrl + relURL;

            string content = DoGet(addressUrl);
            if (content == null)
            {
                return current;
            }

            // This patch is need to fix html syntax error
            content = content.Replace(
                              "<b>Permit Status</b>           \r\n  <td",
                              "<b>Permit Status</b></a></font></td>           \r\n  <td");
            doc.LoadHtml(content);

            List<Inspection> result = current;

            string permitNumber = string.Empty;
            string inspectorName = string.Empty;
            string inspectionType = string.Empty;
            string disposition = string.Empty;
            string clerkName = string.Empty;
            string requestDate = string.Empty;
            string inspectionDate = string.Empty;
            string resultDate = string.Empty;
            string inspectionTime = string.Empty;
            string comments = string.Empty;

            string prevText = string.Empty;

            foreach (HtmlNode td in doc.DocumentNode.SelectNodes("//table/tr/td"))
            {
                string inText = td.InnerText;
                inText = inText.Trim().Replace("&nbsp;", string.Empty);

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
                        if (requestDate != string.Empty)
                        {
                            inspection.RequestDate = Convert.ToDateTime(requestDate);
                        }

                        inspection.InspType = inspectionType;
                        if (inspectionDate != string.Empty)
                        {
                            inspection.ScheduledDate = Convert.ToDateTime(inspectionDate);
                        }

                        if (resultDate != string.Empty)
                        {
                            inspection.InspectedDate = Convert.ToDateTime(resultDate);
                        }

                        inspection.Inspector = inspectorName;
                        inspection.Result = disposition;
                        inspection.InspectionNotes = comments;
                        inspection.ExtraFields = string.Format("ClerkName:{0},InspectionTime:{1}", clerkName, inspectionTime);
                        result.Add(inspection);
                        inspectorName = string.Empty;
                        inspectionType = string.Empty;
                        disposition = string.Empty;
                        clerkName = string.Empty;
                        requestDate = string.Empty;
                        inspectionDate = string.Empty;
                        resultDate = string.Empty;
                        inspectionTime = string.Empty;
                        comments = string.Empty;
                        break;
                }

                if (inText == "Next Page")
                {
                    string nextURL = td.FirstChild.LastChild.GetAttributeValue("href", string.Empty);
                    if (nextURL != string.Empty)
                    {
                        return this.GetMDCInspections(nextURL, result);
                    }
                }

                prevText = inText;
            }

            return result;
        }

        public override List<Inspection> GetInspections(string permitNumber)
        {
            return this.GetMDCInspections(permitNumber);
        }
    }
}
