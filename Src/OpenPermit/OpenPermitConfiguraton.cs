using System;

namespace OpenPermit
{
    public struct StatusMapping
    {
        public string Status { get; set; }
        public string StatusMapped { get; set; }
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
