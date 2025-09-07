using IDS.Core.Operations;
using IDS.Core.Utilities;
using Rhino.Geometry;

namespace IDS.Glenius.Operations
{
    public class BasePlateMaker
    {
        public Mesh BasePlate { get; private set; }

        public bool CreateBasePlate(Curve topCurve, Curve bottomCurve, bool sideWallOnly)
        {
            BasePlate = null;

            if (topCurve != null && bottomCurve != null && topCurve.IsClosed && bottomCurve.IsClosed)
            {
                var meshingParameters = MeshParameters.IDS();
                var topMesh = Mesh.CreateFromPlanarBoundary(topCurve, meshingParameters);
                var bottomMesh = Mesh.CreateFromPlanarBoundary(bottomCurve, meshingParameters);

                if (topMesh != null && bottomMesh != null)
                {
                    BasePlate = StichMesh(topMesh, bottomMesh, !sideWallOnly);

                    if (BasePlate != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private Mesh StichMesh(Mesh topMesh, Mesh bottomMesh, bool sideWallOnly)
        {
            var mesh = MeshOperations.StitchMeshSurfaces(topMesh, bottomMesh, sideWallOnly);
            if (mesh != null)
            {
                mesh.UnifyNormals();
                if (mesh.Normals.ComputeNormals())
                {
                    return mesh;
                }
            }

            return null;
        }
    }

}