using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.V2.ScrewQc;
using IDS.RhinoInterface.Converter;
using Rhino.Geometry;
using System.Linq;

namespace IDS.CMF.ScrewQc
{
    public class ImplantScrewAnatomicalObstacleProxyChecker : ImplantScrewQcProxyChecker
    {
        public override string ScrewQcCheckTrackerName { get; }

        public ImplantScrewAnatomicalObstacleProxyChecker(CMFImplantDirector director) :
            base(ImplantScrewQcCheck.ImplantScrewAnatomicalObstacle)
        {
            var objectManager = new CMFObjectManager(director);
            var anatomicalObstacles = 
                objectManager.GetAllBuildingBlocks(IBB.AnatomicalObstacles)
                    .Select(x => (Mesh)x.Geometry)
                    .Select(RhinoMeshConverter.ToIDSMesh)
                    .ToList();
            Checker = new ImplantScrewAnatomicalObstacleChecker(
                Console, anatomicalObstacles);
            ScrewQcCheckTrackerName = Checker.ScrewQcCheckTrackerName;
        }
    }
}
