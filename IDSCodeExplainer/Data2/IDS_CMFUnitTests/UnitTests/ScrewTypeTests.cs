using IDS.CMF.CasePreferences;
using IDS.CMF.Query;
using IDS.CMF.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class ScrewTypeTests
    {
        [TestMethod]
        public void Mini_Slotted_Self_Drilling_Should_Have_Same_Configuration_As_Mini_Slotted_Except_ScrewStyles()
        {
            //arrange
            var screwTypeToTest = "Mini Slotted Self-Drilling";
            var screwTypeToCompareWith = "Mini Slotted";

            //act
            var screwTypeInformationToTest = Queries.GetScrewLength(screwTypeToTest);
            var screwTypeInformationToCompareWith = Queries.GetScrewLength(screwTypeToCompareWith);

            //assert
            AssertEqualityExceptScrewStyle(screwTypeInformationToTest, screwTypeInformationToCompareWith);
        }

        [TestMethod]
        public void Mini_Slotted_Self_Tapping_Should_Have_Same_Configuration_As_Mini_Slotted_Except_ScrewStyles()
        {
            //arrange
            var screwTypeToTest = "Mini Slotted Self-Tapping";
            var screwTypeToCompareWith = "Mini Slotted";

            //act
            var screwTypeInformationToTest = Queries.GetScrewLength(screwTypeToTest);
            var screwTypeInformationToCompareWith = Queries.GetScrewLength(screwTypeToCompareWith);

            //assert
            AssertEqualityExceptScrewStyle(screwTypeInformationToTest, screwTypeInformationToCompareWith);
        }

        [TestMethod]
        public void Mini_Crossed_Self_Drilling_Should_Have_Same_Configuration_As_Mini_Crossed_Except_ScrewStyles()
        {
            //arrange
            var screwTypeToTest = "Mini Crossed Self-Drilling";
            var screwTypeToCompareWith = "Mini Crossed";

            //act
            var screwTypeInformationToTest = Queries.GetScrewLength(screwTypeToTest);
            var screwTypeInformationToCompareWith = Queries.GetScrewLength(screwTypeToCompareWith);

            //assert
            AssertEqualityExceptScrewStyle(screwTypeInformationToTest, screwTypeInformationToCompareWith);
        }

        [TestMethod]
        public void Mini_Crossed_Self_Tapping_Should_Have_Same_Configuration_As_Mini_Crossed_Except_ScrewStyles()
        {
            //arrange
            var screwTypeToTest = "Mini Crossed Self-Tapping";
            var screwTypeToCompareWith = "Mini Crossed";

            //act
            var screwTypeInformationToTest = Queries.GetScrewLength(screwTypeToTest);
            var screwTypeInformationToCompareWith = Queries.GetScrewLength(screwTypeToCompareWith);

            //assert
            AssertEqualityExceptScrewStyle(screwTypeInformationToTest, screwTypeInformationToCompareWith);
        }

        [TestMethod]
        public void Mini_Slotted_Should_Have_Self_Tapping_And_Self_Drilling_ScrewStyle()
        {
            //arrange
            var screwTypeToTest = "Mini Slotted";

            //act
            var screwStyleInformationToTest = Queries.GetScrewLength(screwTypeToTest).Styles;

            //assert
            AssertScrewHasBothSelfDrillingAndSelfTappingStyle(screwStyleInformationToTest);
        }

        [TestMethod]
        public void Mini_Crossed_Should_Have_Self_Tapping_And_Self_Drilling_ScrewStyle()
        {
            //arrange
            var screwTypeToTest = "Mini Crossed";

            //act
            var screwStyleInformationToTest = Queries.GetScrewLength(screwTypeToTest).Styles;

            //assert
            AssertScrewHasBothSelfDrillingAndSelfTappingStyle(screwStyleInformationToTest);
        }

        [TestMethod]
        public void Mini_Slotted_Self_Drilling_Should_Only_Have_Self_Drilling_ScrewStyle()
        {
            //arrange
            var screwTypeToTest = "Mini Slotted Self-Drilling";

            //act
            var screwStyleInformationToTest = Queries.GetScrewLength(screwTypeToTest).Styles;

            //assert
            AssertScrewOnlyHasSelfDrillingScrewStyle(screwStyleInformationToTest);
        }

        [TestMethod]
        public void Mini_Crossed_Self_Drilling_Should__Only_Have_Self_Drilling_ScrewStyle()
        {
            //arrange
            var screwTypeToTest = "Mini Crossed Self-Drilling";

            //act
            var screwStyleInformationToTest = Queries.GetScrewLength(screwTypeToTest).Styles;

            //assert
            AssertScrewOnlyHasSelfDrillingScrewStyle(screwStyleInformationToTest);
        }

        [TestMethod]
        public void Mini_Slotted_Self_Tapping_Should_Only_Have_Self_Tapping_ScrewStyle()
        {
            //arrange
            var screwTypeToTest = "Mini Slotted Self-Tapping";

            //act
            var screwStyleInformationToTest = Queries.GetScrewLength(screwTypeToTest).Styles;

            //assert
            AssertScrewOnlyHasSelfTappingScrewStyle(screwStyleInformationToTest);
        }

        [TestMethod]
        public void Mini_Crossed_Self_Tapping_Should_Only_Have_Self_Tapping_ScrewStyle()
        {
            //arrange
            var screwTypeToTest = "Mini Crossed Self-Tapping";

            //act
            var screwStyleInformationToTest = Queries.GetScrewLength(screwTypeToTest).Styles;

            //assert
            AssertScrewOnlyHasSelfTappingScrewStyle(screwStyleInformationToTest);
        }

        [TestMethod]
        public void All_Mini_Screw_Types_Have_Correct_Color()
        {
            Test_Screw_Types_Have_Correct_Color("Mini", 32, 79, 247);
        }

        [TestMethod]
        public void All_Micro_Screw_Types_Have_Correct_Color()
        {
            Test_Screw_Types_Have_Correct_Color("Micro", 69, 189, 77);
        }

        private void Test_Screw_Types_Have_Correct_Color(string screwTypeToTest, int expectedBbColorRed, int expectedBbColorGreen, int expectedBbColorBlue)
        {
            //arrange
            var screwLengths = CasePreferencesHelper.LoadScrewLengthData().ScrewLengths;
            var allRelatedScrewTypeNames = screwLengths.Select(sl => sl.ScrewType).Where(st => st.ToLower().Contains(screwTypeToTest.ToLower()));

            foreach (var screwType in allRelatedScrewTypeNames)
            {
                //act
                var bbColorRed = Queries.GetScrewLength(screwType).BbColorRed;
                var bbColorGreen = Queries.GetScrewLength(screwType).BbColorGreen;
                var bbColorBlue = Queries.GetScrewLength(screwType).BbColorBlue;

                //assert
                Assert.AreEqual(expectedBbColorRed, bbColorRed, $"Incorrect Red Color for: {screwType}");
                Assert.AreEqual(expectedBbColorGreen, bbColorGreen, $"Incorrect Green Color for: {screwType}");
                Assert.AreEqual(expectedBbColorBlue, bbColorBlue, $"Incorrect Blue Color for: {screwType}");
            }
        }

        private void AssertEquality(ScrewLength screwTypeToTest, ScrewLength screwTypeToCompareWith)
        {
            //other than the name of ScrewType, all information should be same
            AssertEquality(screwTypeToTest.Styles, screwTypeToCompareWith.Styles);
            AssertEqualityExceptScrewStyle(screwTypeToTest, screwTypeToCompareWith);
        }

        private void AssertEqualityExceptScrewStyle(ScrewLength screwTypeToTest, ScrewLength screwTypeToCompareWith)
        {
            Assert.AreEqual(screwTypeToTest.DefaultOrthognathic, screwTypeToCompareWith.DefaultOrthognathic);
            Assert.AreEqual(screwTypeToTest.StampImprintShapeOffset, screwTypeToCompareWith.StampImprintShapeOffset);
            Assert.AreEqual(screwTypeToTest.StampImprintShapeWidth, screwTypeToCompareWith.StampImprintShapeWidth);
            Assert.AreEqual(screwTypeToTest.StampImprintShapeHeight, screwTypeToCompareWith.StampImprintShapeHeight);
            Assert.AreEqual(screwTypeToTest.StampImprintShapeSectionHeightRatio, screwTypeToCompareWith.StampImprintShapeSectionHeightRatio);
            Assert.AreEqual(screwTypeToTest.StampImprintShapeCreationMaxPastilleThickness, screwTypeToCompareWith.StampImprintShapeCreationMaxPastilleThickness);
            Assert.AreEqual(screwTypeToTest.DefaultReconstruction, screwTypeToCompareWith.DefaultReconstruction);
            Assert.AreEqual(screwTypeToTest.DefaultForGuideFixation, screwTypeToCompareWith.DefaultForGuideFixation);
            Assert.AreEqual(screwTypeToTest.QCCylinderDiameter, screwTypeToCompareWith.QCCylinderDiameter);
            Assert.AreEqual(screwTypeToTest.ScrewDiameter, screwTypeToCompareWith.ScrewDiameter);
            Assert.AreEqual(screwTypeToTest.BbColorRed, screwTypeToCompareWith.BbColorRed);
            Assert.AreEqual(screwTypeToTest.BbColorGreen, screwTypeToCompareWith.BbColorGreen);
            Assert.AreEqual(screwTypeToTest.BbColorBlue, screwTypeToCompareWith.BbColorBlue);
            Assert.AreEqual(screwTypeToTest.GuideVicinityClearance, screwTypeToCompareWith.GuideVicinityClearance);
        }

        private void AssertEquality(List<ScrewStyle> screwStylesToTest, List<ScrewStyle> screwStylesToCompareWith)
        {
            Assert.AreEqual(screwStylesToTest.Count, screwStylesToCompareWith.Count);

            var orderedListToTest = screwStylesToTest.OrderBy(s => s.Name).ToList();
            var orderedListToCompareWith = screwStylesToCompareWith.OrderBy(s => s.Name).ToList();

            for (var i = 0; i < screwStylesToCompareWith.Count; i++)
            {
                var screwStyleToTest = orderedListToTest[i];
                var screwStyleToCompareWith = orderedListToCompareWith[i];

                Assert.AreEqual(screwStyleToTest.Name, screwStyleToCompareWith.Name);
                Assert.AreEqual(screwStyleToTest.Lengths.Count, screwStyleToCompareWith.Lengths.Count);

                var orderedLengthsToTest = screwStyleToTest.Lengths.OrderBy(l => l.Key).ToList();
                var orderedLengthsToCompareWith = screwStyleToCompareWith.Lengths.OrderBy(s => s.Key).ToList();

                for (var j = 0; j < screwStyleToCompareWith.Lengths.Count; j++)
                {
                    var screwLengthToTest = orderedLengthsToTest[j];
                    var screwLengthToCompareWith = orderedLengthsToCompareWith[j];

                    Assert.AreEqual(screwLengthToTest.Key, screwLengthToCompareWith.Key);
                    Assert.AreEqual(screwLengthToTest.Value, screwLengthToCompareWith.Value);
                }
            }
        }

        private void AssertOnlyOneScrewStyleExists(List<ScrewStyle> screwStylesToTest)
        {
            Assert.IsTrue(screwStylesToTest.Count == 1);
        }

        private void AssertAllScrewStylesExist(List<ScrewStyle> screwStylesToTest)
        {
            Assert.IsTrue(screwStylesToTest.Count == 2);
        }

        private void AssertScrewOnlyHasSelfTappingScrewStyle(List<ScrewStyle> screwStyleToTest)
        {
            AssertOnlyOneScrewStyleExists(screwStyleToTest);

            var screwStyleNames = screwStyleToTest.Select(screw => screw.Name).ToList();
            AssertSelfTappingStyleExists(screwStyleNames);
        }

        private void AssertScrewHasBothSelfDrillingAndSelfTappingStyle(List<ScrewStyle> screwStylesToTest)
        {
            AssertAllScrewStylesExist(screwStylesToTest);

            var screwStyleNames = screwStylesToTest.Select(screw => screw.Name).ToList();
            AssertSelfTappingStyleExists(screwStyleNames);
            AssertSelfDrillingStyleExists(screwStyleNames);
        }

        private void AssertSelfTappingStyleExists(List<string> screwStyleNames)
        {
            var hasSelfTapping = screwStyleNames.Contains("Self-Tapping");
            Assert.IsTrue(hasSelfTapping);
        }

        private void AssertSelfDrillingStyleExists(List<string> screwStyleNames)
        {
            var hasSelfDrilling = screwStyleNames.Contains("Self-Drilling");
            Assert.IsTrue(hasSelfDrilling);
        }

        private void AssertScrewOnlyHasSelfDrillingScrewStyle(List<ScrewStyle> screwStyleToTest)
        {
            AssertOnlyOneScrewStyleExists(screwStyleToTest);

            var screwStyleName = screwStyleToTest[0].Name;
            Assert.AreEqual("Self-Drilling", screwStyleName, "Screw Styles are not equal");
        }
    }
}
