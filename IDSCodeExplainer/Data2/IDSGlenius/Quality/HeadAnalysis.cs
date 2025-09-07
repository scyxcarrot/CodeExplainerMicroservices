using IDS.Core.Utilities;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace IDS.Glenius.Quality
{
    public class HeadAnalysis
    {
        private Mesh boneMeshWithBoneHeadThreshold;
        private Mesh boneMeshWithBoneTaperThreshold;

        public HeadAnalysis(Mesh boneMesh, double boneHeadThreshold, double boneTaperThreshold)
        {
            UpdateBoneMesh(boneMesh, boneHeadThreshold, boneTaperThreshold);
        }

        public bool PerformBoneHeadVicinityCheck(Brep headBrep)
        {
            return !HasIntersection(boneMeshWithBoneHeadThreshold, headBrep);
        }

        public bool PerformBoneTaperVicinityCheck(Brep taperBrep)
        {
            return !HasIntersection(boneMeshWithBoneTaperThreshold, taperBrep);
        }

        public void UpdateBoneMesh(Mesh boneMesh, double boneHeadThreshold, double boneTaperThreshold)
        {
            //Offset method : offsets a distance in the opposite direction of the existing vertex normals, hence the -ve value
            //2nd option: use wrap functionality in RhinoMatSdkOperations
            boneMeshWithBoneHeadThreshold = boneMesh.Offset(-boneHeadThreshold);
            boneMeshWithBoneTaperThreshold = boneMesh.Offset(-boneTaperThreshold);
        }

        private bool HasIntersection(Mesh mesh, Brep brep)
        {
            var meshFromBrep = MeshUtilities.ConvertBrepToMesh(brep);
            var intersectionLines = Intersection.MeshMeshFast(mesh, meshFromBrep);
            if (intersectionLines != null && intersectionLines.Length > 0)
            {
                return true;
            }

            return false;
        }
    }
}