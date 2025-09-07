using Rhino.Geometry;

namespace RhinoMtlsCore.Operations
{
    public class GeometryRemesh
    {
        public static Mesh OperatorRemesh(Mesh inmesh, double maxEdgeLength, ulong nTargetTriangles, double qualityThreshold, Mtls.Core.Buffer1D triangelIndices)
        {
            Mtls.Core.Context context = new Mtls.Core.Context();
            Mtls.Geometry.Remesh remesher = new Mtls.Geometry.Remesh(context);

            remesher.MaxEdgeLength = maxEdgeLength;
            remesher.NTargetTriangles = nTargetTriangles;
            remesher.QualityThreshold = qualityThreshold;
            remesher.TriangleIndices = triangelIndices;
            remesher.Triangles.FromInt32Array(inmesh.Faces.ToInt32Array());
            remesher.Vertices.FromDoubleArray(inmesh.Vertices.ToDoubleArray());

            var result = remesher.Operate();

            if (result.Error == Mtls.Geometry.RemeshError.SUCCESS)
            {
                return result.TriangleSurface.ToRhinoMesh();
            }
            else
            {
                return null;
            }
        }

        public static Mesh[] OperatorRemesh(Mesh[] inmeshes, double maxEdgeLength, ulong nTargetTriangles, double qualityThreshold, Mtls.Core.Buffer1D triangelIndices)
        {
            int size = inmeshes.Length;
            Mesh[] results = new Mesh[size];

            for (int i = 0; i < size; ++i)
            {
                var remeshed = OperatorRemesh(inmeshes[i], maxEdgeLength, nTargetTriangles, qualityThreshold, triangelIndices);

                if(null != remeshed)
                {
                    results[i] = remeshed;
                }
            }

            return results;
        }
    }
}
