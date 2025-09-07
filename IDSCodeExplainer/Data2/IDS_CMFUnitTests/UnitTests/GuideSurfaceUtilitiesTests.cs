using IDS.CMF.CasePreferences;
using IDS.CMF.Preferences;
using IDS.CMF.V2.CasePreferences;
using IDS.Core.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Testing.UnitTests
{
#if (Rhino7Installed)

    [TestClass]
    public class GuideSurfaceUtilitiesTests
    {
        private GuidePreferenceModel CreateGuidePreferenceModel()
        {
            const string guideType = "Lefort";
            const string screwType = "Matrix Orthognathic Ø1.85";
            const int caseName = 1;

            var director = ImplantDirectorHelper.CreateActualCMFImplantDirector(EScrewBrand.Synthes,
                ESurgeryType.Orthognathic);
            var casePreferencesHelper = new CasePreferencesDataModelHelper(director);
            var guidePreferenceDataModel = casePreferencesHelper.AddNewGuideCase();
            CasePreferencesDataModelHelper.ConfigureGuideCase(guidePreferenceDataModel, guideType, screwType, caseName);

            return guidePreferenceDataModel;
        }

        [TestMethod]
        public void GuideSolid_Check_Thickness_Test()
        {
            //arrange 
            var testResources = new TestResources();
            var guideParams = CMFPreferences.GetActualGuideParameters();
            var guidePreferenceModel = CreateGuidePreferenceModel();

            StlUtilities.StlBinary2RhinoMesh(testResources.GuideSolidPatchStlFilePath, out var solidPatch);
            StlUtilities.StlBinary2RhinoMesh(testResources.GuideBaseStlFilePath, out var guideBase);
            StlUtilities.StlBinary2RhinoMesh(testResources.GuideSurfaceWrapStlFilePath, out var guideSurfaceWrap);

            //act
            var solidMesh = CMF.Operations.GuideCreatorV2.CreateGuideSolidSurface(new List<Mesh> { solidPatch }, guideBase, guideSurfaceWrap, guideParams, guidePreferenceModel);
            // Make sure the maximumThickness parameter is larger than the maxThickness check below, at all times
            var thicknessValues = Mesh.ComputeThickness(new List<Mesh> { solidMesh }, guideParams.NonMeshParams.NonMeshHeight * 3 + 0.2);

            //assert
            // 0.2 is the delta we allow for the height of solid surface
            var maxThickness = guideParams.NonMeshParams.NonMeshHeight * 2 + 0.2;
            Assert.IsFalse(thicknessValues.Any(value => value.Thickness > maxThickness));
        }
    }

#endif
}