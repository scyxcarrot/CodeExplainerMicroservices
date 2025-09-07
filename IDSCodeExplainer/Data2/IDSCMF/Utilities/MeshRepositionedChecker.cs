using IDS.Core.Utilities;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System;
using System.Linq;

namespace IDS.CMF.Utilities
{
    public class MeshRepositionedChecker
    {
        private static double _tolerance = 0.01;

        public bool IsMeshRepositioned(Mesh originalMesh, Mesh changedMesh)
        {
            var meshFrom = originalMesh.DuplicateMesh();
            meshFrom.Compact(); //remove free points

            var meshTo = changedMesh.DuplicateMesh();
            meshTo.Compact();

            var hasCollision = meshFrom.CollidesWith(meshTo, _tolerance);
            if (!hasCollision)
            {
                //no intersection means mesh has been repositioned
                return true;
            }

            var fromVolume = meshFrom.Volume();
            var toVolume = meshTo.Volume();

            var fromArea = meshFrom.CalculateTotalFaceArea();
            var toArea = meshTo.CalculateTotalFaceArea();

            var biggerMesh = fromVolume > toVolume ? meshFrom : meshTo;
            var smallerMesh = fromVolume > toVolume ? meshTo : meshFrom;

            //comparing smaller mesh to bigger mesh in order to reduce outliers due to loss of material
            double[] vertexDistances;
            double[] triangleCenterDistances;
            TriangleSurfaceDistance.DistanceBetween(smallerMesh, biggerMesh, out vertexDistances, out triangleCenterDistances);

            if (Math.Abs(fromVolume - toVolume) < _tolerance && Math.Abs(fromArea - toArea) < _tolerance)
            {
                //both meshes are considered to have equal shape
                //if there is any distance that is over given threshold, we conclude that the mesh has been repositioned
                return !Array.TrueForAll(vertexDistances, AbsoluteValueWithinTolerance) || !Array.TrueForAll(triangleCenterDistances, AbsoluteValueWithinTolerance);
            }

            if (IsEnoughSurfaceAreaWithinDistanceTolerance(triangleCenterDistances, smallerMesh))
            {
                //there are enough contact surface between smaller mesh and bigger mesh
                return false;
            }

            return true;
        }

        private static bool AbsoluteValueWithinTolerance(double value)
        {
            return Math.Abs(value) < _tolerance;
        }

        private static bool IsEnoughSurfaceAreaWithinDistanceTolerance(double[] distances, Mesh mesh)
        {
            //25% of contact surface
            var indexes = distances.Select((d, index) => new { index, d }).Where(x => AbsoluteValueWithinTolerance(x.d)).Select(x => x.index);

            var surfaceMesh = new Mesh();
            surfaceMesh.Vertices.AddVertices(mesh.Vertices.ToList());

            foreach (var index in indexes)
            {
                surfaceMesh.Faces.AddFace(mesh.Faces[index]);
            }

            surfaceMesh.Compact();

            var totalSurfaceArea = mesh.CalculateTotalFaceArea();
            var threshold = totalSurfaceArea * 0.25;
            var surfaces = surfaceMesh.SplitDisjointPieces();

            foreach (var surface in surfaces)
            {
                var surfaceArea = surface.CalculateTotalFaceArea();
                if (surfaceArea > threshold)
                {
                    return true;
                }
            }

            return false;
        }
    }
}