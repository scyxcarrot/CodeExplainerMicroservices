using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class ScrewBrandTypeConverterTests
    {
        [TestMethod]
        public void ScrewBrandTypeConverter_ConvertFrom_ScrewType_AO_D45()
        {
            //arrange
            const ScrewType screwType = ScrewType.AO_D45;
            const string expectedBrand = "AO";
            const double expectedDiameter = 4.5;
            const string expectedType = "D45";
            const string expectedName = "AO_D45";

            //act
            var brandType = ScrewBrandTypeConverter.ConvertFromScrewType(screwType);

            //assert
            Assert.AreEqual(expectedBrand, brandType.Brand);
            Assert.AreEqual(expectedDiameter, brandType.Diameter);
            Assert.AreEqual(expectedType, brandType.Type);
            Assert.AreEqual(expectedName, $"{brandType}");
        }

        [TestMethod]
        public void ScrewBrandTypeConverter_ConvertFrom_ScrewType_AO_D65()
        {
            //arrange
            const ScrewType screwType = ScrewType.AO_D65;
            const string expectedBrand = "AO";
            const double expectedDiameter = 6.5;
            const string expectedType = "D65";
            const string expectedName = "AO_D65";

            //act
            var brandType = ScrewBrandTypeConverter.ConvertFromScrewType(screwType);

            //assert
            Assert.AreEqual(expectedBrand, brandType.Brand);
            Assert.AreEqual(expectedDiameter, brandType.Diameter);
            Assert.AreEqual(expectedType, brandType.Type);
            Assert.AreEqual(expectedName, $"{brandType}");
        }

        [TestMethod]
        [ExpectedException(typeof(System.Exception))]
        public void ScrewBrandTypeConverter_ConvertFrom_ScrewType_Invalid()
        {
            //arrange
            const ScrewType screwType = ScrewType.Invalid;

            //act
            var brandType = ScrewBrandTypeConverter.ConvertFromScrewType(screwType);

            //assert - exception
        }

        [TestMethod]
        public void ScrewBrandTypeConverter_ConvertTo_ScrewType_AO_D45()
        {
            //arrange
            var brandType = new ScrewBrandType("AO", 4.5, ScrewLocking.None);
            const ScrewType expectedType = ScrewType.AO_D45;

            //act
            var screwType = ScrewBrandTypeConverter.ConvertToScrewType(brandType);

            //assert
            Assert.AreEqual(expectedType, screwType);
        }

        [TestMethod]
        public void ScrewBrandTypeConverter_ConvertTo_ScrewType_AO_D65()
        {
            //arrange
            var brandType = new ScrewBrandType("AO", 6.5, ScrewLocking.None);
            const ScrewType expectedType = ScrewType.AO_D65;

            //act
            var screwType = ScrewBrandTypeConverter.ConvertToScrewType(brandType);

            //assert
            Assert.AreEqual(expectedType, screwType);
        }

        [TestMethod]
        [ExpectedException(typeof(System.Exception))]
        public void ScrewBrandTypeConverter_ConvertTo_NotSupported_ScrewType()
        {
            //arrange
            var brandType = new ScrewBrandType("MBS", 6.5, ScrewLocking.NonLocking);

            //act
            var screwType = ScrewBrandTypeConverter.ConvertToScrewType(brandType);

            //assert - exception
        }
    }
}
