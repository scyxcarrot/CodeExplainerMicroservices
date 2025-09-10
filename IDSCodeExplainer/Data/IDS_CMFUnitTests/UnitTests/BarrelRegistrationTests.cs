using IDS.CMF;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Utilities;
using IDS.CMF.V2.CasePreferences;
using IDS.Core.V2.Geometries;
using IDS.RhinoInterfaces.Converter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IDS.Testing.UnitTests
{
#if (Rhino7Installed)

    [TestClass]
    public class BarrelRegistrationTests
    {
        [TestMethod]
        public void RegisterAllGuideRegisteredBarrel_Registers_All_Barrels()
        {
            // Arrange
            var allScrews = CreateCaseWithImplantScrewsAndGuideSupport(
                out CMFImplantDirector director, out CMFObjectManager objectManager);

            // Act
            var guideSupport = (Mesh)objectManager.GetBuildingBlock(IBB.GuideSupport).Geometry;
            var barrelRegistrator = new CMFBarrelRegistrator(director);
            barrelRegistrator.RegisterAllGuideRegisteredBarrel(guideSupport, out bool areAllBarrelsMeetingSpecs);

            // Assert
            Assert.AreEqual(6, objectManager.GetAllBuildingBlocks(IBB.RegisteredBarrel).Count());
            Assert.IsTrue(areAllBarrelsMeetingSpecs);
        }

        [TestMethod]
        public void RegisterSingleScrewBarrel_Registers_One_Barrel()
        {
            // Arrange
            var allScrews = CreateCaseWithImplantScrewsAndGuideSupport(
                out CMFImplantDirector director, out CMFObjectManager objectManager);

            // Act
            var guideSupport = (Mesh)objectManager.GetBuildingBlock(IBB.GuideSupport).Geometry;
            var barrelRegistrator = new CMFBarrelRegistrator(director);
            barrelRegistrator.RegisterSingleScrewBarrel(allScrews[0], guideSupport, out bool isBarrelLevelingSkipped);

            // Assert
            Assert.AreEqual(1,objectManager.GetAllBuildingBlocks(IBB.RegisteredBarrel).Count());
        }

        [TestMethod]
        public void GetBarrelColor_Returns_Correctly()
        {
            // Arrange and Act
            var colorNotMeetingSpecs = CMFBarrelRegistrator.GetBarrelColor(EScrewBrand.Synthes, "Matrix Orthognathic Ø1.85",
                false, 0);

            var colorMeetingSpecs = CMFBarrelRegistrator.GetBarrelColor(EScrewBrand.Synthes, "Matrix Orthognathic Ø1.85",
                true, 0);

            // Assert
            Assert.AreEqual(Color.Red, colorNotMeetingSpecs);
            Assert.AreEqual(Color.FromArgb(128, 128, 128), colorMeetingSpecs);
        }

        private List<Screw> CreateCaseWithImplantScrewsAndGuideSupport(out CMFImplantDirector director, out CMFObjectManager objectManager)
        {
            var testPoints = new List<IDSPoint3D>
            {
                new IDSPoint3D(1, 1, 2),
                new IDSPoint3D(2, 1, 2),
                new IDSPoint3D(3, 1, 2),

            };
            var screws = ImplantScrewTestUtilities.CreateMultipleScrews(testPoints, Transform.Identity, true);
            
            director = screws[0].Director;
            objectManager = new CMFObjectManager(director);
            double guideSupportBoxLength = 20;
            var guideSupportMesh = BuildingBlockHelper.CreateRectangleMesh(RhinoPoint3dConverter.ToPoint3d(new IDSPoint3D(0, 0, 0)),
                RhinoPoint3dConverter.ToPoint3d(new IDSPoint3D(guideSupportBoxLength, guideSupportBoxLength, guideSupportBoxLength)),
                1);
            BuildingBlockHelper.AddNewBuildingBlock(BuildingBlocks.Blocks[IBB.GuideSupport],
                guideSupportMesh, objectManager);

            var casePreferenceData = director.CasePrefManager.CasePreferences[0];
            var implantComponent = new ImplantCaseComponent();
            var screwBuildingBlock = implantComponent.GetImplantBuildingBlock(IBB.Screw, casePreferenceData);
            var allScrews = objectManager.GetAllBuildingBlocks(screwBuildingBlock)
                .Select(screw => (Screw)screw).ToList();
            allScrews.ForEach(x => x.BarrelType = "Standard");

            return allScrews;
        }
    }

#endif
}