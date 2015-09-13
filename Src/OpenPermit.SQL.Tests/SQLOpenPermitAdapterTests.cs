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
        public void TestBadAddress()
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
            this.PopulateTestDB();

            PermitFilter filter = new PermitFilter();
            filter.Address = "825 NW 129 Ave Miami, FL 33182";
            IOpenPermitAdapter adapter = new SQLOpenPermitAdpater();

            List<Permit> permits = adapter.SearchPermits(filter);

            Assert.AreEqual(permits.Count, 1);

            this.CleanupTestDB();

        }

        private void CleanupTestDB()
        {
            Database db = new Database("openpermit");
            db.Execute("DELETE FROM Permit");

        }


        private void PopulateTestDB()
        {
            Database db = new Database("openpermit");
            for (int i = 0; i < 30; i++)
            {
                Permit permit = new Permit();
                permit.AddedSqFt = i;
                permit.AppliedDate = DateTime.Now;
                permit.COIssuedDate = DateTime.Now;
                permit.CompletedDate = DateTime.Now;
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
                permit.ExpiresDate = DateTime.Now;
                permit.ExtraFields = "{'blah': 'blue'}";
                permit.Fee = 30 + i;
                permit.HoldDate = DateTime.Now;
                permit.HousingUnits = i;
                permit.IssuedDate = DateTime.Now;
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
                permit.PermitType = "PERMTYPE_" + i.ToString();
                permit.PermitTypeDesc = "TYPEDESC_" + i.ToString();
                permit.PermitTypeMapped = "TYPEMAPPEDDESC_" + i.ToString();
                permit.PIN = "456" + (10 + i).ToString();
                permit.ProjectId = "PROJID_" + i.ToString();
                permit.ProjectName = "PROJNAME_" + i.ToString();
                permit.ProposedUse = "PORPUSE_" + i.ToString();
                permit.Publisher = "PUBLISH_" + i.ToString();
                permit.RemovedSqFt = i;
                permit.StatusCurrent = "STATUS_" + i.ToString();
                permit.StatusCurrentMapped = "STATUSMAPPED_" + i.ToString();
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

            }

        }

    }
}
