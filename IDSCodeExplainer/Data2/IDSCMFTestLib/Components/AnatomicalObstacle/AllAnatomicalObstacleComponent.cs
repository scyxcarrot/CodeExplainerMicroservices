using IDS.CMF.ImplantBuildingBlocks;
using System.Collections.Generic;

namespace IDS.CMF.TestLib.Components
{
    public class AllAnatomicalObstacleComponent
    {
        public List<AnatomicalObstacleComponent> AnatomicalObstacles { get; set; } =
            new List<AnatomicalObstacleComponent>();

        public void ParseToDirector(CMFImplantDirector director, string workDir)
        {
            var objectManager = new CMFObjectManager(director);
            foreach (var anatomicalObstacle in AnatomicalObstacles)
            {
                anatomicalObstacle.ParseToDirector(objectManager, workDir);
            }
        }

        public void FillToComponent(CMFImplantDirector director, string workDir)
        {
            var objectManager = new CMFObjectManager(director);
            var anatomicalObstacles = objectManager.GetAllBuildingBlocks(IBB.AnatomicalObstacles);
            foreach (var anatomicalObstacleRhinoObject in anatomicalObstacles)
            {
                var anatomicalObstacle = new AnatomicalObstacleComponent();
                anatomicalObstacle.FillToComponent(anatomicalObstacleRhinoObject, workDir);
                AnatomicalObstacles.Add(anatomicalObstacle);
            }
        }
    }
}
