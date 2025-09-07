using System;
using IDS.CMF.V2.MTLS.Operation;
using IDS.CMF.V2.ScrewQc;
using IDS.Core.V2.Utilities;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using System.Collections.Generic;

namespace IDS.CMF.ScrewQc
{
    public class ImplantScrewAnatomicalObstacleChecker : ImplantScrewQcChecker
    {
        private readonly IMesh _anatomicalObstacleAppended;
        public override string ScrewQcCheckTrackerName => "Distance to Anatomical Obstacle";

        public ImplantScrewAnatomicalObstacleChecker(
            IConsole console,
            List<IMesh> anatomicalObstacles) :
            base(console, ImplantScrewQcCheck.ImplantScrewAnatomicalObstacle)
        {
            _anatomicalObstacleAppended = MeshUtilitiesV2.AppendMeshes(anatomicalObstacles);
        }

        public override IScrewQcResult Check(IScrewQcData screwQcData)
        {
            return new ImplantScrewAnatomicalObstacleResult(ScrewQcCheckName,
                new ImplantScrewAnatomicalObstacleContent()
                {
                    DistanceToAnatomicalObstacles = GetScrewToAnatomicalObstacles(screwQcData),
                });
        }

        public double GetScrewToAnatomicalObstacles(IScrewQcData screwQcData)
        {
            var distance = double.NaN;
            if (_anatomicalObstacleAppended != null)
            {
                distance = ScrewQcOperations.PerformQcScrewAnatomyDistance(
                    Console, screwQcData, _anatomicalObstacleAppended);
                distance = Math.Round(distance, 2);
            }

            return distance;
        }
    }
}
