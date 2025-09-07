using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using MtlsIds34.Array;
using MtlsIds34.PointCloud;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;

namespace IDS.Core.V2.MTLS.Operation
{
    public static class InsideMeshDiagnostic
    {
        [HandleProcessCorruptedStateExceptions]
        public static IList<bool> PointsAreInsideMesh(IConsole console, IMesh mesh, IList<IPoint3D> points)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                var insideMesh = new InsideMesh()
                {
                    Triangles = Array2D.Create(context, mesh.Faces.ToFacesArray2D()),
                    Vertices = Array2D.Create(context, mesh.Vertices.ToVerticesArray2D()),
                    Points = Array2D.Create(context, points.Select(p => (IVertex)new IDSVertex(p)).ToList().ToVerticesArray2D())
                };

                try
                {
                    var result = insideMesh.Operate(context);
                    var isInside = (byte[])result.IsInside.Data;
                    return isInside.Select(i => i == 1).ToList();
                }
                catch (Exception e)
                {
                    throw new MtlsException("InsideMesh", e.Message);
                }
            }
        }

        public static bool PointIsInsideMesh(IConsole console, IMesh mesh, IPoint3D point)
        {
            return PointsAreInsideMesh(console, mesh, new List<IPoint3D>() { point }).FirstOrDefault();
        }
    }
}
