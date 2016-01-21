using System;
using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Cors;
using System.Web.Http.Dependencies;
using System.Web.Http.Filters;

using Microsoft.AspNet.WebApi.MessageHandlers.Compression;
using Microsoft.AspNet.WebApi.MessageHandlers.Compression.Compressors;
using Microsoft.Owin;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using Owin;

[assembly: OwinStartup(typeof(OpenPermit.DefaultOpenPermitStartup))]

namespace OpenPermit
{
    public class LowercaseContractResolver : DefaultContractResolver
    {
        protected override string ResolvePropertyName(string propertyName)
        {
            return propertyName.ToLower();
        }
    }

    public class DefaultOpenPermitStartup
    {
        public void Configuration(IAppBuilder app)
        {
            HttpConfiguration config = new HttpConfiguration();
            var cors = new EnableCorsAttribute("*", "*", "*");
            config.EnableCors(cors);
            config.MapHttpAttributeRoutes();

            var settings = new JsonSerializerSettings();
            settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            settings.NullValueHandling = NullValueHandling.Ignore;
            settings.DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate;
            config.Formatters.JsonFormatter.SerializerSettings = settings;

            var factory = new DefaultOpenPermitAdapterFactory();
            var adapter = factory.GetOpenPermitAdapter();

            config.Services.Add(typeof(IFilterProvider), new DefaultOpenPermitAdapterFilter(adapter));
            config.MessageHandlers.Insert(0, new ServerCompressionHandler(4096, new GZipCompressor(), new DeflateCompressor()));
            app.UseWebApi(config);
        }
    }

    internal class DefaultOpenPermitAdapterFilter : ActionFilterAttribute, IFilterProvider
    {
        private IOpenPermitAdapter adapter;

        public DefaultOpenPermitAdapterFilter(IOpenPermitAdapter adapter)
        {
            if (adapter == null)
            {
                throw new ArgumentNullException("factory can not be null.");
            }

            this.adapter = adapter;
        }

        public IEnumerable<FilterInfo> GetFilters(HttpConfiguration configuration, HttpActionDescriptor actionDescriptor)
        {
            if (actionDescriptor.ControllerDescriptor.ControllerType == typeof(OpenPermitController))
            {
                return new List<FilterInfo> { new FilterInfo(this, FilterScope.Action) };
            }

            return new List<FilterInfo>();
        }

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            OpenPermitController controller = actionContext.ControllerContext.Controller as OpenPermitController;
            controller.Adapter = this.adapter;
        }
    }
}
