using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using MtlsIds34.Array;
using System;
using System.Runtime.ExceptionServices;

namespace IDS.Core.V2.MTLS.Operation
{
    public static class AutoFixV2
    {
        /// <summary>
        /// Performs the automatic fix.
        /// </summary>
        /// <param name="console">The console for MTLS.</param>
        /// <param name="inmesh">The inmesh.</param>
        /// <param name="operation">The operation.</param>
        /// <param name="operationName">Operation name to display if there is exception.</param>
        /// <returns></returns>
        [HandleProcessCorruptedStateExceptions]
        private static IMesh PerformAutoFix(IConsole console, IMesh inmesh,
            MtlsIds34.MeshFix.AutoFixMethod operation, string operationName = "AutoFix")
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                var op = new MtlsIds34.MeshFix.AutoFix()
                {
                    Triangles = Array2D.Create(context, inmesh.Faces.ToFacesArray2D()),
                    Vertices = Array2D.Create(context, inmesh.Vertices.ToVerticesArray2D()),
                    Method = operation
                };

                try
                {
                    var result = op.Operate(context);
                    var vertexArray = (double[,])result.Vertices.Data;
                    var triangleArray = (ulong[,])result.Triangles.Data;

                    return new IDSMesh(vertexArray, triangleArray);
                }
                catch (Exception e)
                {
                    throw new MtlsException(operationName, e.Message);
                }
            }
        }

        /// <summary>
        /// Performs the unify on an STL.
        /// </summary>
        /// <param name="console">The console for MTLS.</param>
        /// <param name="mesh">The mesh.</param>
        /// <returns></returns>
        public static IMesh PerformUnify(IConsole console, IMesh mesh)
        {
            return PerformAutoFix(console, mesh, MtlsIds34.MeshFix.AutoFixMethod.Unify);
        }

        /// <summary>
        /// Remove noise shells of an STL.
        /// </summary>
        /// <param name="console">The console for MTLS.</param>
        /// <param name="inmesh">The inmesh.</param>
        /// <returns></returns>
        [HandleProcessCorruptedStateExceptions]
        public static IMesh RemoveNoiseShells(IConsole console, IMesh inmesh)
        {
            return PerformAutoFix(console, inmesh,
                MtlsIds34.MeshFix.AutoFixMethod.RemoveNoiseShells,
                "RemoveNoiseShells");
        }

        /// <summary>
        /// Stitch an STL.
        /// </summary>
        /// <param name="console">The console for MTLS.</param>
        /// <param name="inmesh">The inmesh.</param>
        /// <returns></returns>
        [HandleProcessCorruptedStateExceptions]
        public static IMesh PerformStitch(IConsole console, IMesh inmesh)
        {
            return PerformAutoFix(console, inmesh,
                MtlsIds34.MeshFix.AutoFixMethod.Stitch,
                "Stitch");
        }

        /// <summary>
        /// Fill holes of an STL.
        /// </summary>
        /// <param name="console">The console for MTLS.</param>
        /// <param name="inmesh">The inmesh.</param>
        /// <returns></returns>
        [HandleProcessCorruptedStateExceptions]
        public static IMesh PerformFillHoles(IConsole console, IMesh inmesh)
        {
            return PerformAutoFix(console, inmesh,
                MtlsIds34.MeshFix.AutoFixMethod.FillHoles,
                "FillHoles");
        }

        /// <summary>
        /// Basic Autofix for a mesh
        /// </summary>
        /// <param name="console">The console for MTLS.</param>
        /// <param name="inputMesh">The mesh to autofix</param>
        /// <param name="iterations">Number of iterations to perform autofix</param>
        /// <returns></returns>
        public static IMesh PerformBasicAutoFix(IConsole console, IMesh inputMesh, uint iterations)
        {
            var fixedMesh = inputMesh;
            for (var i = 0; i < iterations; i++)
            {
                fixedMesh = PerformAutoFix(console, fixedMesh,
                    MtlsIds34.MeshFix.AutoFixMethod.Basic, "Basic autofix");
            }

            return fixedMesh;
        }

        /// <summary>
        /// Remove free points.
        /// </summary>
        /// <param name="console">The console for MTLS.</param>
        /// <param name="inMesh">The inmesh.</param>
        /// <returns></returns>
        [HandleProcessCorruptedStateExceptions]
        public static IMesh RemoveFreePoints(IConsole console, IMesh inMesh)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                var removeFreePoints = new MtlsIds34.MeshFix.RemoveFreePoints()
                {
                    Triangles = inMesh.Faces.ToFacesArray2D(),
                    Vertices = inMesh.Vertices.ToVerticesArray2D(),
                };

                try
                {
                    var result = removeFreePoints.Operate(context);
                    var vertexArray = (double[,])result.Vertices.Data;
                    var triangleArray = (ulong[,])result.Triangles.Data;

                    return new IDSMesh(vertexArray, triangleArray);
                }
                catch (Exception e)
                {
                    throw new MtlsException("RemoveFreePoints", e.Message);
                }
            }
        }

        public static IMesh PerformRemoveOverlappingTriangles(IConsole console, IMesh mesh)
        {
            return PerformAutoFix(
                console,
                mesh,
                MtlsIds34.MeshFix.AutoFixMethod.FixOverlaps,
                "RemoveOverlappingTriangles");
        }

        public static IMesh PerformRemoveIntersectingTriangles(IConsole console, IMesh mesh)
        {
            return PerformAutoFix(
                console,
                mesh,
                MtlsIds34.MeshFix.AutoFixMethod.FixTriangles,
                "RemoveIntersectingTriangles");
        }

        /// <summary>
        /// smoothing to a mesh. 
        /// </summary>
        /// <param name="console">The console for MTLS.</param>
        /// <param name="inMesh">The inmesh.</param>
        /// <param name="iterations">No of time smoothing the mesh</param>
        /// <returns></returns>
        [HandleProcessCorruptedStateExceptions]
        public static IMesh SmoothMesh(IConsole console, IMesh inMesh, int iterations)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                var smoothMesh = new MtlsIds34.MeshFix.Smooth()
                {
                    Triangles = inMesh.Faces.ToFacesArray2D(),
                    Vertices = inMesh.Vertices.ToVerticesArray2D(),
                    NumberOfIterations = iterations,
                };

                try
                {
                    var result = smoothMesh.Operate(context);
                    var vertexArray = (double[,])result.Vertices.Data;
                    var triangleArray = (ulong[,])result.Triangles.Data;

                    return new IDSMesh(vertexArray, triangleArray);
                }
                catch (Exception e)
                {
                    throw new MtlsException("SmoothMesh", e.Message);
                }
            }
        }

        /// <summary>
        /// invert a normal of a mesh. 
        /// </summary>
        /// <param name="console">The console for MTLS.</param>
        /// <param name="inMesh">The inmesh.</param>
        /// <returns></returns>
        [HandleProcessCorruptedStateExceptions]
        public static IMesh InvertNormal(IConsole console, IMesh inMesh)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                var smoothMesh = new MtlsIds34.MeshFix.InvertNormals()
                {
                    Triangles = inMesh.Faces.ToFacesArray2D()
                };

                try
                {
                    var result = smoothMesh.Operate(context);
                    var vertexArray = inMesh.Vertices.ToVerticesArray2D();
                    var triangleArray = (ulong[,])result.Triangles.Data;

                    return new IDSMesh(vertexArray, triangleArray);
                }
                catch (Exception e)
                {
                    throw new MtlsException("InvertNormal", e.Message);
                }
            }
        }
    }
}
