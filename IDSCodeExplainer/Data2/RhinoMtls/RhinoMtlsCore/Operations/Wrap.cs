using MtlsIds34.MeshDesign;
using Rhino.Geometry;
using RhinoMtlsCore.Common;
using RhinoMtlsCore.Utilities;
using System;
using System.Runtime.ExceptionServices;

namespace RhinoMtlsCore.Operations
{
    public class Wrap
    {
        /// <summary>
        /// Performs the wrap.
        /// </summary>
        /// <param name="meshes">The meshes.</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="gapSize">Size of the gap.</param>
        /// <param name="resultingOffset">The resulting offset.</param>
        /// <param name="protectThinWalls">if set to <c>true</c> [protect thin walls].</param>
        /// <param name="reduceTriangles">if set to <c>true</c> [reduce triangles].</param>
        /// <param name="preserveSharpFeatures">if set to <c>true</c> [preserve sharp features].</param>
        /// <param name="preserveSurfaces">if set to <c>true</c> [preserve surfaces].</param>
        /// <param name="wrapped">The wrapped.</param>
        /// <returns></returns>
        [HandleProcessCorruptedStateExceptions]
        [Obsolete("Please use the operation in IDS.Core.V2.MTLS.Operation")]
        public static bool PerformWrap(Mesh[] meshes, double resolution, double gapSize, double resultingOffset,
            bool protectThinWalls, bool reduceTriangles, bool preserveSharpFeatures, bool preserveSurfaces, out Mesh wrapped)
        {
            var opmesh = MeshUtilities.MergeMeshes(meshes);

            if (null != opmesh)
            {
                if (opmesh.Faces.QuadCount > 0)
                    opmesh.Faces.ConvertQuadsToTriangles();

                using (var context = MtlsIds34Globals.CreateContext())
                {
                    var op = new ShrinkWrap()
                    {
                        SmallestDetail = resolution,
                        GapSize = gapSize,
                        Offset = resultingOffset,
                        PreserveSharpFeatures = preserveSharpFeatures,
                        //PreserveSurfaces = preserveSurfaces,
                        ProtectThinWalls = protectThinWalls,
                        //ReduceTriangles = reduceTriangles,
                        //Algorithm = ShrinkWrapAlgorithm.MarchingCubes
                    };
                    op.Triangles = opmesh.Faces.ToArray2D(context);
                    op.Vertices = opmesh.Vertices.ToArray2D(context);

                    try
                    {
                        var result = op.Operate(context);

                        //if (result.Error == ShrinkWrapError.success)
                        {
                            var vertexArray = result.Vertices.ToDouble2DArray();
                            var triangleArray = result.Triangles.ToUint64Array();
                            wrapped = MeshUtilities.MakeRhinoMesh(vertexArray, triangleArray);

                            return true;
                        }
                    }
                    catch (Exception e)
                    {
                        throw new MtlsException("ShrinkWrap", e.Message);
                    }
                }
            }
            wrapped = null;
            return false;
        }
    }
}