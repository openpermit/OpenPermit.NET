using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace OpenPermit
{
    public enum FieldChoices
    {
        Geo = 1,
        Recommended = 2,
        Optional = 4,
        All = Geo | Recommended | Optional
    }

    public enum TypeChoices
    {
        Master,
        Building,
        Electrical,
        Plumbing,
        Mechanical,
        Fire
    }

    public enum StatusChoices
    {
        [EnumMember(Value = "Application Acceptance")]
        Applied,
        [EnumMember(Value = "In Review")]
        PlanReview,
        [EnumMember(Value = "Permit Issued")]
        Issued,
        [EnumMember(Value = "Inspection Phase")]
        Inspections,
        [EnumMember(Value = "Permit Finaled")]
        Closed,
        [EnumMember(Value = "Permit Cancelled")]
        Expired
    }

    public interface IOpenPermitAdapter
    {
        #region Permits
        List<Permit> SearchPermits(PermitFilter filter);

        Permit GetPermit(string permitNumber);

        List<PermitStatus> GetPermitTimeline(string permitNumber);
        #endregion

        #region Inspections
        List<Inspection> GetInspections(string permitNumber);

        Inspection GetInspection(string permitNumber, string inspectionId);

        Attachment GetInspectionAttachment(string permitNumber, string inspectionId, string attachmentId);
        #endregion

        #region Contractors
        List<Contractor> GetContractors(string permitNumber);

        Contractor GetContractor(string permitNumber, string contractorId);
        #endregion
    }

    public struct Agency
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Latitude { get; set; }

        public string Longitude { get; set; }

        public string Logo { get; set; }

        public string Address { get; set; }

        public string Lastline { get; set; }

        public string Web { get; set; }

        public string Phone { get; set; }

        public string Fax { get; set; }

        public string Email { get; set; }
    }

    public struct PermitFilter
    {
        public string PermitNumber { get; set; }

        public string Address { get; set; }

        public Box BoundingBox { get; set; }

        public List<TypeChoices> Types { get; set; }

        public FieldChoices Fields { get; set; }

        public Page Page { get; set; }

        public List<StatusChoices> Status { get; set; }

        public Tuple<StatusChoices, DateTime, DateTime> TimeFrame { get; set; }
    }

    public struct PermitType
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public Agency Agency { get; set; }

        public string Description { get; set; }

        public string Condition { get; set; }
    }

    public struct Attachment
    {
        public Stream Content { get; set; }

        public string ContentType { get; set; }
    }

    public class Box
    {
        public double MinX { get; set; }

        public double MinY { get; set; }

        public double MaxX { get; set; }

        public double MaxY { get; set; }
    }

    public class Page
    {
        public int Offset { get; set; }

        public int Limmit { get; set; }
    }

    public class Inspection
    {
        public string PermitNum { get; set; }

        public string InspType { get; set; }

        public string InspTypeMapped { get; set; }

        public string Result { get; set; }

        public string ResultMapped { get; set; }

        public DateTime? ScheduledDate { get; set; }

        public DateTime? InspectedDate { get; set; }

        public string InspectionNotes { get; set; }

        public string Description { get; set; }

        // TODO should this be bool and the have Json.NET serialize as 0/1?
        public int Final { get; set; }

        public DateTime? RequestDate { get; set; }

        public DateTime? DesiredDate { get; set; }

        public int ReInspection { get; set; }

        public string Inspector { get; set; }

        public object ExtraFields { get; set; }

        public string Id { get; set; }
    }

    public class Contractor
    {
        public string CompanyName { get; set; }

        public string Trade { get; set; }

        public string TradeMapped { get; set; }

        public string LicenseNumber { get; set; }

        public string StateLicensed { get; set; }

        public string FullName { get; set; }

        public string CompanyDescription { get; set; }

        public string Phone { get; set; }

        public string Address1 { get; set; }

        public string Address2 { get; set; }

        public string City { get; set; }

        public string State { get; set; }

        public string Zip { get; set; }

        public string Email { get; set; }
    }
}
