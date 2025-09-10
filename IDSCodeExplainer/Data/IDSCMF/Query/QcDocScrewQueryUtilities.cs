using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Quality;
using IDS.CMF.Utilities;
using IDS.CMF.V2.ScrewQc;
using IDS.Core.Utilities;
using IDS.RhinoInterfaces.Converter;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace IDS.CMF.Query
{
    public static class QcDocScrewQueryUtilities
    {
        public static QcDocScrewAndResultsInfoModel GetQcDocScrewAndResultsInfoModel(QcDocBaseScrewInfoData info, IEnumerable<IScrewQcResult> results)
        {
            return new QcDocScrewAndResultsInfoModel(info, results.Select(r => r.GetQcDocTableCellMessage()));
        }

        private static QcDocBaseScrewInfoData GetQcDocBaseScrewInfoData(CMFImplantDirector director, Screw screw, string screwType, ref Dictionary<string, long> timeRecorded)
        {
            var qcDocBaseScrewInfoData = new QcDocBaseScrewInfoData
            {
                Index = screw.Index,
                ScrewType = screwType,
                Length = (screw.HeadPoint - screw.TipPoint).Length
            };

            var timerComponent = new Stopwatch();
            timerComponent.Start();
            qcDocBaseScrewInfoData.Diameter = CasePreferencesHelper.LoadScrewLengthData().ScrewLengths
                .First(x => x.ScrewType == qcDocBaseScrewInfoData.ScrewType).ScrewDiameter;

            timerComponent.Stop();
            timeRecorded.Add($"Load Screw Length Data {screw.Index}", timerComponent.ElapsedMilliseconds);

            return qcDocBaseScrewInfoData;
        }

        public static QcDocBaseScrewInfoData GetQcDocGuideScrewInfoData(CMFImplantDirector director, Screw screw, out Dictionary<string, long> timeRecorded)
        {
            var objectManager = new CMFObjectManager(director);
            var guidePrefData = objectManager.GetGuidePreference(screw);
            var screwType = guidePrefData.GuidePrefData.GuideScrewTypeValue;

            timeRecorded = new Dictionary<string, long>();

            var qcDocBaseScrewInfoData = GetQcDocBaseScrewInfoData(director, screw, screwType, ref timeRecorded);

            var timerComponent = new Stopwatch();
            timerComponent.Start();

            var constraintMesh = (Mesh)objectManager.GetBuildingBlock(IBB.GuideSurfaceWrap).Geometry;
            var averageNormalAtCenterOfRotation = ScrewUtilities.GetNormalMeshAtScrewPoint(screw, constraintMesh,
                ScrewAngulationConstants.AverageNormalRadiusGuideFixationScrew);
            var referenceDirection = -(averageNormalAtCenterOfRotation);
            qcDocBaseScrewInfoData.Angle = MathUtilities.CalculateDegrees(screw.Direction, referenceDirection);

            timerComponent.Stop();
            timeRecorded.Add($"Calculate Screw Angle {screw.Index}", timerComponent.ElapsedMilliseconds);

            return qcDocBaseScrewInfoData;
        }

        public static QcDocBaseScrewInfoData GetQcDocImplantScrewInfoData(CMFImplantDirector director, Screw screw, out Dictionary<string, long> timeRecorded)
        {
            var objectManager = new CMFObjectManager(director);
            var casePrefData = objectManager.GetCasePreference(screw);
            var screwType = casePrefData.CasePrefData.ScrewTypeValue;

            timeRecorded = new Dictionary<string, long>();

            var qcDocBaseScrewInfoData = GetQcDocBaseScrewInfoData(director, screw, screwType, ref timeRecorded);

            var timerComponent = new Stopwatch();
            timerComponent.Start();

            var screwAnalysis = new CMFScrewAnalysis(director);

            var dotPastille = casePrefData.ImplantDataModel.DotList.FirstOrDefault(dot => (dot as DotPastille)?.Screw != null && screw.Id == (dot as DotPastille).Screw.Id);
            var referenceDirection = dotPastille.Direction;
            qcDocBaseScrewInfoData.Angle = screwAnalysis.CalculateScrewAngle(screw, -RhinoVector3dConverter.ToVector3d(referenceDirection));

            timerComponent.Stop();
            timeRecorded.Add($"Calculate Screw Angle {screw.Index}", timerComponent.ElapsedMilliseconds);

            return qcDocBaseScrewInfoData;
        }
    }
}
