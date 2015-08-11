using System;
using System.Collections.Generic;

namespace OpenPermit
{
    public class OpenPermitAgency
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ConnectionString { get; set; }
        public string Configuration { get; set; }
    }

    public class OpenPermitContext
    {
        public OpenPermitAgency Agency { get; set; }

        //Any Additional properties can be passed here.
        public Dictionary<string, object> CustomProperties { get; set; }

    }
}
