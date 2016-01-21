using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace OpenPermit.Socrata.Tests
{
    [TestClass]
    public class SocrataOpenPermitAdapterTests
    {
        [TestMethod]
        public void TestSocrataPermitSearch()
        {
            IOpenPermitAdapter adapter = new SocrataOpenPermitAdapter();
            Page page = new Page();
            page.Offset = 0;
            page.Limmit = 10;
            PermitFilter filter = new PermitFilter();
            filter.Page = page;
            List<Permit> permits = adapter.SearchPermits(filter);

            Assert.AreEqual(permits.Count, 10);
        }
    }
}
