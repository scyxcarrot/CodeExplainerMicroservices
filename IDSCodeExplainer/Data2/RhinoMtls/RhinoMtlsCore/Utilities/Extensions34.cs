using MtlsIds34.Array;
using MtlsIds34.Core;
using Rhino.Collections;

namespace RhinoMtlsCore.Utilities
{
    //\todo Separate all of the extensions into namespaces to categorize them
    public static class Conversion34
    {
        public static Array2D ToArray2D(this Rhino.Geometry.Collections.MeshFaceList faces, Context context)
        {
            return Array2D.Create(context, faces.ToUint64Array());
        }

        public static Array2D ToArray2D(this Rhino.Geometry.Collections.MeshVertexList vertices, Context context)
        {
            return Array2D.Create(context, vertices.ToDouble2DArray());
        }

        public static Array2D ToArray2D(this Point3dList pt3dList, Context context)
        {
            return Array2D.Create(context, pt3dList.ToDouble2DArray());
        }

        public static ulong[,] ToUint64Array(this Array2D triangles)
        {
            if (triangles == null || triangles.Data == null)
            {
                const int verticesPerTriangle = 3;
                return new ulong[0, verticesPerTriangle];
            }

            return (ulong[,])triangles.Data;
        }

        public static double[,] ToDouble2DArray(this Array2D vertices)
        {
            if (vertices == null || vertices.Data == null)
            {
                const int coordinatesPerVertex = 3;
                return new double[0, coordinatesPerVertex];
            }

            return (double[,])vertices.Data;
        }
    }
}