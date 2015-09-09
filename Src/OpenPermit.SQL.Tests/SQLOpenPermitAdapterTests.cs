using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

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
    }
}
