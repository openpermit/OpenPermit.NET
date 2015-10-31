using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using PetaPoco;

namespace OpenPermit.SQL.Tests
{

    [TestClass]
    public class SQLOpenPermitAdapterTests
    {
        [TestMethod]
        public void TestNoMatchByAddress()
        {
            PermitFilter filter = new PermitFilter();
            filter.Address = "9672 SW 158th Ave Miami, FL 33196";
            IOpenPermitAdapter adapter = new SQLOpenPermitAdpater();

            List<Permit> permits = adapter.SearchPermits(filter);

            Assert.AreEqual(permits.Count, 0);

        }

        [TestMethod]
        public void TestMatchBadAddress()
        {
            PermitFilter filter = new PermitFilter();
            filter.Address = "WE$R@cdfg45";
            IOpenPermitAdapter adapter = new SQLOpenPermitAdpater();

            List<Permit> permits = adapter.SearchPermits(filter);

            Assert.IsNull(permits);

        }

        [TestMethod]
        public void TestMatchByAddress()
        {
            PermitFilter filter = new PermitFilter();
            filter.Address = "825 NW 129 Ave Miami, FL 33182";
            IOpenPermitAdapter adapter = new SQLOpenPermitAdpater();

            List<Permit> permits = adapter.SearchPermits(filter);

            Assert.AreEqual(permits.Count, 1);
        }

        private List<Permit> GetBoxPermits(List<TypeChoices> types, FieldChoices fields = FieldChoices.All)
        {
            PermitFilter filter = new PermitFilter();
            Box box = new Box();

            box.MinX = -81;
            box.MaxX = -80;
            box.MinY = 25;
            box.MaxY = 26;
            filter.BoundingBox = box;
            filter.Types = types;
            filter.Fields = fields;
            IOpenPermitAdapter adapter = new SQLOpenPermitAdpater();
            List<Permit> permits = adapter.SearchPermits(filter);

            return permits;
        }

        [TestMethod]
        public void TestMatchByBox()
        {

            List<Permit> permits = this.GetBoxPermits(null);
            Assert.AreEqual(30, permits.Count);            
        }

        [TestMethod]
        public void TestMatchByBoxAndGeo()
        {
            List<Permit> permits = this.GetBoxPermits(null, FieldChoices.Geo);
            Assert.AreEqual(30, permits.Count);
            Assert.IsNull(permits[0].Description);
        }

        [TestMethod]
        public void TestMatchByBoxAndRecommended()
        {
            List<Permit> permits = this.GetBoxPermits(null, FieldChoices.Recommended);
            Assert.AreEqual(30, permits.Count);
            Assert.IsNotNull(permits[0].Description);
            Assert.IsNull(permits[0].ProposedUse);
        }

        [TestMethod]
        public void TestMatchByBoxAndOptional()
        {
            List<Permit> permits = this.GetBoxPermits(null, FieldChoices.Optional);
            Assert.AreEqual(30, permits.Count);
            Assert.IsNotNull(permits[0].Description);
            Assert.IsNotNull(permits[0].ProposedUse);
        }

        [TestMethod]
        public void TestMatchByBoxAndMaster()
        {
            List<Permit> permits = this.GetBoxPermits(new List<TypeChoices>(new TypeChoices[] { TypeChoices.Master }));
            Assert.AreEqual(15, permits.Count);
        }

        [TestMethod]
        public void TestMatchByBoxAndFire()
        {
            List<Permit> permits = this.GetBoxPermits(new List<TypeChoices>(new TypeChoices[] { TypeChoices.Fire }));
            Assert.AreEqual(6, permits.Count);
        }

        [TestMethod]
        public void TestMatchByBoxAndBuilding()
        {
            List<Permit> permits = this.GetBoxPermits(new List<TypeChoices>(new TypeChoices[] { TypeChoices.Building }));
            Assert.AreEqual(6, permits.Count);
        }

        [TestMethod]
        public void TestMatchByBoxAndPlumbing()
        {
            List<Permit> permits = this.GetBoxPermits(new List<TypeChoices>(new TypeChoices[] { TypeChoices.Plumbing }));
            Assert.AreEqual(6, permits.Count);
        }

        [TestMethod]
        public void TestMatchByBoxAndElectrical()
        {
            List<Permit> permits = this.GetBoxPermits(new List<TypeChoices>(new TypeChoices[] { TypeChoices.Electrical }));
            Assert.AreEqual(6, permits.Count);
        }

        [TestMethod]
        public void TestMatchByBoxAndMechanical()
        {
            List<Permit> permits = this.GetBoxPermits(new List<TypeChoices>(new TypeChoices[] { TypeChoices.Mechanical }));
            Assert.AreEqual(6, permits.Count);
        }

        public void TestBadBox()
        {
            PermitFilter filter = new PermitFilter();
            Box box = new Box();

            box.MinX = -85;
            box.MaxX = -84;
            box.MinY = 20;
            box.MaxY = 21;
            filter.BoundingBox = box;
            IOpenPermitAdapter adapter = new SQLOpenPermitAdpater();
            List<Permit> permits = adapter.SearchPermits(filter);
            Assert.AreEqual(0, permits.Count);
        }

        [TestMethod]
        public void TestNoFilter()
        {
            IOpenPermitAdapter adapter = new SQLOpenPermitAdpater();
            List<Permit> permits = adapter.SearchPermits(new PermitFilter());
            Assert.AreEqual(30, permits.Count);

        }

        [TestMethod]
        public void TestNoFilterMatchMechanical()
        {   
            PermitFilter filter = new PermitFilter();
            filter.Types = new List<TypeChoices>(new TypeChoices[] { TypeChoices.Mechanical });
            IOpenPermitAdapter adapter = new SQLOpenPermitAdpater();
            List<Permit> permits = adapter.SearchPermits(filter);
            Assert.AreEqual(6, permits.Count);

        }

        [TestMethod]
        public void TestNoFilterMatchStatusClosed()
        {
            PermitFilter filter = new PermitFilter();
            filter.Status = new List<StatusChoices>(new StatusChoices[] { StatusChoices.Closed });
            IOpenPermitAdapter adapter = new SQLOpenPermitAdpater();
            List<Permit> permits = adapter.SearchPermits(filter);
            Assert.AreEqual(7, permits.Count);

        }

        [TestMethod]
        public void TestNoFilterMatchTimeFrameClosed()
        {
            PermitFilter filter = new PermitFilter();
            filter.TimeFrame = new Tuple<StatusChoices, DateTime, DateTime>(StatusChoices.Closed,
                Convert.ToDateTime("01/01/2013"), Convert.ToDateTime("01/01/2014"));
            IOpenPermitAdapter adapter = new SQLOpenPermitAdpater();
            List<Permit> permits = adapter.SearchPermits(filter);
            Assert.AreEqual(8, permits.Count);

        }

        [TestMethod]
        public void TestGetExistingPermit()
        {
            IOpenPermitAdapter adapter = new SQLOpenPermitAdpater();
            Permit permit = adapter.GetPermit("PERMNUM_17");
            Assert.IsNotNull(permit);
            Assert.AreEqual("34RT56890317", permit.ContractorLicNum);

        }

        [TestMethod]
        public void TestGetBadPermit()
        {
            IOpenPermitAdapter adapter = new SQLOpenPermitAdpater();
            Permit permit = adapter.GetPermit("PERMNUM_45");
            Assert.IsNull(permit);
        }

        [TestMethod]
        public void TestGetBadPermitTimeline()
        {
            IOpenPermitAdapter adapter = new SQLOpenPermitAdpater();
            List<PermitStatus> timeline = adapter.GetPermitTimeline("PERMNUM_45");
            Assert.AreEqual(timeline.Count, 0);
        }

        [TestMethod]
        public void TestGetPermitTimeline()
        {
            IOpenPermitAdapter adapter = new SQLOpenPermitAdpater();
            List<PermitStatus> timeline = adapter.GetPermitTimeline("PERMNUM_15");
            Assert.AreEqual(timeline.Count, 10);
        }

        [TestMethod]
        public void TestGetBadInspections()
        {
            IOpenPermitAdapter adapter = new SQLOpenPermitAdpater();
            List<Inspection> inspections = adapter.GetInspections("PERMNUM_45");
            Assert.AreEqual(inspections.Count, 0);
        }

        [TestMethod]
        public void TestGetPermitInspections()
        {
            IOpenPermitAdapter adapter = new SQLOpenPermitAdpater();
            List<Inspection> inspections = adapter.GetInspections("PERMNUM_15");
            Assert.AreEqual(inspections.Count, 10);
        }

        [TestMethod]
        public void TestGetBadInspection()
        {
            IOpenPermitAdapter adapter = new SQLOpenPermitAdpater();
            Inspection inspection = adapter.GetInspection("PERMNUM_15", "-1");
            Assert.IsNull(inspection);
        }

        [TestMethod]
        public void TestGetPermitInspection()
        {
            IOpenPermitAdapter adapter = new SQLOpenPermitAdpater();
            List<Inspection> inspections = adapter.GetInspections("PERMNUM_15");
            Inspection refInspection = inspections[5];
            Inspection inspection = adapter.GetInspection("PERMNUM_15", refInspection.Id);
            Assert.IsNotNull(inspection);
            Assert.AreEqual(refInspection.Inspector, inspection.Inspector);
        }


        [ClassCleanup]
        static public void CleanupTestDB()
        {
            Database db = new Database("openpermit");
            db.Execute("DELETE FROM Permit");
            db.Execute("DELETE FROM PermitStatus");
            db.Execute("DELETE FROM Inspection");

        }


        [ClassInitialize]
        static public void PopulateTestDB(TestContext ctx)
        {
            Database db = new Database("openpermit");
            string[] types = new string[] { "BLDG", "FIRE", "ELEC", "MECH", "PLUM" };
            string[] currStatus = new string[] { "APPLIED", "ISSUED", "CLOSED", "EXPIRED" };
            string[] statusMapped = new string[] { "Application Accepted", "Permit Issued", "Permit Finaled", "Permit Cancelled" };
            string[] applied = new string[] { "01/01/2012", "01/01/2013", "01/01/2014", "01/01/2015" };
            string[] issued = new string[] { "01/02/2012", "01/02/2013", "01/02/2014", "01/02/2015" };
            string[] closed = new string[] { "03/01/2012", "03/01/2013", "03/01/2014", "03/01/2015" };
            string[] expired = new string[] { "06/01/2012", "06/01/2013", "06/01/2014", "06/01/2015" };
            for (int i = 0; i < 30; i++)
            {
                Permit permit = new Permit();
                permit.AddedSqFt = i;
                permit.MasterPermitNum = (i % 2).ToString();
                permit.AppliedDate = Convert.ToDateTime(applied[i % 4]);
                permit.COIssuedDate = DateTime.Now;
                permit.CompletedDate = Convert.ToDateTime(closed[i % 4]);
                permit.ContractorAddress1 = String.Format("29{0} SW {1} Ave", i, i + 6);
                permit.ContractorAddress2 = "Address2_" + i.ToString();
                permit.ContractorCity = "Miami";
                permit.ContractorCompanyDesc = "Company Description " + i.ToString();
                permit.ContractorCompanyName = "Company Name " + i.ToString();
                permit.ContractorEmail = String.Format("Contractor_{0}@aecosoft.com", i);
                permit.ContractorFullName = "ContractorName_" + i.ToString();
                permit.ContractorLicNum = "34RT568903" + i.ToString();
                permit.ContractorPhone = "305-444-55" + (i + 10).ToString();
                permit.ContractorState = "FL";
                permit.ContractorStateLic = "FL5467021";
                permit.ContractorTrade = "ContractorTrade_" + i.ToString();
                permit.ContractorTradeMapped = "ContractrorTradeMapped_" + i.ToString();
                permit.ContractorZip = "331" + (i + 40).ToString();
                permit.Description = "PermitDescription_" + i.ToString();
                permit.EstProjectCost = 30000 + i;
                permit.ExpiresDate = Convert.ToDateTime(expired[i % 4]);
                permit.ExtraFields = "{'blah': 'blue'}";
                permit.Fee = 30 + i;
                permit.HoldDate = DateTime.Now;
                permit.HousingUnits = i;
                permit.IssuedDate = Convert.ToDateTime(issued[i % 4]);
                permit.Jurisdiction = "Miami-Dade";
                permit.Latitude = 25.700189 - (double)i/100;
                permit.Link = String.Format("http://permiturl{0}.com", i);
                permit.Longitude = -80.288020 - (double)i/100;
                permit.OriginalAddress1 = String.Format("8{0} NW 1{1}th Ave", i, i + 4);
                permit.OriginalAddress2 = "OrgAddress2_" + i.ToString();
                permit.OriginalCity = "Miami";
                permit.OriginalState = "FL";
                permit.OriginalZip = "331" + (i + 57).ToString();
                permit.PermitClass = "PERM_" + i.ToString();
                permit.PermitClassMapped = "PERM_" + i.ToString() + "_CLASS";
                permit.PermitNum = "PERMNUM_" + i.ToString();
                permit.PermitType = types[i % 5];
                permit.PermitTypeDesc = "TYPEDESC_" + i.ToString();
                permit.PermitTypeMapped = "TYPEMAPPEDDESC_" + i.ToString();
                permit.PIN = "456" + (10 + i).ToString();
                permit.ProjectId = "PROJID_" + i.ToString();
                permit.ProjectName = "PROJNAME_" + i.ToString();
                permit.ProposedUse = "PORPUSE_" + i.ToString();
                permit.Publisher = "PUBLISH_" + i.ToString();
                permit.RemovedSqFt = i;
                permit.StatusCurrent = currStatus[i % 4];
                permit.StatusCurrentMapped = statusMapped[i % 4];
                permit.StatusDate = DateTime.Now;
                permit.TotalAccSqFt = 10000 + i;
                permit.TotalFinishedSqFt = 5000 + i;
                permit.TotalHeatedSqFt = 5000 + i;
                permit.TotalSprinkledSqFt = 4000 + i;
                permit.TotalSqFt = 2500 + i;
                permit.TotalUnfinishedSqFt = 500 + i;
                permit.TotalUnheatedSqFt = 1000 + i;
                permit.VoidDate = DateTime.Now;
                permit.WorkClass = "WORKCLASS_" + i.ToString();
                permit.WorkClassMapped = "WORKCLASS_" + i.ToString();

                db.Insert("Permit", "PermitNum", false, permit);

                db.Execute("UPDATE Permit SET Location=geography::Point(Latitude, Longitude, 4326)");

            }

            for (int j = 0; j < 10; j++)
            {
                PermitStatus status = new PermitStatus();
                status.PermitNum = "PERMNUM_15";
                status.StatusPrevious = "STATUS_" + j;
                status.StatusPreviousDate = DateTime.Now;
                status.StatusPreviousMapped = "STATUSMAPPED_" + j;
                status.Comments = "COMMENTS_" + j;

                db.Insert("PermitStatus", "id", true, status);

            }

            for (int j = 0; j < 10; j++)
            {
                Inspection inspection = new Inspection();
                inspection.PermitNum = "PERMNUM_15";
                inspection.InspectedDate = DateTime.Now;
                inspection.InspectionNotes = "MYNOTES_" + j;
                inspection.Inspector = "INSPECTOR_" + j;
                inspection.InspType = "TYPE_" + j;
                inspection.InspTypeMapped = "TYPEMAPPED_" + j;
                inspection.ReInspection = 0;
                inspection.RequestDate = DateTime.Now;
                inspection.Result = "PASSED";
                inspection.ResultMapped = "PASSED_MAPPED";
                inspection.ScheduledDate = DateTime.Now;
                inspection.Description = "DESCRIPTION_" + j;
                inspection.DesiredDate = DateTime.Now;
                inspection.ExtraFields = "{'blah': 'blue'}";
                inspection.Final = 1;

                db.Insert("Inspection", "Id", true, inspection);

            }

        }

    }
}
