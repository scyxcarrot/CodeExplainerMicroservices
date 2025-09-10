using IDS.CMF.CasePreferences;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Query;
using IDS.CMF.Visualization;
using IDS.Common.Enumerators;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace IDS.CMF.Quality
{
    public class QCImplantGuideSection
    {
        private CMFImplantDirector director;
        private CMFObjectManager objectManager;

        public QCImplantGuideSection(CMFImplantDirector director)
        {
            this.director = director;
            objectManager = new CMFObjectManager(director);
        }

        public void ImplantGuideInfo(ref Dictionary<string, string> valueDictionary, CasePreferenceDataModel casePrefData, DocumentType docType, Mesh actualGuide)
        {
            if (docType == DocumentType.PlanningQC)
            {
                // Visibility Settings
                ////////////////
                valueDictionary.Add("GUIDE_DISPLAY", "none");
            }
            else
            {
                var bonesQuery = new QCDocumentBonesQuery(director);
                var allBones = bonesQuery.GetGuideBones();

                if (docType == DocumentType.MetalQC)
                {
                    var guideComponent = new GuideCaseComponent();
                    var extendedBuildingBlock = guideComponent.GetGuideBuildingBlock(IBB.GuidePreview, casePrefData);
                    var hasGuidePreview = objectManager.HasBuildingBlock(extendedBuildingBlock);
                    if (hasGuidePreview)
                    {
                        valueDictionary.Add("IMG_GUIDE_ONBONE", ScreenshotsGuide.GenerateGuidePreviewOnBoneImageString(director, casePrefData));

                        var guidePreview = (Mesh)objectManager.GetBuildingBlock(extendedBuildingBlock).Geometry;
                        FillDictionaryForGuideClearance(ref valueDictionary, casePrefData, allBones, guidePreview);
                    }

                    // Visibility Settings
                    ////////////////
                    valueDictionary.Add("GUIDE_DISPLAY", hasGuidePreview ? "block" : "none");
                }
                else if (docType == DocumentType.ApprovedQC)
                {
                    var hasActualGuide = actualGuide != null;
                    if (hasActualGuide)
                    {
                        valueDictionary.Add("IMG_GUIDE_ONBONE", ScreenshotsGuide.GenerateMeshOnBoneImageString(director, casePrefData, actualGuide));

                        FillDictionaryForGuideClearance(ref valueDictionary, casePrefData, allBones, actualGuide);
                    }

                    // Visibility Settings
                    ////////////////
                    valueDictionary.Add("GUIDE_DISPLAY", hasActualGuide ? "block" : "none");
                }
            }
        }

        private void FillDictionaryForGuideClearance(ref Dictionary<string, string> valueDictionary, CasePreferenceDataModel casePrefData, Mesh bone, Mesh guide)
        {
            var guideForClearance = guide.DuplicateMesh();
            guideForClearance.Compact(); //remove free points

            double[] vertexDistances;
            double[] triangleCenterDistances;
            TriangleSurfaceDistance.DistanceBetween(guideForClearance, bone, out vertexDistances, out triangleCenterDistances);

            valueDictionary.Add("GUIDE_MIN_CLEARANCE", vertexDistances.Min().ToString("F4", CultureInfo.InvariantCulture));

            var guideClearance = ScreenshotsUtilities.GenerateClearanceMeshForScreenshot(guideForClearance, vertexDistances.ToList());
            valueDictionary.Add("IMG_GUIDE_CLEARANCE", ScreenshotsGuide.GenerateGuideClearanceImageString(director, casePrefData, guideClearance));
        }
    }
}
