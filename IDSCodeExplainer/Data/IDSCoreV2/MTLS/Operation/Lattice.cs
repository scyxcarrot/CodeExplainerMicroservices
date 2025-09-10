using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using MtlsIds34.Array;
using MtlsIds34.Lattice;
using System;
using System.Runtime.ExceptionServices;

namespace IDS.Core.V2.MTLS.Operation
{
    public class Lattice
    {
        [HandleProcessCorruptedStateExceptions]
        public static IMesh CreateMeshFromCurve(IConsole console, ICurve curve, double segmentRadius)
        {
            var curvePointArray = curve.Points.ToPointsArray2D();
            var curveSegmentArray = Curves.PopulateSegments(curvePointArray.GetLength(0), 0);

            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                var operation = new ToMesh()
                {
                    SegmentRadius = segmentRadius,
                    FractionalTriangleEdgeLength = 0.3,
                    Mode = ToMeshMode.Legacy,
                    Points = Array2D.Create(context, curvePointArray),
                };

                if (curve.IsClosed())
                {
                    operation.Segments = Array2D.Create(context, curveSegmentArray);
                }

                try
                {
                    var result = operation.Operate(context);

                    var vertexArray = (double[,])result.Vertices.Data;
                    var triangleArray = (ulong[,])result.Triangles.Data;

                    return new IDSMesh(vertexArray, triangleArray);
                }
                catch (Exception e)
                {
                    throw new MtlsException("ToMesh", e.Message);
                }
            }
        }
    }
}
