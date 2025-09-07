using IDS.Core.Utilities;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IDS.Glenius.Visualization
{
    public class CylindricalOffsetVisualizer
    {
        private static CylindricalOffsetVisualizer _instance;

        private readonly Dictionary<double, CylinderListConduit> cylinderOffsetsDictionary;

        private CylindricalOffsetVisualizer()
        {
            cylinderOffsetsDictionary = new Dictionary<double, CylinderListConduit>();
        }

        public static CylindricalOffsetVisualizer Get()
        {
            return _instance ?? (_instance = new CylindricalOffsetVisualizer());
        }

        public void ToggleVisualization(GleniusImplantDirector director, double offset, double transparency, Color color)
        {
            CreateCylinderConduitsIfNotCreated(director, offset);

            var cylinderListConduit = cylinderOffsetsDictionary[offset];
            cylinderListConduit.UpdateMaterial(transparency, color);
            cylinderListConduit.Enabled = !cylinderListConduit.Enabled;

            if (cylinderListConduit.Enabled)
            {
                //this is required because if a smaller offset cylinder conduit is enabled on top of an already enabled larger offset cylinder conduit, the smaller offset cylinder conduit will be clipped out
                ReorderDrawing();
            }

            director.Document.Views.Redraw();
        }

        public void Reset()
        {
            foreach (var cylinderConduit in cylinderOffsetsDictionary.Values)
            {
                cylinderConduit.Enabled = false;
            }
            cylinderOffsetsDictionary.Clear();
        }

        private void CreateCylinderConduitsIfNotCreated(GleniusImplantDirector director, double offset)
        {
            if (cylinderOffsetsDictionary.ContainsKey(offset))
            {
                return;
            }

            var cylinderList = new List<Cylinder>();
            var implantDerivedEntities = new ImplantDerivedEntities(director);
            var objectManager = new GleniusObjectManager(director);
            var screws = objectManager.GetAllBuildingBlocks(IBB.Screw).Select(rhinoObj => rhinoObj as Screw);
            foreach (var screw in screws)
            {
                var screwHoleScaffold = implantDerivedEntities.GetScrewHoleScaffold(screw);
                var plane = new Plane(screw.HeadPoint, screw.Direction);
                var cylinder = CylinderUtilities.CreateCylinderFromBoundingBox(screwHoleScaffold.GetBoundingBox(true), plane, screw.HeadPoint, screw.TotalLength, offset);
                cylinderList.Add(cylinder);
            }

            var cylinderConduits = new CylinderListConduit(cylinderList)
            {
                Enabled = false
            };
            cylinderOffsetsDictionary.Add(offset, cylinderConduits);
        }

        private void ReorderDrawing()
        {
            var orderedConduits = cylinderOffsetsDictionary.Where(cylinder => cylinder.Value.Enabled).OrderBy(cylinder => cylinder.Key).Select(cylinder => cylinder.Value).ToList();
            if (orderedConduits.Count <= 1)
            {
                return;
            }

            foreach (var conduit in orderedConduits)
            {
                conduit.Enabled = false;
                conduit.Enabled = true;
            }
        }
    }
}
