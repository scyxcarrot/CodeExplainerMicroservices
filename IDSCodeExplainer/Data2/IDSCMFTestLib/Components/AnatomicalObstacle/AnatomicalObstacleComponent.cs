using IDS.CMF.Utilities;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace IDS.CMF.TestLib.Components
{
    public class AnatomicalObstacleComponent
    {
        public string AnatomicalObstacleOriginalName { get; set; }

        public MeshComponent AnatomicalObstacleMesh { get; set; } = new MeshComponent();

        public void ParseToDirector(CMFObjectManager objectManager, string workDir)
        {
            AnatomicalObstacleMesh.ParseFromComponent(workDir, out var mesh);
            AnatomicalObstacleUtilities.AddAsAnatomicalObstacle(objectManager, mesh, AnatomicalObstacleOriginalName);
        }

        public void FillToComponent(RhinoObject anatomicalObstacle, string workDir)
        {
            AnatomicalObstacleOriginalName =  AnatomicalObstacleUtilities.GetAnatomicalObstacleOriginPartName(anatomicalObstacle);
            var mesh = (Mesh)anatomicalObstacle.Geometry;
            AnatomicalObstacleMesh.FillToComponent($"AnatomicalObstacle{anatomicalObstacle.Id}.stl", workDir, mesh);
        }
    }
}
