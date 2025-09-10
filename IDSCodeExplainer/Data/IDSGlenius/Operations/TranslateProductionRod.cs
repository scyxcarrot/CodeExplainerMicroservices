using IDS.Core.ImplantBuildingBlocks;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace IDS.Glenius.Operations
{
    public class TranslateProductionRod : TranslateObjectAlongHeadPlaneXY
    {
        private readonly double maxRangeOfMovement;

        public TranslateProductionRod(GleniusImplantDirector director) : base(director, BuildingBlocks.Blocks[IBB.ProductionRod].Name)
        {
            maxRangeOfMovement = 5.0;
            material = new DisplayMaterial(Colors.Metal, 0.0);

            objectPreview = objectManager.GetBuildingBlock(IBB.ProductionRod).Geometry;

            Plane coordinateSystem;
            if (objectManager.GetBuildingBlockCoordinateSystem(IBB.ProductionRod, out coordinateSystem))
            {
                objectCoordinateSystem = coordinateSystem;
            }
            else
            {
                objectManager.SetBuildingBlockCoordinateSystem(IBB.ProductionRod, objectCoordinateSystem);
            }
            objectPreviewOrigin = objectCoordinateSystem.Origin;
        }

        protected override void TransformBuildingBlocks(Transform transform)
        {
            RhinoDoc.AddRhinoObject += RhinoDocAddRhinoObject;
            objectManager.TransformBuildingBlock(IBB.ProductionRod, transform);
            RhinoDoc.AddRhinoObject -= RhinoDocAddRhinoObject;
            objectCoordinateSystem.Transform(transform);
            objectManager.SetBuildingBlockCoordinateSystem(IBB.ProductionRod, objectCoordinateSystem);
        }

        protected override bool IsWithinRangeOfMovement(double distanceFromCenter)
        {
            if (distanceFromCenter < maxRangeOfMovement)
            {
                return true;
            }
            return false;
        }

        private void RhinoDocAddRhinoObject(object sender, RhinoObjectEventArgs e)
        {
            if (director != null)
            {
                // Prepare the correct layer if original layer was deleted (due to automatic deletion of empty layers)
                // GetLayer call will add a new layer if it is not found
                ImplantBuildingBlockProperties.GetLayer(BuildingBlocks.Blocks[IBB.ProductionRod], director.Document);
            }
        }
    }
}