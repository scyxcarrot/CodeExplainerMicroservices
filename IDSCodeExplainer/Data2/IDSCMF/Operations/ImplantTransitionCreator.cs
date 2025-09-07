using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Query;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Geometry;
using System;

namespace IDS.CMF.Operations
{
    public class ImplantTransitionCreator
    {
        private readonly CMFImplantDirector _director;
        private readonly Mesh _supportInputsMesh;

        public ImplantTransitionCreator(CMFImplantDirector director)
        {
            _director = director;

            var objectManager = new CMFObjectManager(_director);
            var constraintMeshQuery = new ConstraintMeshQuery(objectManager);
            var placeableBoneObjects = constraintMeshQuery.GetConstraintRhinoObjectForImplant();
            var implantMargins = objectManager.GetAllBuildingBlocks(IBB.ImplantMargin);

            _supportInputsMesh = new Mesh();
            foreach (var boneObject in placeableBoneObjects)
            {
                var mesh = (Mesh)boneObject.Geometry;
                _supportInputsMesh.Append(mesh);
            }

            foreach (var marginObject in implantMargins)
            {
                var mesh = (Mesh)marginObject.Geometry;
                _supportInputsMesh.Append(mesh);
            }
        }

        public bool GenerateImplantTransition(Curve curve1, Curve curve2, out Mesh outputTransition)
        {
            try
            {
                Point3d[] firstPoints;
                Point3d[] secondPoints;
                outputTransition = StitchCurves(curve1, curve2, out firstPoints, out secondPoints);
                outputTransition = OffsetSurface(outputTransition, firstPoints, secondPoints);

                return outputTransition != null;
            }
            catch (Exception e)
            {
                Msai.TrackException(new IDSException("[DEV] ImplantTransitionCreation", e), "CMF");
                outputTransition = null;
                return false;
            }
        }

        private Mesh StitchCurves(Curve curve1, Curve curve2, out Point3d[] firstPoints, out Point3d[] secondPoints)
        {
            var stitched = new Mesh();

            var firstCurveLength = curve1.GetLength();
            var secondCurveLength = curve2.GetLength();

            //divide both curves to same number of segments
            var segmentLength = 0.1;
            int divideCount;
            if (firstCurveLength < secondCurveLength)
            {
                curve1.DivideByLength(segmentLength, true, out firstPoints);
                divideCount = firstPoints.Length;
            }
            else
            {
                curve2.DivideByLength(segmentLength, true, out secondPoints);
                divideCount = secondPoints.Length;
            }
            
            if (!Curve.DoDirectionsMatch(curve1, curve2))
            {
                curve2.Reverse();
            }

            curve1.DivideByCount(divideCount, true, out firstPoints);
            curve2.DivideByCount(divideCount, true, out secondPoints);

            for (var i = 0; i < divideCount; i++)
            {
                var a = stitched.Vertices.Add(firstPoints[i]);
                var b = stitched.Vertices.Add(secondPoints[i]);

                var c = stitched.Vertices.Add(firstPoints[i + 1]);
                var d = stitched.Vertices.Add(secondPoints[i + 1]);

                stitched.Faces.AddFace(a, b, d, c);
            }

            stitched.Faces.ConvertQuadsToTriangles();
            return stitched;
        }

        private Mesh OffsetSurface(Mesh transitionSurface, Point3d[] firstPoints, Point3d[] secondPoints)
        {
            transitionSurface.FaceNormals.ComputeFaceNormals();
            var averageSurfaceNormal = VectorUtilities.CalculateAverageNormal(transitionSurface);

            var averageNormalForCurve1 = GetAverageNormalAtPoints(firstPoints);
            var averageNormalForCurve2 = GetAverageNormalAtPoints(secondPoints);

            var needToFlipForCurve1 = RhinoMath.ToDegrees(Vector3d.VectorAngle(averageSurfaceNormal, averageNormalForCurve1)) > 90;
            var needToFlipForCurve2 = RhinoMath.ToDegrees(Vector3d.VectorAngle(averageSurfaceNormal, averageNormalForCurve2)) > 90;

            if (needToFlipForCurve1 && needToFlipForCurve2)
            {
                transitionSurface.Flip(true, true, true);
            }
            else if ((needToFlipForCurve1 && !needToFlipForCurve2) || (!needToFlipForCurve1 && needToFlipForCurve2))
            {
                //on situations where both sides (curve1 and curve2 sides) do not complement the need to flip or not, get the more protruded side and flip based on that side
                //more protruded side is determined by the average distance the side is from center; the larger the distance, the more protruded the side is
                var center = _supportInputsMesh.GetBoundingBox(true).Center;

                var averageDistanceForCurve1 = GetAverageDistanceFromCenter(center, firstPoints);
                var averageDistanceForCurve2 = GetAverageDistanceFromCenter(center, secondPoints);

                var largerAverageNormal = (averageDistanceForCurve1 > averageDistanceForCurve2)
                    ? averageNormalForCurve1
                    : averageNormalForCurve2;

                if (RhinoMath.ToDegrees(Vector3d.VectorAngle(averageSurfaceNormal, largerAverageNormal)) > 90)
                {
                    transitionSurface.Flip(true, true, true);
                }
            }

            const double thickness = 0.4;

            return transitionSurface.Offset(thickness, true);
        }

        private Vector3d GetAverageNormalAtPoints(Point3d[] points)
        {
            var averageNormal = Vector3d.Zero;

            foreach (var point in points)
            {
                var vector = VectorUtilities.FindAverageNormalAtPoint(point, _supportInputsMesh, 2.5, 1);
                averageNormal += vector;
            }

            averageNormal.Unitize();
            return averageNormal;
        }

        private double GetAverageDistanceFromCenter(Point3d center, Point3d[] points)
        {
            var totalDistance = 0.0;

            foreach (var point in points)
            {
                totalDistance += (center - point).Length;
            }

            return totalDistance / points.Length;
        }
    }
}
