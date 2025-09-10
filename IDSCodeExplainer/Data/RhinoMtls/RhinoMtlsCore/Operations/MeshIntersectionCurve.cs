using Rhino.Geometry;
using RhinoMtlsCore.Common;
using RhinoMtlsCore.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;

namespace RhinoMtlsCore.Operations
{
    public class MeshIntersectionCurve
    {
        [HandleProcessCorruptedStateExceptions]
        public static List<Curve> IntersectionCurve(Mesh mesh1, Mesh mesh2)
        {
            if (mesh1.Faces.QuadCount > 0)
            {
                mesh1.Faces.ConvertQuadsToTriangles();
            }

            if (mesh2.Faces.QuadCount > 0)
            {
                mesh2.Faces.ConvertQuadsToTriangles();
            }

            using (var context = MtlsIds34Globals.CreateContext())
            {
                var operation = new MtlsIds34.Geometry.IntersectionsMeshAndMesh();
                operation.Triangles1 = mesh1.Faces.ToArray2D(context);
                operation.Vertices1 = mesh1.Vertices.ToArray2D(context);
                operation.Triangles2 = mesh2.Faces.ToArray2D(context);
                operation.Vertices2 = mesh2.Vertices.ToArray2D(context);

                try
                {
                    var output = operation.Operate(context);

                    if (output.Points == null || output.Segments == null)
                    {
                        return new List<Curve>();
                    }

                    var vertices = output.Points.ToDouble2DArray();
                    var segments = (long[,])output.Segments.Data;
                    var curves = Curves.CreateCurvesBySegments(vertices, segments);

                    return Curve.JoinCurves(curves).ToList();
                }
                catch (Exception e)
                {
                    throw new MtlsException("IntersectionCurve", e.Message);
                }
            }
        }
    }
}