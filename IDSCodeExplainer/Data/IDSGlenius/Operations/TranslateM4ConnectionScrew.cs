using IDS.Core.Utilities;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Visualization;
using Rhino.Display;
using Rhino.Geometry;

namespace IDS.Glenius.Operations
{
    public class TranslateM4ConnectionScrew : TranslateObjectAlongHeadPlaneXY
    {
        private readonly double m4ConnectionScrewRadius; //2nd cylinder
        private readonly double minRangeOfMovement;
        private readonly double maxRangeOfMovement;

        public TranslateM4ConnectionScrew(GleniusImplantDirector director) : base(director, BuildingBlocks.Blocks[IBB.M4ConnectionScrew].Name)
        {
            material = new DisplayMaterial(Colors.Metal, 0.0);
            m4ConnectionScrewRadius = 3.0;
            minRangeOfMovement = 5.75;
            maxRangeOfMovement = 14.0; //14.5 - 0.5 (CylinderHat has a rounded edge at the top)

            var connectionScrewMesh = objectManager.GetBuildingBlock(IBB.M4ConnectionScrew).Geometry as Mesh;
            var safetyZoneBrep = objectManager.GetBuildingBlock(IBB.M4ConnectionSafetyZone).Geometry as Brep;
            connectionScrewMesh.Append(MeshUtilities.ConvertBrepToMesh(safetyZoneBrep));
            objectPreview = connectionScrewMesh;

            Plane coordinateSystem;
            if (objectManager.GetBuildingBlockCoordinateSystem(IBB.M4ConnectionScrew, out coordinateSystem))
            {
                objectCoordinateSystem = coordinateSystem;
            }
            objectPreviewOrigin = objectCoordinateSystem.Origin;
        }

        protected override void TransformBuildingBlocks(Transform transform)
        {
            var components = BuildingBlocks.GetM4ConnectionScrewComponents();
            foreach (var ibb in components)
            {
                objectManager.TransformBuildingBlock(ibb, transform);
            }

            objectCoordinateSystem.Transform(transform);
            objectManager.SetBuildingBlockCoordinateSystem(IBB.M4ConnectionScrew, objectCoordinateSystem);
        }

        protected override bool IsWithinRangeOfMovement(double distanceFromCenter)
        {
            if (distanceFromCenter - m4ConnectionScrewRadius > minRangeOfMovement && distanceFromCenter + m4ConnectionScrewRadius < maxRangeOfMovement)
            {
                return true;
            }
            return false;
        }
    }
}