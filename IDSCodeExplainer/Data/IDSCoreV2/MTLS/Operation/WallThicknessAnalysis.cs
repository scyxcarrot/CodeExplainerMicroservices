using IDS.Core.V2.Extensions;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using MtlsIds34.Array;
using MtlsIds34.MeshInspect;
using System;
using System.Runtime.ExceptionServices;

namespace IDS.Core.V2.MTLS.Operation
{
    public static class WallThicknessAnalysis
    {
        [HandleProcessCorruptedStateExceptions]
        public static bool MeshWallThicknessInMM(IConsole console, IMesh mesh, out double[] thicknesses)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                var wallThickness = new WallThickness
                {
                    Triangles = Array2D.Create(context, mesh.Faces.ToFacesArray2D()),
                    Vertices = Array2D.Create(context, mesh.Vertices.ToVerticesArray2D()),
                    MaxDistance = 10.0
                };

                try
                {
                    var result = wallThickness.Operate(context);
                    thicknesses = (double[])result.Thickness.Data;
                    //var opposite = (ulong[])result.Opposite.Data; //new return type might use in future
                    return true;
                }
                catch (Exception e)
                {
                    throw new MtlsException("WallThickness", e.Message);
                }
            }
        }
    }
}