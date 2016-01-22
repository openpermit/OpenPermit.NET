using System;
using System.Collections.Generic;

using OpenPermit;

namespace OpenPermit.Accela
{
    public enum PermitClassField
    {
        RecordType,
        RecordSubType
    }

    public enum WorkClassField
    {
        RecordSubType,
        RecordType,
        ConstructType
    }

    public class AgencyConfiguration
    {
        public string[] Modules { get; set; }

        public List<StatusMapping> Status { get; set; }

        public List<PermitTypeMapping> PermitType { get; set; }

        public PermitClassField PermitClassField { get; set; }

        public List<PermitClassMapping> PermitClass { get; set; }

        public WorkClassField WorkClassField { get; set; }

        public List<WorkClassMapping> WorkClass { get; set; }

        public List<ContractorTradeMapping> ContractorTrade { get; set; }

        public List<TimelineMapping> Timeline { get; set; }

        public List<InspectionTypeMapping> InspectionType { get; set; }

        public List<InspectionResultMapping> InspectionResult { get; set; }
    }
}
