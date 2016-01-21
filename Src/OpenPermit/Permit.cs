using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace OpenPermit
{
    /// <summary>
    /// Class the encapsulates the BSDL Permit Data Structure.
    /// All Fields are required unless specified by additional comments.
    /// </summary>
    public class Permit
    {
        public string PermitNum { get; set; }

        public string Description { get; set; }

        public DateTime? IssuedDate { get; set; }

        public DateTime? CompletedDate { get; set; }

        public string StatusCurrent { get; set; }

        public string OriginalAddress1 { get; set; }

        public string OriginalAddress2 { get; set; }

        public string OriginalCity { get; set; }

        public string OriginalState { get; set; }

        public string OriginalZip { get; set; }

        public string Jurisdiction { get; set; }

        public string PermitClass { get; set; }

        public string PermitClassMapped { get; set; }

        public string StatusCurrentMapped { get; set; }

        public DateTime? AppliedDate { get; set; }

        public string WorkClass { get; set; }

        public string WorkClassMapped { get; set; }

        public string PermitType { get; set; }

        public string PermitTypeMapped { get; set; }

        public string PermitTypeDesc { get; set; }

        public DateTime? StatusDate { get; set; }

        public int TotalSqFt { get; set; }

        public string Link { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public double EstProjectCost { get; set; }

        public int HousingUnits { get; set; }

        public string PIN { get; set; }

        public string ContractorCompanyName { get; set; }

        public string ContractorTrade { get; set; }

        public string ContractorTradeMapped { get; set; }

        public string ContractorLicNum { get; set; }

        public string ContractorStateLic { get; set; }

        public string ProposedUse { get; set; }

        public int AddedSqFt { get; set; }

        public int RemovedSqFt { get; set; }

        public string MasterPermitNum { get; set; }

        public DateTime? ExpiresDate { get; set; }

        public DateTime? COIssuedDate { get; set; }

        public DateTime? HoldDate { get; set; }

        public DateTime? VoidDate { get; set; }

        public string ProjectName { get; set; }

        public string ProjectId { get; set; }

        public int TotalFinishedSqFt { get; set; }

        public int TotalUnfinishedSqFt { get; set; }

        public int TotalHeatedSqFt { get; set; }

        public int TotalUnheatedSqFt { get; set; }

        public int TotalAccSqFt { get; set; }

        public int TotalSprinkledSqFt { get; set; }

        public object ExtraFields { get; set; }

        public string Publisher { get; set; }

        public double Fee { get; set; }

        public string ContractorFullName { get; set; }

        public string ContractorCompanyDesc { get; set; }

        public string ContractorPhone { get; set; }

        public string ContractorAddress1 { get; set; }

        public string ContractorAddress2 { get; set; }

        public string ContractorCity { get; set; }

        public string ContractorState { get; set; }

        public string ContractorZip { get; set; }

        public string ContractorEmail { get; set; }
    }

    /// <summary>
    /// Structure holding the BLDS Permit Status.
    /// Use comments field for additional status description
    /// </summary>
    public class PermitStatus
    {
        public string PermitNum { get; set; }

        public string StatusPrevious { get; set; }

        public DateTime? StatusPreviousDate { get; set; }

        public string StatusPreviousMapped { get; set; }

        public string Comments { get; set; }
    }
}
