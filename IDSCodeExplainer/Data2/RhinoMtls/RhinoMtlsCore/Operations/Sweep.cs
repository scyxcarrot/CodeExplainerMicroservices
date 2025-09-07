using MtlsIds34.Array;
using MtlsIds34.Core;
using Rhino.Geometry;
using RhinoMtlsCore.Common;
using RhinoMtlsCore.Utilities;
using System;
using System.Runtime.ExceptionServices;

namespace RhinoMtlsCore.Operations
{
    public class Sweep
    {
        public static bool PerformCircularSweep(Curve curve, double radius, out Mesh meshSweep)
        {
            var curvePointArray = curve.ToDouble2DArray(100, 0.01, true);

            return PerformCircularSweep(curvePointArray, radius, out meshSweep);
        }

        [HandleProcessCorruptedStateExceptions]
        public static bool PerformCircularSweep(double[,] curvePointArray, double radius, out Mesh meshSweep)
        {
            try
            {
                using (var context = MtlsIds34Globals.CreateContext())
                {
                    var operation = new MtlsIds34.MeshDesign.Sweep
                    {
                        PathPoints = Array2D.Create(context, curvePointArray),
                        ProfilePoints = GenerateCircularProfilePoints(context, radius)
                    };

                    var result = operation.Operate(context);

                    var vertexArray = result.Vertices.ToDouble2DArray();
                    var triangleArray = result.Triangles.ToUint64Array();

                    meshSweep = MeshUtilities.MakeRhinoMesh(vertexArray, triangleArray);

                    return true;

                }
            }
            catch (Exception e)
            {
                throw new MtlsException("Sweep", e.Message);
            }
        }

        private static Array2D GenerateCircularProfilePoints(Context context, double radius)
        {
            var circle = new Circle(radius);
            var arcCurve = new ArcCurve(circle);

            Point3d[] points;
            arcCurve.DivideByLength(0.1, true, out points);

            const int coordinatesPerVertex = 2;
            var doubleArray = new double[points.Length + 1, coordinatesPerVertex];

            for (var i = 0; i < points.Length; i++)
            {
                doubleArray[i, 0] = points[i].X;
                doubleArray[i, 1] = points[i].Y;
                //strip out Z
            }

            //close the curve
            doubleArray[points.Length, 0] = points[0].X;
            doubleArray[points.Length, 1] = points[0].Y;

            return Array2D.Create(context, doubleArray);
        }
    }
}
