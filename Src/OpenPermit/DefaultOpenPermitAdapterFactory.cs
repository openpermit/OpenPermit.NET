using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Web;

namespace OpenPermit
{
    internal class DefaultOpenPermitAdapterFactory : IOpenPermitAdapterFactory
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

            this.agency = new OpenPermitAgency
            {
                Id = (ConfigurationManager.AppSettings["OP.Agency.Id"] != null) ? ConfigurationManager.AppSettings["OP.Agency.Id"] : string.Empty,
                Name = ConfigurationManager.AppSettings["OP.Agency.Name"],
                ConnectionString = ConfigurationManager.AppSettings["OP.Agency.Connection"]
            };

            string agencyConfig = Path.Combine(HttpContext.Current.Server.MapPath("/"), "agency.config");
            if (File.Exists(agencyConfig))
            {
                this.agency.Configuration = File.ReadAllText(agencyConfig);
            }

            string adapterTypeName = ConfigurationManager.AppSettings["OP.Agency.Adapter"];
            if (adapterTypeName == null)
            {
                throw new ConfigurationErrorsException("OP.Agency.Adapter is a required configuration setting.");
            }

            this.adapterType = Type.GetType(adapterTypeName);
            if (this.adapterType == null)
            {
                throw new ArgumentException("OP.Agency.Adapter could not be loaded.");
            }

            if (!typeof(IOpenPermitAdapter).IsAssignableFrom(this.adapterType))
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

            context.Agency = this.agency;
            var adapter = (IOpenPermitAdapter)Activator.CreateInstance(this.adapterType, new object[] { context });
            return adapter;
        }
    }
}
