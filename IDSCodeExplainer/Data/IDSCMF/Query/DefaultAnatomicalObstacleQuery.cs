using IDS.CMF.ImplantBuildingBlocks;
using Rhino.DocObjects;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Query
{
    public class DefaultAnatomicalObstacleQuery
    {
        private readonly CMFObjectManager _objectManager;

        public DefaultAnatomicalObstacleQuery(CMFObjectManager objectManager)
        {
            this._objectManager = objectManager;
        }

        public IEnumerable<Mesh> GetDefaultAnatomicalObstacles()
        {
            var rhinoObjects = GetDefaultAnatomicalObstacleRhinoObjects();
            var anatomicalObstacle = rhinoObjects.Select(r => (Mesh)r.Geometry);
            return anatomicalObstacle;
        }

        public bool IsDefaultAnatomicalObstacle(RhinoObject rhinoObject)
        {
            var rhinoObjects = GetDefaultAnatomicalObstacleRhinoObjects();
            return rhinoObjects.Select(r => r.Name).Contains(rhinoObject.Name);
        }

        private List<RhinoObject> GetDefaultAnatomicalObstacleRhinoObjects()
        {
            var proPlanImportComponent = new ProPlanImportComponent();
            var partNamePatterns = proPlanImportComponent.Blocks.Where(b => b.IsDefaultAnatomicalObstacle).Select(b => $"{b.PartNamePattern}$");
            return _objectManager.GetAllBuildingBlockRhinoObjectByMatchingNames(ProPlanImportComponent.StaticIBB, partNamePatterns);
        }
    }
}
