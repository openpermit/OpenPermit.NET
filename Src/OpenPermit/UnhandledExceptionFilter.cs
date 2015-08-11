using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Diagnostics;

using System.Net.Http;
using System.Web.Http.Filters;

namespace OpenPermit
{
    public class UnhandledExceptionFilter : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext context)
        {
            HttpStatusCode status = HttpStatusCode.InternalServerError;
            var exception = context.Exception;


            Trace.TraceError("Message: {0} Stack: {1}", exception.Message, exception.StackTrace);

            // create a new response and attach our ApiError object
            // which now gets returned on ANY exception result
            var errorResponse = context.Request.CreateResponse(status);
            context.Response = errorResponse;

            base.OnException(context);
        }
    }
}
