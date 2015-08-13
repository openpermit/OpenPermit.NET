using System;
using System.IO;
using System.Web;
using System.Collections.Generic;
using System.Configuration;

namespace OpenPermit
{
    class DefaultOpenPermitAdapterFactory : IOpenPermitAdapterFactory
    {
        private OpenPermitAgency agency;
        private Type adapterType;

        public DefaultOpenPermitAdapterFactory()
        {
            string agencyName = ConfigurationManager.AppSettings["OP.Agency.Name"];

            if (agencyName == null)
            {
                throw new ConfigurationErrorsException("OP.Agency.Name is a required configuration setting.");
            }

            agency = new OpenPermitAgency
            {
                Id = ((ConfigurationManager.AppSettings["OP.Agency.Id"] != null) ? ConfigurationManager.AppSettings["OP.Agency.Id"] : ""),
                Name = ConfigurationManager.AppSettings["OP.Agency.Name"],
                ConnectionString = ConfigurationManager.AppSettings["OP.Agency.Connection"]
            };

            string agencyConfig = Path.Combine(HttpContext.Current.Server.MapPath("/"), "agency.config");
            if (File.Exists(agencyConfig))
            {
                agency.Configuration = File.ReadAllText(agencyConfig);
            }

            string adapterTypeName = ConfigurationManager.AppSettings["OP.Agency.Adapter"];
            if (adapterTypeName == null)
            {
                throw new ConfigurationErrorsException("OP.Agency.Adapter is a required configuration setting.");
            }

            adapterType = Type.GetType(adapterTypeName);
            if (adapterType == null)
            {
                throw new ArgumentException("OP.Agency.Adapter could not be loaded.");
            }
            if(!typeof(IOpenPermitAdapter).IsAssignableFrom(adapterType))
            {
                throw new ArgumentException("OP.Agency.Adapter is invalid, must implement IOpenPermitAdapter.");
            }
        }

        public IOpenPermitAdapter GetOpenPermitAdapter(OpenPermitContext context = null)
        {
            if (context == null)
            {
                context = new OpenPermitContext();
            }
            context.Agency = agency;
            var adapter = (IOpenPermitAdapter)Activator.CreateInstance(adapterType, new object[] {context});
            return adapter;
        }
    }
}
