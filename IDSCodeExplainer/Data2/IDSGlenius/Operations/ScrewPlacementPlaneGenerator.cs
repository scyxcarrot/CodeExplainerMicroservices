using IDS.Core.Utilities;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Query;
using Rhino;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace IDS.Glenius.Operations
{
    public class ScrewPlacementPlaneGenerator
    {
        private readonly GleniusImplantDirector director;

        public ScrewPlacementPlaneGenerator(GleniusImplantDirector director)
        {
            this.director = director;
        }

        public Plane GenerateHeadConstraintPlane()
        {
            GleniusObjectManager objectManager = new GleniusObjectManager(director);

            var headAlignment = new HeadAlignment(director.AnatomyMeasurements, objectManager, RhinoDoc.ActiveDoc, director.defectIsLeft);
            var vector = headAlignment.GetHeadCoordinateSystem().ZAxis;

            //Hat
            var query = new HeadQueries(director);
            var hatBottomPlane = query.QueryCylinderHatMedialLateralPlane(true);
            var cylinderHatMesh = MeshUtilities.ConvertBrepToMesh(objectManager.GetBuildingBlock(IBB.CylinderHat).Geometry as Brep);
            var ptCenterHat = AreaMassProperties.Compute(cylinderHatMesh).Centroid;

            //TaperMantle
            var taperMantleMesh = MeshUtilities.ConvertBrepToMesh(objectManager.GetBuildingBlock(IBB.TaperMantleSafetyZone).Geometry as Brep);

            var ptsProjectedOnTaperMantle = Intersection.ProjectPointsToMeshes(new[] { taperMantleMesh }, new[] { ptCenterHat }, vector, 0);
            var ptTaperMantleMostBottom = PointUtilities.FindFurthermostPointAlongVector(ptsProjectedOnTaperMantle, vector); // Because the points are projected both ways somehow...

            //Illustration
            //Cylinder Hat + Taper Mantle Safety Zone
            //
            //  +++++++++++++++++++++
            //  +                   +
            //  ++++++++++A++++++++++
            //      |     ^     |  
            //      |     :     |  
            //      |     X     |  
            //      |     :     |  
            //      |     v     |  
            //      \_____B_____/
            //
            //WHERE A IS ptHatMostBottom
            //WHERE B IS ptTaperMantleMostBottom
            //WHERE X IS offsetFromHeadCylBottom

            double offsetFromHeadCylBottom = ptTaperMantleMostBottom.DistanceTo(hatBottomPlane.Origin) / 2;
            var calibrationPlaneOrigin = hatBottomPlane.Origin + (vector * offsetFromHeadCylBottom);

            return new Plane(calibrationPlaneOrigin, vector);
        }

    }
}
