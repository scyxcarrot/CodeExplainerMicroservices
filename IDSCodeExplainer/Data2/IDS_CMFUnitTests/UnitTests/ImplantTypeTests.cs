using IDS.CMF.CasePreferences;
using IDS.CMF.Constants;
using IDS.CMF.Utilities;
using IDS.CMF.V2.CasePreferences;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class ImplantTypeTests
    {
        #region ScrewTypeConstants
        private const string OrthognathicScrew = "Matrix Orthognathic Ø1.85";
        private const string MidfaceScrew = "Matrix Midface Ø1.55";
        private const string MandibleScrew2_0 = "Matrix Mandible Ø2.0";
        private const string MandibleScrew2_4 = "Matrix Mandible Ø2.4";
        private const string MiniSlottedSelfTapping = "Mini Slotted Self-Tapping";
        private const string MiniSlottedSelfDrilling = "Mini Slotted Self-Drilling";
        private const string MiniCrossedSelfTapping = "Mini Crossed Self-Tapping";
        private const string MiniCrossedSelfDrilling = "Mini Crossed Self-Drilling";
        private const string MicroSlotted = "Micro Slotted";
        private const string MicroCrossed = "Micro Crossed";
        #endregion ScrewTypeConstants

        #region Synthes
        [TestMethod]
        public void Check_Synthes_Brand_Has_Correct_Screw_Types()
        {
            var allScrewTypes = new List<string>
            {
                OrthognathicScrew,
                MidfaceScrew,
                MandibleScrew2_0,
                MandibleScrew2_4
            };

            var screwTypesExceptMidface = new List<string>
            {
                OrthognathicScrew,
                MandibleScrew2_0,
                MandibleScrew2_4
            };

            var implantsWithoutMidfaceScrew = new List<string>
            {
                "BSSOSingle",
                "BSSODouble",
                "MandibleSmall",
                "MandibleLarge"
            };

            Check_General_Brand_Has_Correct_Screw_Types(EScrewBrand.Synthes, allScrewTypes, screwTypesExceptMidface,
                implantsWithoutMidfaceScrew);
        }
        
        [TestMethod]
        public void Matrix_Orthognathic_1_85_Should_Have_Standard_Short_Midface_Barrel()
        {
            // arrange
            var implantScrewType = OrthognathicScrew;
            var availableBarrelTypes = new Dictionary<string, string>
            {
                {"Standard", "Matrix_Orthognathic_Ø1.85_Standard" },
                {"Short", "Matrix_Orthognathic_Ø1.85_Short"},
                {"Midface Standard", "Matrix_Midface_Ø1.55_Standard"},
                {"Midface Short", "Matrix_Midface_Ø1.55_Short"},
                {"Marking", "Matrix_Orthognathic_Ø1.85_Marking"},
                {"Midface Marking", "Matrix_Midface_Ø1.55_Marking"}
            };

            // act and assert
            AssertImplantScrewTypeHasBarrelTypes(implantScrewType, availableBarrelTypes);
        }

        [TestMethod]
        public void Matrix_Midface_1_55_Should_Have_Standard_Short_Barrel()
        {
            //arrange
            var implantScrewType = MidfaceScrew;
            var availableBarrelTypes = new Dictionary<string, string>
            {
                {"Standard", "Matrix_Midface_Ø1.55_Standard"}, 
                {"Short", "Matrix_Midface_Ø1.55_Short"},
                {"Marking", "Matrix_Midface_Ø1.55_Marking"}
            };
            //act and assert
            AssertImplantScrewTypeHasBarrelTypes(implantScrewType, availableBarrelTypes);
        }

        [TestMethod]
        public void Matrix_Mandible_2_0_Should_Have_Standard_Short_Barrel()
        {
            //arrange
            var implantScrewType = MandibleScrew2_0;
            var availableBarrelTypes = new Dictionary<string, string>
            {
                {"Standard", "Matrix_Mandible_Ø2.0_Standard"},
                {"Short", "Matrix_Mandible_Ø2.0_Short"},
                {"Marking", "Matrix_Mandible_Ø2.0_Marking"}
            };
            //act and assert
            AssertImplantScrewTypeHasBarrelTypes(implantScrewType, availableBarrelTypes);
        }

        [TestMethod]
        public void Matrix_Mandible_2_4_Should_Have_Long_Hexagonal_Barrel()
        {
            //arrange
            var implantScrewType = MandibleScrew2_4;
            var availableBarrelTypes = new Dictionary<string, string>
            {
                {"Standard", "Matrix_Mandible_Ø2.4_Standard"},
                {"Short", "Matrix_Mandible_Ø2.4_Short"},
                {"Marking", "Matrix_Mandible_Ø2.4_Marking"}
            };
            //act and assert
            AssertImplantScrewTypeHasBarrelTypes(implantScrewType, availableBarrelTypes);
        }

        #endregion Synthes

        #region SynthesUsCanada
        [TestMethod]
        public void Check_Synthes_US_Canada_Brand_Has_Correct_Screw_Types()
        {
            var allScrewTypes = new List<string>
            {
                OrthognathicScrew,
                MidfaceScrew,
                MandibleScrew2_0,
                MandibleScrew2_4
            };

            var screwTypesExceptMidface = new List<string>
            {
                OrthognathicScrew,
                MandibleScrew2_0,
                MandibleScrew2_4
            };

            var implantsWithoutMidfaceScrew = new List<string>
            {
                "BSSOSingle",
                "BSSODouble",
                "MandibleSmall",
                "MandibleLarge"
            };
            Check_General_Brand_Has_Correct_Screw_Types(EScrewBrand.SynthesUsCanada, 
                allScrewTypes,
                screwTypesExceptMidface,
                implantsWithoutMidfaceScrew);
        }

        #endregion SynthesUsCanada

        #region MtlsStandardPlus
        [TestMethod]
        public void Mini_Slotted_Self_Tapping_Should_Have_Standard_Short_Barrel()
        {
            //arrange
            var implantScrewType = MiniSlottedSelfTapping;
            var availableBarrelTypes = new Dictionary<string, string>
            {
                {"Standard", "Mini_Self_Tapping_Standard"},
                {"Short", "Mini_Self_Tapping_Short"},
                {"Marking", "Mini_Self_Tapping_Marking"}
            };
            //act and assert
            AssertImplantScrewTypeHasBarrelTypes(implantScrewType, availableBarrelTypes);
        }

        [TestMethod]
        public void Mini_Slotted_Self_Drilling_Should_Have_Standard_Short_Barrel()
        {
            //arrange
            var implantScrewType = MiniSlottedSelfDrilling;
            var availableBarrelTypes = new Dictionary<string, string>
            {
                {"Standard", "Mini_Self_Drilling_Standard"},
                {"Short", "Mini_Self_Drilling_Short"},
                {"Marking", "Mini_Self_Drilling_Marking"}
            };
            //act and assert
            AssertImplantScrewTypeHasBarrelTypes(implantScrewType, availableBarrelTypes);
        }

        [TestMethod]
        public void Mini_Crossed_Self_Tapping_Should_Have_Standard_Short_Barrel()
        {
            //arrange
            var implantScrewType = MiniCrossedSelfTapping;
            var availableBarrelTypes = new Dictionary<string, string>
            {
                {"Standard", "Mini_Self_Tapping_Standard"},
                {"Short", "Mini_Self_Tapping_Short"},
                {"Marking", "Mini_Self_Tapping_Marking"}
            };
            //act and assert
            AssertImplantScrewTypeHasBarrelTypes(implantScrewType, availableBarrelTypes);
        }

        [TestMethod]
        public void Mini_Crossed_Self_Drilling_Should_Have_Standard_Short_Barrel()
        {
            //arrange
            var implantScrewType = MiniCrossedSelfDrilling;
            var availableBarrelTypes = new Dictionary<string, string>
            {
                {"Standard", "Mini_Self_Drilling_Standard"},
                {"Short", "Mini_Self_Drilling_Short"},
                {"Marking", "Mini_Self_Drilling_Marking"}
            };
            //act and assert
            AssertImplantScrewTypeHasBarrelTypes(implantScrewType, availableBarrelTypes);
        }

        [TestMethod]
        public void Micro_Slotted_Should_Have_Standard_Barrel()
        {
            //arrange
            var implantScrewType = MicroSlotted;
            var availableBarrelTypes = new Dictionary<string, string>
            {
                {"Standard", "Micro_Standard"},
                {"Marking", "Micro_Marking"}
            };
            //act and assert
            AssertImplantScrewTypeHasBarrelTypes(implantScrewType, availableBarrelTypes);
        }

        [TestMethod]
        public void Micro_Crossed_Should_Have_Standard_Barrel()
        {
            //arrange
            var implantScrewType = MicroCrossed;
            var availableBarrelTypes = new Dictionary<string, string>
            {
                {"Standard", "Micro_Standard"},
                {"Marking", "Micro_Marking"}
            };
            //act and assert
            AssertImplantScrewTypeHasBarrelTypes(implantScrewType, availableBarrelTypes);
        }

        [TestMethod]
        public void Check_MTLSStandardPlus_Brand_Has_Correct_Screw_Types()
        {
            var allScrewTypes = new List<string>
            {
                MiniCrossedSelfTapping,
                MiniCrossedSelfDrilling,
                MicroCrossed
            };
            
            var screwTypesExceptMicro = new List<string>
            {
                MiniCrossedSelfTapping,
                MiniCrossedSelfDrilling,
            };

            var implantsWithoutMicroScrew = new List<string>
            {
                "Maxilla"
            };

            Check_General_Brand_Has_Correct_Screw_Types(EScrewBrand.MtlsStandardPlus, allScrewTypes, screwTypesExceptMicro,
                implantsWithoutMicroScrew);
        }

        [TestMethod]
        public void MtlsStandardPlus_Implant_Type_Should_Not_Have_Mini_Crossed_Screw_Type()
        {
            //arrange
            var screwTypeToTest = ObsoletedScrewStyle.MiniCrossed;

            Implant_Type_Should_Not_Have_Screw_Type_Without_Screw_Style_Specified(EScrewBrand.MtlsStandardPlus, screwTypeToTest);
        }

        [TestMethod]
        public void MtlsStandardPlus_Implant_Type_Should_Not_Have_Mini_Slotted_Screw_Type()
        {
            //arrange
            var screwTypeToTest = ObsoletedScrewStyle.MiniSlotted;

            Implant_Type_Should_Not_Have_Screw_Type_Without_Screw_Style_Specified(EScrewBrand.MtlsStandardPlus, screwTypeToTest);
        }

        [TestMethod]
        public void MtlsStandardPlus_Implant_Type_Should_Not_Have_Mini_Crossed_Hex_Barrel_Screw_Type()
        {
            //arrange
            var screwTypeToTest = ObsoletedScrewStyle.MiniCrossedHexBarrel;

            Implant_Type_Should_Not_Have_Screw_Type_Without_Screw_Style_Specified(EScrewBrand.MtlsStandardPlus, screwTypeToTest);
        }

        [TestMethod]
        public void MtlsStandardPlus_Implant_Type_Should_Not_Have_Mini_Slotted_Hex_Barrel_Screw_Type()
        {
            //arrange
            var screwTypeToTest = ObsoletedScrewStyle.MiniSlottedHexBarrel; 

            Implant_Type_Should_Not_Have_Screw_Type_Without_Screw_Style_Specified(EScrewBrand.MtlsStandardPlus, screwTypeToTest);
        }

        #endregion

        #region Helper methods
        private void Implant_Type_Should_Not_Have_Screw_Type_Without_Screw_Style_Specified(EScrewBrand brand, string screwTypeToTest)
        {
            Check_Implant_Screw_Type_Has_Been_Removed(brand, screwTypeToTest);
        }

        private void Check_Implant_Screw_Type_Has_Been_Removed(EScrewBrand brand, string screwTypeToTest)
        {
            var casePreferencesInfo = CasePreferencesHelper.LoadScrewBrandCasePreferencesInfo(brand);
            foreach (var implant in casePreferencesInfo.Implants)
            {
                AssertScrewTypeDoesNotExist(implant.Screw, screwTypeToTest);
            }
        }

        private void AssertScrewTypeDoesNotExist(List<ScrewPreferences> screwPreferences, string screwTypeToTest)
        {
            Assert.IsTrue(screwPreferences.All(s => s.ScrewType != screwTypeToTest));
        }

        private void AssertImplantScrewTypeHasBarrelTypes(string implantScrewType, Dictionary<string, string> availableBarrelTypes)
        {
            var screwLengths = CasePreferencesHelper.LoadScrewLengthData().ScrewLengths;
            var screwLength = screwLengths.Find(x => x.ScrewType == implantScrewType);
            Assert.IsTrue(availableBarrelTypes.Count == screwLength.BarrelTypesAndBarrelNames.Count);
            Assert.IsTrue(screwLength.BarrelTypesAndBarrelNames.All(x =>
                availableBarrelTypes.TryGetValue(x.Key, out var value)
                && x.Value == value));
        }

        private void Check_General_Brand_Has_Correct_Screw_Types(EScrewBrand screwBrand, List<string> allScrewTypes, 
            List<string> screwTypesExceptSome, List<string> implantsToUseScrewTypesExceptSome)
        {
            var casePreferencesInfo = CasePreferencesHelper.LoadScrewBrandCasePreferencesInfo(screwBrand);
            allScrewTypes.Sort();
            screwTypesExceptSome.Sort();

            foreach (var implant in casePreferencesInfo.Implants)
            {
                var screwTypesAvailable = implant.Screw.Select(x => x.ScrewType).ToList();
                screwTypesAvailable.Sort();

                if (implantsToUseScrewTypesExceptSome.Contains(implant.ImplantType))
                {
                    CollectionAssert.AreEqual(screwTypesExceptSome, screwTypesAvailable);
                }
                else
                {
                    CollectionAssert.AreEqual(allScrewTypes, screwTypesAvailable);
                }
            }
        }
        #endregion
    }
}
