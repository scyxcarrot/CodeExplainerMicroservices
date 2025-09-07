using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.V2.CasePreferences;
using Rhino.Geometry;
using System.Collections.Generic;

namespace IDS.Testing.UnitTests
{
#if (Rhino7Installed)
    public static class GuideScrewTestUtilities
    {
        const string GuideType = "Lefort";
        const string ScrewType = "Matrix Orthognathic Ø1.85";
        const int CaseNum = 1;

        public static List<Screw> CreateGuideScrew(out CMFImplantDirector director,
            out GuidePreferenceModel guidePreferenceDataModel, uint numScrews = 1)
        {
            InstantiateClasses(out director, out guidePreferenceDataModel, out var objectManager, out var guideScrewBb, out var screwAideDict);

            var screws = new List<Screw>();

            for (var i = 0; i < numScrews; i++)
            {
                PositionUtilities.RandomXY(out var x, out var y, 10, -10);
                var screw = new Screw(director, new Point3d(x, y, 1), new Point3d(x, y, -10), screwAideDict, 1,
                    ScrewType);
                objectManager.AddNewBuildingBlock(guideScrewBb, screw);
                screws.Add(screw);
            }

            return screws;
        }

        public static List<Screw> CreateGuideScrewWithPoints(out CMFImplantDirector director, IEnumerable<(Point3d, Point3d)> screwPairs, 
            out GuidePreferenceModel guidePreferenceDataModel)
        {
            InstantiateClasses(out director, out guidePreferenceDataModel, out var objectManager, out var guideScrewBb, out var screwAideDict);

            var screws = new List<Screw>();

            var index = 1;
            foreach (var screwPair in screwPairs)
            {
                var screwHeadPt = screwPair.Item1;
                var screwTipPt = screwPair.Item2;

                var screw = new Screw(director, screwHeadPt, screwTipPt, screwAideDict, index, ScrewType);
                objectManager.AddNewBuildingBlock(guideScrewBb, screw);
                screws.Add(screw);

                index += 1;
            }

            return screws;
        }

        private static void InstantiateClasses(out CMFImplantDirector director, out GuidePreferenceModel guidePreferenceDataModel,
            out CMFObjectManager objectManager, out ExtendedImplantBuildingBlock guideScrewBb, out Dictionary<string, GeometryBase> screwAideDict)
        {
            director = ImplantDirectorHelper.CreateActualCMFImplantDirector(EScrewBrand.Synthes,
                ESurgeryType.Orthognathic);
            var casePreferencesHelper = new CasePreferencesDataModelHelper(director);
            guidePreferenceDataModel = casePreferencesHelper.AddNewGuideCase();
            CasePreferencesDataModelHelper.ConfigureGuideCase(guidePreferenceDataModel, GuideType, ScrewType, CaseNum);

            objectManager = new CMFObjectManager(director);
            var guideCaseComponent = new GuideCaseComponent();
            guideScrewBb =
                guideCaseComponent.GetGuideBuildingBlock(IBB.GuideFixationScrew, guidePreferenceDataModel);
            screwAideDict = guidePreferenceDataModel.GuideScrewAideData.GenerateScrewAideDictionary();
        }
    }
#endif
}
