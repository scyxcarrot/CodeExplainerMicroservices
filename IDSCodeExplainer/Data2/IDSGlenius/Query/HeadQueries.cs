using IDS.Core.Utilities;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using Rhino;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace IDS.Glenius.Query
{
    public class HeadQueries
    {
        private readonly GleniusImplantDirector director;

        public HeadQueries(GleniusImplantDirector director)
        {
            this.director = director;
        }

        public static double GetHeadDiameter(HeadType type)
        {
            switch (type)
            {
                case HeadType.TYPE_36_MM:
                    return 36;
                case HeadType.TYPE_38_MM:
                    return 38;
                case HeadType.TYPE_42_MM:
                    return 42;
                default:
                    return -1;
            }
        }

        public Plane QueryCylinderHatMedialLateralPlane(bool getMedial)
        {
            var objectManager = new GleniusObjectManager(director);

            var headAlignment = new HeadAlignment(director.AnatomyMeasurements, objectManager, RhinoDoc.ActiveDoc, director.defectIsLeft);
            var vector = headAlignment.GetHeadCoordinateSystem().ZAxis;

            //Hat
            var cylinderHatMesh = MeshUtilities.ConvertBrepToMesh(objectManager.GetBuildingBlock(IBB.CylinderHat).Geometry as Brep);
            var ptCenterHat = AreaMassProperties.Compute(cylinderHatMesh).Centroid;
            var ptsProjectedOnCylinderHat = Intersection.ProjectPointsToMeshes(new[] { cylinderHatMesh }, new[] { ptCenterHat }, vector, 0);

            if (!getMedial)
            {
                vector = -vector;
            }

            var ptHatMostBottom = PointUtilities.FindFurthermostPointAlongVector(ptsProjectedOnCylinderHat, vector); // Because the points are projected both ways somehow...

            return new Plane(ptHatMostBottom, vector);
        }

    }
}
