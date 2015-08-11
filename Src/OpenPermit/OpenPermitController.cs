using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Collections.Specialized;
using System.Web;
using System.Configuration;
using System.Net.Http.Headers;

namespace OpenPermit
{
    [RoutePrefix("op/permits")]
    [UnhandledExceptionFilter]
    public class OpenPermitController : ApiController
    {
        public IOpenPermitAdapter Adapter { get; set; }

        [Route]
        public HttpResponseMessage GetPermits(string number = null, string address = null)
        {          
            List<Permit> permits = Adapter.SearchPermits(new PermitFilter { PermitNumber = number, Address = address });

            if (permits != null)
            {
                return Request.CreateResponse<List<Permit>>(permits);
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }
        }

        [Route("{number}")]
        public HttpResponseMessage GetPermit(string number, string options = null)
        {
            Permit permit = Adapter.GetPermit(number);

            if (permit.PermitNum != null)
            {
                return Request.CreateResponse<Permit>(permit);
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }
        }
    }
}
