using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Utilities
{
    public static class ThreadSafeCommonOperations
    {
        private static readonly object MeshMeshIntersectionSync = new object();
        public static bool IntersectionMeshMeshAccurate(Brep qcCylinder, List<Mesh> meshes)
        {
            lock (MeshMeshIntersectionSync)
            {
                foreach (var obj in meshes)
                {
                    var intersections = Intersection.MeshMeshAccurate(obj, Mesh.CreateFromBrep(qcCylinder).FirstOrDefault(), 0.001);
                    if (intersections != null)
                    {
                        return true;// return immediately if its intersect with osteotomy parts
                    }
                }
                return false;
            }
        }

        private static object intersectionMeshPlaneSync = new object();
        public static Polyline[] IntersectionMeshPlane(Mesh mesh, Plane plane)
        {
            lock (intersectionMeshPlaneSync)
            {
                return Intersection.MeshPlane(mesh, plane);
            }
        }

        private static object brepCreateFromCylinderSync = new object();
        public static Brep BrepCreateFromCylinder(Cylinder cylinder)
        {
            lock (brepCreateFromCylinderSync)
            {
                return Brep.CreateFromCylinder(cylinder, true, true);
            }
        }

        private static object meshCreateFromBrepSync = new object();
        public static Mesh[] MeshCreateFromBrep(Brep brep)
        {
            lock (meshCreateFromBrepSync)
            {
                return Mesh.CreateFromBrep(brep);
            }
        }

        private static object intersectionMeshMeshFastSync = new object();
        public static Line[] IntersectionMeshMeshFast(Mesh mesh1, Mesh mesh2)
        {
            lock (intersectionMeshMeshFastSync)
            {
                return Intersection.MeshMeshFast(mesh1, mesh2);
            }
        }
    }
}
