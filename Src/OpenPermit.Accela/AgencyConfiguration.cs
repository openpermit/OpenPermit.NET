using System;
using System.Collections.Generic;

using OpenPermit;

namespace OpenPermit.Accela
{
    public class AgencyConfiguration
    {
        public string[] Modules { get; set; }

        public List<StatusMapping> Status { get; set; }

        public List<TimelineMapping> Timeline { get; set; }

        public List<InspectionTypeMapping> InspectionType { get; set; }

        public List<InspectionResultMapping> InspectionResult { get; set; }
    }
}
