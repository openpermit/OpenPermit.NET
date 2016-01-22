using System;

namespace OpenPermit
{
    public struct StatusMapping
    {
        public string Status { get; set; }

        public string StatusMapped { get; set; }
    }

    public struct PermitTypeMapping
    {
        public string PermitType { get; set; }

        public string PermitTypeMapped { get; set; }
    }

    public struct PermitClassMapping
    {
        public string PermitClass { get; set; }

        public string PermitClassMapped { get; set; }
    }

    public struct WorkClassMapping
    {
        public string WorkClass { get; set; }

        public string WorkClassMapped { get; set; }
    }

    public struct ContractorTradeMapping
    {
        public string ContractorTrade { get; set; }

        public string ContractorTradeMapped { get; set; }
    }

    public struct TimelineMapping
    {
        public string StatusPreviousMapped { get; set; }

        public string StatusPrevious { get; set; }
    }

    public struct InspectionResultMapping
    {
        public string Result { get; set; }

        public string ResultMapped { get; set; }
    }

    public struct InspectionTypeMapping
    {
        public string InspectionType { get; set; }

        public string InspectionTypeMapped { get; set; }
    }
}
