using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class ScrewBrandTypeTests
    {
        [TestMethod]
        public void ScrewBrandType_MBS()
        {
            //arrange
            const string brand = "MBS";
            const double diameter = 6.5;
            const ScrewLocking locking = ScrewLocking.NonLocking;

            //act
            var brandType = new ScrewBrandType(brand, diameter, locking);

            //assert
            Assert.AreEqual(brandType.Type, "D65NL");
            Assert.AreEqual($"{brandType}", "MBS_D65NL");
        }

        [TestMethod]
        public void ScrewBrandType_CUSTOMA()
        {
            //arrange
            const string brand = "CUSTOMA";
            const double diameter = 50.5;
            const ScrewLocking locking = ScrewLocking.Locking;

            //act
            var brandType = new ScrewBrandType(brand, diameter, locking);

            //assert
            Assert.AreEqual(brandType.Type, "D505L");
            Assert.AreEqual($"{brandType}", "CUSTOMA_D505L");
        }

        [TestMethod]
        public void ScrewBrandType_CUSTOMB()
        {
            //arrange
            const string brand = "CUSTOMB";
            const double diameter = 6.0;
            const ScrewLocking locking = ScrewLocking.None;

            //act
            var brandType = new ScrewBrandType(brand, diameter, locking);

            //assert
            Assert.AreEqual(brandType.Type, "D60");
            Assert.AreEqual($"{brandType}", "CUSTOMB_D60");
        }

        [TestMethod]
        public void ScrewBrandType_TryParse_Diameter()
        {
            //arrange
            const string screwBrandTypeName = "CUSTOMC_D505";
            const string brand = "CUSTOMC";
            const double diameter = 5.05;
            const ScrewLocking locking = ScrewLocking.None;

            //act
            ScrewBrandType brandType;
            var parsedSuccessfully = ScrewBrandType.TryParse(screwBrandTypeName, out brandType);

            //assert
            Assert.IsTrue(parsedSuccessfully);
            Assert.AreEqual(brandType.Brand, brand);
            Assert.AreEqual(brandType.Diameter, diameter);
            Assert.AreEqual(brandType.Locking, locking);
        }
    }
}
