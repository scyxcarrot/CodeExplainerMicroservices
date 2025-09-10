using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Core.Utilities
{
    /*
     * MeshAnalysis provides functionality mesh analysis like: wall thickness, m2m analysis
     */

    public static class MeshAnalysis
    {
        /*
        * This function calculates the wall thickness of a mesh
        */

        public static bool MeshThickness(Mesh mesh, out List<double> thi)
        {
            // init
            thi = new List<double>();
            List<int> dummyFaceIds;

            // Add all vertices to origins list
            List<Point3d> origins = new List<Point3d>();
            foreach (Point3f theVert in mesh.Vertices.ToList())
            {
                origins.Add((Point3d)theVert);
            }

            // Add all vertex normals to rays list
            List<Vector3d> rays = new List<Vector3d>();
            foreach (Vector3f theNormal in mesh.Normals.ToList())
            {
                rays.Add(-(Vector3d)theNormal);
            }

            // Shoot the rays
            bool success = MeshUtilities.IntersectWithRaysOnlyFirst(mesh, origins, rays, out thi, out dummyFaceIds, orTranslation: 0.001);
            if (!success)
            {
                return false;
            }

            // success
            return true;
        }

        /*
        * This function calculates the distance from the source along its vertex normal to the target mesh
        */

        public static bool MeshToMeshAnalysis(Mesh source, Mesh target, out List<double> d)
        {
            // Init
            d = new List<double>();
            List<int> dummyFaceIds;

            // Add all vertices to origins list
            List<Point3d> origins = new List<Point3d>();
            foreach (Point3f theVert in source.Vertices.ToList())
            {
                origins.Add((Point3d)theVert);
            }

            // Add all vertex normals to rays list
            List<Vector3d> rays = new List<Vector3d>();
            foreach (Vector3f theNormal in source.Normals.ToList())
            {
                rays.Add(-(Vector3d)theNormal);
            }

            // Shoot the rays
            bool success = MeshUtilities.IntersectWithRaysOnlyFirst(target, origins, rays, out d, out dummyFaceIds, orTranslation: 0.0);
            if (!success)
            {
                return false;
            }

            // success
            return true;
        }
    }
}