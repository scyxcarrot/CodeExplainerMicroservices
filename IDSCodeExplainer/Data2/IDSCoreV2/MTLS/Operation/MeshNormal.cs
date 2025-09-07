using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using MtlsIds34.Array;
using MtlsIds34.MeshInspect;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace IDS.Core.V2.MTLS.Operation
{
    public static class MeshNormal
    {
        public class NormalResult
        {
            public List<IVector3D> VertexNormals { get; set; }
            public List<IVector3D> TriangleNormals { get; set; }
            public List<IPoint3D> TriangleBarycenters { get; set; }
        }

        /// <summary>
        /// Performs the normal for vertices and triangle.
        /// </summary>
        /// <param name="console">The console for MTLS.</param>
        /// <param name="mesh">The mesh.</param>
        /// <returns>Normal result</returns>
        [HandleProcessCorruptedStateExceptions]
        public static NormalResult PerformNormal(IConsole console, IMesh mesh)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                var boolean = new Normals()
                {
                    Vertices = Array2D.Create(context, mesh.Vertices.ToVerticesArray2D()),
                    Triangles = Array2D.Create(context, mesh.Faces.ToFacesArray2D())
                };

                try
                {
                    var normalResultMtls = boolean.Operate(context);
                    var normalResult = new NormalResult
                    {
                        VertexNormals = new List<IVector3D>(),
                        TriangleNormals = new List<IVector3D>(),
                        TriangleBarycenters = new List<IPoint3D>()
                    };

                    var vertexNormals = ((double[,])normalResultMtls.VertexNormals.Data);
                    var numOfVertices = vertexNormals.GetLength(0);
                    for (var i = 0; i < numOfVertices; i++)
                    {
                        normalResult.VertexNormals.Add(new IDSVector3D(
                            vertexNormals[i, 0], 
                            vertexNormals[i, 1], 
                            vertexNormals[i, 2]));
                    }

                    var triangleNormals = ((double[,])normalResultMtls.TriangleNormals.Data);
                    var numOfTriangles = triangleNormals.GetLength(0);
                    for (var i = 0; i < numOfTriangles; i++)
                    {
                        normalResult.TriangleNormals.Add(new IDSVector3D(
                            triangleNormals[i, 0],
                            triangleNormals[i, 1],
                            triangleNormals[i, 2]));
                    }

                    var triangleBarycenters = ((double[,])normalResultMtls.TriangleBarycenters.Data);
                    for (var i = 0; i < numOfTriangles; i++)
                    {
                        normalResult.TriangleBarycenters.Add(new IDSPoint3D(
                            triangleBarycenters[i, 0],
                            triangleBarycenters[i, 1],
                            triangleBarycenters[i, 2]));
                    }

                    return normalResult;
                }
                catch (Exception e)
                {
                    throw new MtlsException("Normals", e.Message);
                }
            }
        }
    }
}
