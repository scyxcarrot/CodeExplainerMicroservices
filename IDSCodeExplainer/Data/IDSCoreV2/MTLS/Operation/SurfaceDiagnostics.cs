using IDS.Core.V2.Extensions;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using MtlsIds34.Array;
using System;
using System.Runtime.ExceptionServices;

namespace IDS.Core.V2.MTLS.Operation
{
    public static class SurfaceDiagnostics
    {        
        /// <summary>
        /// Performs surface diagnostics.
        /// </summary>
        /// <param name="console">The console for MTLS.</param>
        /// <param name="mesh">The mesh.</param>
        /// <param name="volume">The volume result return from the method</param>
        /// <param name="area">The area result return from the method</param>
        [HandleProcessCorruptedStateExceptions]
        public static void PerformSurfaceDiagnostics(IConsole console, IMesh mesh,
            out double volume, out double area)
        {
            PerformMultiSurfaceDiagnostics(console, mesh, new ulong[mesh.Faces.Count], out var volumes, out var areas);
            volume = volumes[0];
            area = areas[0];
        }

        [HandleProcessCorruptedStateExceptions]
        public static void PerformMultiSurfaceDiagnostics(IConsole console, IMesh mesh, ulong[] surfaceStructure,
            out double[] volumes, out double[] areas)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                var op = new MtlsIds34.MeshInspect.SurfaceDiagnostics()
                {
                    Triangles = Array2D.Create(context, mesh.Faces.ToFacesArray2D()),
                    Vertices = Array2D.Create(context, mesh.Vertices.ToVerticesArray2D()),
                    SurfaceStructure = Array1D.Create(context, surfaceStructure)
                };

                try
                {
                    var result = op.Operate(context);
                    volumes = (double[])result.Volumes.Data;
                    areas = (double[])result.Areas.Data;
                }
                catch (Exception e)
                {
                    throw new MtlsException("SurfaceDiagnostics", e.Message);
                }
            }
        }

        public static double GetMeshArea(IConsole console, IMesh mesh)
        {
            PerformSurfaceDiagnostics(console, mesh, out var _, out var area);
            return area;
        }
    }
}
