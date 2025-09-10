using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.CMF.V2.ScrewQc;
using IDS.Core.DataTypes;
using Rhino.Geometry.Intersect;
using System.Collections.Immutable;
using System.Linq;

namespace IDS.CMF.ScrewQc
{
    public class ImplantScrewGaugeIntersectChecker: GuideScrewQcChecker<ImplantScrewGaugeIntersectResult>
    {
        private readonly ImmutableDictionary<Screw, ScrewInfoRecord> _implantScrewsAtOriginalPos;

        public override string ScrewQcCheckTrackerName => "Intersection Of Gauges";

        public ImplantScrewGaugeIntersectChecker(CMFImplantDirector director, ImmutableDictionary<Screw, ScrewInfoRecord> implantScrewsAtOriginalPos) : 
            base(director, GuideScrewQcCheck.ImplantScrewGaugeIntersection)
        {
            _implantScrewsAtOriginalPos = implantScrewsAtOriginalPos;
        }

        protected override ImplantScrewGaugeIntersectResult CheckForSharedScrew(Screw screw)
        {
            var guideScrewGauge = ScrewGaugeUtilities.MergeAllLengthScrewGaugeMeshes(screw);
            var content = new ImplantScrewGaugeIntersectContent();

            foreach (var implantScrewAtOriginalPos in _implantScrewsAtOriginalPos)
            {
                var implantScrewGauge = ScrewGaugeUtilities.MergeAllLengthScrewGaugeMeshes(implantScrewAtOriginalPos.Key);
                var implantScrewInfoRecord = implantScrewAtOriginalPos.Value;
                var intersection = Intersection.MeshMeshAccurate(guideScrewGauge, implantScrewGauge, IntersectionParameters.Tolerance);
                if (intersection != null && intersection.Any())
                {
                    content.IntersectedImplantScrewGauges.Add(implantScrewInfoRecord);
                }
            }

            return new ImplantScrewGaugeIntersectResult(ScrewQcCheckName, content);
        }
    }
}
