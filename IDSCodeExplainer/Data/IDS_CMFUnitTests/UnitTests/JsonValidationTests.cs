using IDS.CMF.CasePreferences;
using IDS.CMF.DataModel;
using IDS.CMF.Query;
using IDS.CMF.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class JsonValidationTests
    {
        [TestMethod]
        public void Screw_Length_Json_Validation_Test()
        {
            var screwTypes = new List<string>();
            var screwLengthsData = CasePreferencesHelper.LoadScrewLengthData();
            foreach (var screwLengthData in screwLengthsData.ScrewLengths)
            {
                Assert.IsFalse(screwTypes.Contains(screwLengthData.ScrewType), $"\"{screwLengthData.ScrewType}\" is duplicated in ScrewLength.json");

                screwTypes.Add(screwLengthData.ScrewType);
            }
        }

        [TestMethod]
        public void Pastille_Diameter_Same_For_All_Matrix_Mandible_Screws_Synthes()
        {
            //Arrange
            var eScrewBrand = "Synthes";
            var expectedPastilleDiameter = 6.2;
            var subString = "Matrix Mandible";

            var screwBrandCasePreferencesInfo = CasePreferencesHelper.LoadScrewBrandCasePreferencesInfo(Converter.ToEScrewBrandType(eScrewBrand));

            //Act
            AssertEqualityOfPastilleDiameterOfScrewType(screwBrandCasePreferencesInfo, expectedPastilleDiameter, subString);
        }

        [TestMethod]
        public void Pastille_Diameter_Same_For_All_Matrix_Mandible_Screws_SynthesUsCanada()
        {
            //Arrange
            var eScrewBrand = "SynthesUsCanada";
            var expectedPastilleDiameter = 6.2;
            var subString = "Matrix Mandible";

            var screwBrandCasePreferencesInfo = CasePreferencesHelper.LoadScrewBrandCasePreferencesInfo(Converter.ToEScrewBrandType(eScrewBrand));

            //Act
            AssertEqualityOfPastilleDiameterOfScrewType(screwBrandCasePreferencesInfo, expectedPastilleDiameter, subString);
        }

        private void AssertEqualityOfPastilleDiameterOfScrewType(ScrewBrandCasePreferencesInfo screwBrandCasePreferencesInfo,double expectedPastilleDiameter, string screwTypeSubString)
        {

            foreach (var implantPreference in screwBrandCasePreferencesInfo.Implants)
            {
                var screwPreference = implantPreference.Screw;
                foreach (var screw in screwPreference)
                {
                    var screwType = screw.ScrewType.ToLower();
                    if (screwType.Contains(screwTypeSubString))
                    {
                        var actualPastilleDiameter =
                            Queries.PastilleDiameter(screwBrandCasePreferencesInfo.ScrewBrand, implantPreference.ImplantType, screw.ScrewType);

                        //Assert
                        Assert.AreEqual(expectedPastilleDiameter, actualPastilleDiameter);
                    }
                }
            }
        }
    }
}
