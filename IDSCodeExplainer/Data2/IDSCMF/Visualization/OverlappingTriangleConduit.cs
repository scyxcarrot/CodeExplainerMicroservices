using Rhino.Display;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IDS.CMF.Visualization
{
    public class OverlappingTriangleConduit : DisplayConduit
    {
        private const double SphereDiameter = 3;
        private const uint MaxIterationFindMean = 3;


        private readonly Mesh _sourceMesh;
        private readonly Mesh _overlapTriangleMesh;
        private readonly BoundingBox _boxOverlapTriangleMesh;

        private readonly Color _wireFrameColor;
        private readonly DisplayMaterial _overlapTriangleMaterial;
        private readonly DisplayMaterial _attentionMaterial;
        private readonly DisplayMaterial _sourceMeshMaterial;

        private readonly List<Mesh> _attentionRegionList;

        public bool OverlappingTriangleFound { get; }

        public OverlappingTriangleConduit(Mesh sourceMesh)
        {
            _sourceMesh = sourceMesh;
            _boxOverlapTriangleMesh = sourceMesh.GetBoundingBox(false);

            _wireFrameColor = Color.OrangeRed;
            _overlapTriangleMaterial = GetOverlappingTriangleMaterial();
            _attentionMaterial = GetAttentionMaterial();
            _sourceMeshMaterial = GetSourceMeshMaterial();

            _attentionRegionList = new List<Mesh>();

            var overlapTriangleMesh = MeshDiagnostics.GetOverlappingTrianglesInMesh(sourceMesh);
            OverlappingTriangleFound = (overlapTriangleMesh != null);
            if (!OverlappingTriangleFound)
            {
                return;
            }

            _overlapTriangleMesh = overlapTriangleMesh;
            _boxOverlapTriangleMesh.Union(_overlapTriangleMesh.GetBoundingBox(false));

            var spheresPos = ClusteringVertices(_overlapTriangleMesh, SphereDiameter, MaxIterationFindMean);
            foreach (var sphere in spheresPos.Select(sphereCenter => new Sphere(sphereCenter, SphereDiameter)))
            {
                _attentionRegionList.Add(Mesh.CreateFromSphere(sphere, 100, 100));
            }
        }

        private static DisplayMaterial GetOverlappingTriangleMaterial()
        {
            return new DisplayMaterial()
            {
                Diffuse = Color.Red,
                Specular = Color.Red,
                Emission = Color.Red
            };
        }

        private static DisplayMaterial GetAttentionMaterial()
        {
            return new DisplayMaterial
            {
                Transparency = 0.9,
                Diffuse = Color.Aqua,
                Specular = Color.Aqua,
                Emission = Color.Aqua,
            };
        }

        private static DisplayMaterial GetSourceMeshMaterial()
        {
            return new DisplayMaterial
            {
                Diffuse = Color.LightGray
            };
        }

        private static IEnumerable<Point3d> ClusteringVertices(Mesh mesh, double maxDistance, uint maxIteration)
        {
            var means = mesh.Vertices.Select(vertex => new Point3d(vertex)).ToList();

            for (var i = 0; i < maxIteration; i++)
            {
                var newMeans = new List<Point3d>();
                foreach (var mean in means)
                {
                    var tmpMeans = means.Where(m => m.EpsilonEquals(mean, maxDistance)).ToList();
                    var newMean = MeanPos(tmpMeans);

                    if (newMeans.Any(point3d => point3d.EpsilonEquals(newMean, 0.1)))
                    {
                        continue;
                    }

                    newMeans.Add(newMean);
                }

                means = newMeans;
            }

            return means;
        }

        private static Point3d MeanPos(IEnumerable<Point3d> points)
        {
            var meanPos = new Point3d(0, 0, 0);
            meanPos = points.Aggregate(meanPos, (current, point3d) => current + point3d);
            return meanPos / points.Count();
        }

        protected override void CalculateBoundingBox(CalculateBoundingBoxEventArgs e)
        {
            e.IncludeBoundingBox(_boxOverlapTriangleMesh);
        }

        protected override void PostDrawObjects(DrawEventArgs e)
        {
            e.Display.DrawMeshShaded(_sourceMesh, _sourceMeshMaterial);
            if (_overlapTriangleMesh != null)
            {
                e.Display.DrawMeshShaded(_overlapTriangleMesh, _overlapTriangleMaterial);
            }
        }

        protected override void DrawOverlay(DrawEventArgs e)
        {
            if (_overlapTriangleMesh != null)
            {
                e.Display.DrawMeshWires(_overlapTriangleMesh, _wireFrameColor);
            }

            foreach (var attentionRegion in _attentionRegionList)
            {
                e.Display.DrawMeshShaded(attentionRegion, _attentionMaterial);
            }
        }

        public void CleanUp()
        {
            _overlapTriangleMaterial.Dispose();
            _attentionMaterial.Dispose();
            _attentionRegionList.ForEach(a=>a.Dispose());
            _attentionRegionList.Clear();
        }
    }
}

