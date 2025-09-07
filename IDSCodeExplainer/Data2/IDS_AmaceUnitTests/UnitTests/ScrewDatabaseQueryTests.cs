using IDS.Core.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Testing.UnitTests
{
    [DeploymentItem(@"UnitTestData", @"UnitTestData")]
    [TestClass]
    public class ScrewDatabaseQueryTests
    {
        [TestMethod]
        public void ScrewDatabaseQuery_GetDefaultScrewBrand()
        {
            //arrange
            var resource = new TestResources();
            var query = new ScrewDatabaseQuery(resource.ScrewDatabaseXmlPath);
            const string expectedDefaultScrewBrand = "AO";

            //act
            var defaultScrewBrand = query.GetDefaultScrewBrand();

            //assert
            Assert.AreEqual(expectedDefaultScrewBrand, defaultScrewBrand);
        }

        [TestMethod]
        public void ScrewDatabaseQuery_GetAvailableScrewBrands()
        {
            //arrange
            var resource = new TestResources();
            var query = new ScrewDatabaseQuery(resource.ScrewDatabaseXmlPath);
            var expectedAvailableScrewBrands = new List<string>
            {
                "AO",
                "MBS",
                "CUSTOMA",
                "CUSTOMB"
            };

            //act
            var availableScrewBrands = query.GetAvailableScrewBrands();

            //assert
            var containsAll = expectedAvailableScrewBrands.TrueForAll(brand => availableScrewBrands.Contains(brand));
            Assert.IsTrue(containsAll);
        }

        [TestMethod]
        public void ScrewDatabaseQuery_GetDefaultScrewType()
        {
            //arrange
            var resource = new TestResources();
            var query = new ScrewDatabaseQuery(resource.ScrewDatabaseXmlPath);
            const string screwBrand = "AO";
            const string expectedDefaultScrewType = "D65";

            //act
            var defaultScrewType = query.GetDefaultScrewType(screwBrand);

            //assert
            Assert.AreEqual(expectedDefaultScrewType, defaultScrewType);
        }

        [TestMethod]
        public void ScrewDatabaseQuery_GetAvailableScrewTypes()
        {
            //arrange
            var resource = new TestResources();
            var query = new ScrewDatabaseQuery(resource.ScrewDatabaseXmlPath);
            const string screwBrand = "AO";
            var expectedAvailableScrewTypes = new List<string>
            {
                "D65",
                "D45"
            };

            //act
            var availableScrewTypes = query.GetAvailableScrewTypes(screwBrand);

            //assert
            var containsAll = expectedAvailableScrewTypes.TrueForAll(brand => availableScrewTypes.Contains(brand));
            Assert.IsTrue(containsAll);
        }

        [TestMethod]
        public void ScrewDatabaseQuery_GetAvailableScrewLengths()
        {
            //arrange
            var resource = new TestResources();
            var query = new ScrewDatabaseQuery(resource.ScrewDatabaseXmlPath);
            const string screwBrand = "AO";
            const string screwType = "D65";
            var expectedAvailableScrewLengths = MathUtilities.Range(10.0, 200.0, 1.0).ToList();

            //act
            var availableScrewLengths = query.GetAvailableScrewLengths(screwBrand, screwType);

            //assert
            var containsAll = expectedAvailableScrewLengths.TrueForAll(length => availableScrewLengths.Contains(length));
            Assert.IsTrue(containsAll);
        }

        [TestMethod]
        public void ScrewDatabaseQuery_GetAvailableScrewLengths_FromTestFile()
        {
            //arrange
            var resource = new TestResources();
            var query = new ScrewDatabaseQuery(resource.TestScrewDatabaseXmlPath);
            const string screwBrand = "SCREWBRANDA";
            const string screwType = "D65NL";
            var expectedAvailableScrewLengths = MathUtilities.Range(10.0, 100.0, 10.0).ToList();
            expectedAvailableScrewLengths.Add(55.5);

            //act
            var availableScrewLengths = query.GetAvailableScrewLengths(screwBrand, screwType);

            //assert
            var containsAll = expectedAvailableScrewLengths.TrueForAll(length => availableScrewLengths.Contains(length));
            Assert.IsTrue(containsAll);
        }
    }
}
