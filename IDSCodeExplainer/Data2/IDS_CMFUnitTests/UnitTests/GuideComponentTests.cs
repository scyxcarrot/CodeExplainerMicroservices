using IDS.CMF;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.V2.CasePreferences;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Geometry;

namespace IDS.Testing.UnitTests
{
#if (Rhino7Installed)

    [TestClass]
    public class GuideComponentTests
    {
        [TestMethod]
        public void Delete_Guide_Will_Delete_All_Its_Components()
        {
            //Bug 1071844: C: Null Exception Error - Guide plastic entities are not invalidated if guides are deleted

            //arrange
            var director = ImplantDirectorHelper.CreateActualCMFImplantDirector(EScrewBrand.Synthes, ESurgeryType.Orthognathic);
            var objectManager = new CMFObjectManager(director);
            var guideCaseComponent = new GuideCaseComponent();

            var casePreferencesHelper = new CasePreferencesDataModelHelper(director);
            var guidePreferenceDataModel = casePreferencesHelper.AddNewGuideCase();

            var mesh = Mesh.CreateFromSphere(new Sphere(Point3d.Origin, 5.0), 10, 10);

            var guideComponents = guideCaseComponent.GetGuideComponents();

            foreach (var guideComponent in guideComponents)
            {
                var buildingBlock = guideCaseComponent.GetGuideBuildingBlock(guideComponent, guidePreferenceDataModel);
                objectManager.AddNewBuildingBlock(buildingBlock, mesh.DuplicateMesh());
            }

            guidePreferenceDataModel.Graph.InvalidateGraph();

            //act
            director.CasePrefManager.HandleDeleteGuidePreference(guidePreferenceDataModel);

            //assert
            foreach (var guideComponent in guideComponents)
            {
                var buildingBlock = guideCaseComponent.GetGuideBuildingBlock(guideComponent, guidePreferenceDataModel);
                var exist = objectManager.HasBuildingBlock(buildingBlock);
                Assert.IsFalse(exist, $"{guideComponent} still exist!");
            }
        }
    }

#endif
}