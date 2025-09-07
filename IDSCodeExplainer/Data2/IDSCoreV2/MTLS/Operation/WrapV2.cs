using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Core.V2.Utilities;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using MtlsIds34.Array;
using MtlsIds34.MeshDesign;
using System;
using System.Runtime.ExceptionServices;

namespace IDS.Core.V2.MTLS.Operation
{
    public static class WrapV2
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
        public static bool PerformWrap(IConsole console, IMesh[] meshes, double resolution, double gapSize, double resultingOffset,
            bool protectThinWalls, bool reduceTriangles, bool preserveSharpFeatures, bool preserveSurfaces, out IMesh wrapped)
        {
            var opmesh = MeshUtilitiesV2.AppendMeshes(meshes);

            if (null != opmesh)
            {
                var helper = new MtlsIds34ContextHelper(console);
                using (var context = helper.CreateContext())
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
                        Triangles = Array2D.Create(context, opmesh.Faces.ToFacesArray2D()),
                        Vertices = Array2D.Create(context, opmesh.Vertices.ToVerticesArray2D()),
                    };

                    try
                    {
                        var result = op.Operate(context);

                        //if (result.Error == ShrinkWrapError.success)
                        {
                            var vertexArray = (double[,])result.Vertices.Data;
                            var triangleArray = (ulong[,])result.Triangles.Data;

                            wrapped = new IDSMesh(vertexArray, triangleArray);
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
