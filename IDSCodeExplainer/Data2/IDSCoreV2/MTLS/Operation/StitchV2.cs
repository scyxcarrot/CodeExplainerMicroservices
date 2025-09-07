using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using MtlsIds34.Array;
using MtlsIds34.MeshFix;
using System;
using System.Runtime.ExceptionServices;

namespace IDS.Core.V2.MTLS.Operation
{
    public static class StitchV2
    {        /// <summary>
             /// Performs stitch vertices.
             /// (To replace Rhino.Geometry.Mesh.Vertices.CombineIdentical & Rhino.Geometry.Mesh.Weld & Rhino.Geometry.Mesh.Compact)
             /// </summary>
             /// <param name="console">The console for MTLS.</param>
             /// <param name="mesh">The mesh.</param>
             /// <param name="tolerance">Numerical tolerance to decided whether to stitch or not</param>
             /// <returns>Return the mesh if stitched mesh</returns>
        [HandleProcessCorruptedStateExceptions]
        public static IMesh PerformStitchVertices(IConsole console, IMesh mesh, double tolerance = -1.0)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                var op = new StitchVertices()
                {
                    Vertices = Array2D.Create(context, mesh.Vertices.ToVerticesArray2D()),
                    Triangles = Array2D.Create(context, mesh.Faces.ToFacesArray2D()),
                    Tolerance = tolerance
                };

                try
                {
                    var result = op.Operate(context);
                    if (!result.StitchingApplied)
                    {
                        return new IDSMesh(mesh);
                    }
                    var vertices = (double[,])result.Vertices.Data;
                    var triangles = (ulong[,])result.Triangles.Data;
                    return new IDSMesh(vertices, triangles);

                }
                catch (Exception e)
                {
                    throw new MtlsException("StitchVertices", e.Message);
                }
            }
        }
    }
}
