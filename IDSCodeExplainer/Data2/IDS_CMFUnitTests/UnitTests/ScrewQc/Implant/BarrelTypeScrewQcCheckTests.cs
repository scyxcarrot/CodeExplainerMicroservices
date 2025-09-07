using IDS.CMF;
using IDS.CMF.Constants;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.ScrewQc;
using IDS.CMF.TestLib;
using IDS.CMF.Utilities;
using IDS.Core.V2.Geometries;
using IDS.RhinoInterfaces.Converter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Geometry;
using System.Linq;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class BarrelTypeScrewQcCheckTests
    {
        [TestMethod]
        public void BarrelTypeResult_QC_Result_Should_Return_Barrel_Full_Name_And_Green_Color_If_No_Barrel()
        {
            // Arrange
            const string barrelType = "Long";
            const string expectedBarrelTypeName = "Long";
            var director = GetSampleCase(barrelType, out var screw);

            // Act and Assert
            ActAndAssertQcResult(screw, expectedBarrelTypeName, "col_green");
        }

        [TestMethod]
        public void BarrelTypeResult_QC_Result_Should_Return_Barrel_Full_Name_And_Green_Color_If_No_User_Dictionary_Key()
        {
            // Arrange
            const string barrelType = "Long";
            const string expectedBarrelTypeName = "Long";
            var director = GetSampleCase(barrelType, out var screw);
            AddBarrel(director, screw);

            // Act and Assert
            ActAndAssertQcResult(screw, expectedBarrelTypeName, "col_green");
        }

        [TestMethod]
        public void BarrelTypeResult_QC_Result_Should_Return_Barrel_Full_Name_And_Green_Color_On_Success()
        {
            // Arrange
            const string barrelType = "Long";
            const string expectedBarrelTypeName = "Long";
            var director = GetSampleCase(barrelType, out var screw);
            AddBarrel(director, screw);

            var objectManager = new CMFObjectManager(director);
            var registeredBarrels = objectManager.GetAllBuildingBlocks(IBB.RegisteredBarrel);
            UserDictionaryUtilities.ModifyUserDictionary(registeredBarrels.First(), BarrelAttributeKeys.KeyIsGuideCreationError, false);

            // Act and Assert
            ActAndAssertQcResult(screw, expectedBarrelTypeName, "col_green");
        }

        [TestMethod]
        public void BarrelTypeResult_QC_Result_Should_Return_Barrel_Full_Name_And_Orange_Color_On_Error()
        {
            // Arrange
            const string barrelType = "Long";
            const string expectedBarrelTypeName = "Long";
            var director = GetSampleCase(barrelType, out var screw);
            AddBarrel(director, screw);

            var objectManager = new CMFObjectManager(director);
            var registeredBarrels = objectManager.GetAllBuildingBlocks(IBB.RegisteredBarrel);
            UserDictionaryUtilities.ModifyUserDictionary(registeredBarrels.First(), BarrelAttributeKeys.KeyIsGuideCreationError, true);

            // Act and Assert
            ActAndAssertQcResult(screw, expectedBarrelTypeName, "col_orange");
        }

        private CMFImplantDirector GetSampleCase(string barrelTypeToSet, out Screw screw)
        {
            //      Using Test Library to create case, to know the config of the case
            //      Can refer JSON at IDS_CMFUnitTests/Resources/JsonConfig/Screw/ImplantScrewSerializationTestData.json
            var resource = new TestResources();
            var director = CMFImplantDirectorConverter.ParseHeadlessFromFile(
                resource.ImplantScrewSerializationTestDataFilePath, string.Empty);
            var screwManager = new ScrewManager(director);
            var screws = screwManager.GetAllScrews(false);
            screw = screws[0];
            screw.BarrelType = barrelTypeToSet;

            return director;
        }

        private void AddBarrel(CMFImplantDirector director, Screw screw)
        {
            var objectManager = new CMFObjectManager(director);
            var staticIbb = BuildingBlocks.Blocks[IBB.RegisteredBarrel];
            var registeredBarrelIbb = staticIbb.Clone();
            var casePreference = director.CasePrefManager.CasePreferences[0];
            registeredBarrelIbb.Name = string.Format(registeredBarrelIbb.Layer,
                casePreference.CaseGuid);
            registeredBarrelIbb.Layer = string.Format(registeredBarrelIbb.Layer,
                casePreference.CaseName);
            var registeredBarrelEibb = new ExtendedImplantBuildingBlock
            {
                Block = registeredBarrelIbb,
                PartOf = IBB.RegisteredBarrel
            };

            var barrelBrep = new Sphere(RhinoPoint3dConverter.ToPoint3d(IDSPoint3D.Zero), 2).ToBrep();
            var barrelGuid = objectManager.AddNewBuildingBlock(registeredBarrelEibb, barrelBrep);

            screw.RegisteredBarrelId = barrelGuid;
        }

        private void ActAndAssertQcResult(Screw screw, string expectedBarrelTypeName, string expectedColumnColor)
        {
            // Act
            var checker = new BarrelTypeChecker();
            var result = checker.Check(screw);

            // Assert
            Assert.AreEqual($"<td class=\"{expectedColumnColor}\">{expectedBarrelTypeName}</td>", result.GetQcDocTableCellMessage(), "QC Doc result name is not match");
        }
    }
}
