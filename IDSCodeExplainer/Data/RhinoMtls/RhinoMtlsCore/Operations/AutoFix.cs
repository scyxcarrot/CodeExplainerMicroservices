using MtlsIds34.Array;
using Rhino.Geometry;
using RhinoMtlsCore.Common;
using RhinoMtlsCore.Utilities;
using System;
using System.Linq;
using System.Runtime.ExceptionServices;

namespace RhinoMtlsCore.Operations
{
    public class AutoFix
    {
        /// <summary>
        /// Performs the automatic fix.
        /// </summary>
        /// <param name="inmesh">The inmesh.</param>
        /// <param name="operation">The operation.</param>
        /// <returns></returns>
        [Obsolete("Please use the operation in IDS.Core.V2.MTLS.Operation")]
        [HandleProcessCorruptedStateExceptions]
        private static Mesh PerformAutoFix(Mesh inmesh, MtlsIds34.MeshFix.AutoFixMethod operation)
        {
            if (inmesh.Faces.QuadCount > 0)
            {
                inmesh.Faces.ConvertQuadsToTriangles();
            }

            using (var context = MtlsIds34Globals.CreateContext())
            {
                var op = new MtlsIds34.MeshFix.AutoFix();
                op.Triangles = inmesh.Faces.ToArray2D(context);
                op.Vertices = inmesh.Vertices.ToArray2D(context);
                op.Method = operation;

                try
                {
                    var result = op.Operate(context);

                    var vertexArray = (double[,])result.Vertices.Data;
                    var triangleArray = (ulong[,])result.Triangles.Data;

                    return MeshUtilities.MakeRhinoMesh(vertexArray, triangleArray);
                }
                catch (Exception e)
                {
                    throw new MtlsException("AutoFix", e.Message);
                }
            }
        }

        /// <summary>
        /// Performs the automatic fix on a Rhino Mesh
        /// </summary>
        /// <param name="inmesh">The inmesh.</param>
        /// <param name="iterations">The iterations.</param>
        /// <returns></returns>
        public static Mesh PerformAutoFix(Mesh inmesh, uint iterations)
        {
            Mesh fixedModel = inmesh;

            for (int iter = 0; iter < iterations; ++iter)
            {
                fixedModel = PerformAutoFix(fixedModel, MtlsIds34.MeshFix.AutoFixMethod.Basic);
            }

            return fixedModel;
        }

        /// <summary>
        /// Performs the unify on an STL.
        /// </summary>
        /// <param name="mesh">The mesh.</param>
        /// <returns></returns>
        [Obsolete("Please use the operation in IDS.Core.V2.MTLS.Operation")]
        public static Mesh PerformUnify(Mesh mesh)
        {
            return PerformAutoFix(mesh, MtlsIds34.MeshFix.AutoFixMethod.Unify);
        }

        /// <summary>
        /// Remove noise shells of an STL.
        /// </summary>
        /// <param name="inmesh">The inmesh.</param>
        /// <returns></returns>
        [Obsolete("Please use the operation in IDS.Core.V2.MTLS.Operation")]
        [HandleProcessCorruptedStateExceptions]
        public static Mesh RemoveNoiseShells(Mesh inmesh)
        {
            if (inmesh.Faces.QuadCount > 0)
            {
                inmesh.Faces.ConvertQuadsToTriangles();
            }

            using (var context = MtlsIds34Globals.CreateContext())
            {
                var op = new MtlsIds34.MeshFix.AutoFix();
                op.Triangles = inmesh.Faces.ToArray2D(context);
                op.Vertices = inmesh.Vertices.ToArray2D(context);
                op.Method = MtlsIds34.MeshFix.AutoFixMethod.RemoveNoiseShells;

                try
                {

                    var result = op.Operate(context);
                    var vertexArray = result.Vertices.ToDouble2DArray();
                    var triangleArray = result.Triangles.ToUint64Array();

                    return MeshUtilities.MakeRhinoMesh(vertexArray, triangleArray);
                }
                catch (Exception e)
                {
                    throw new MtlsException("RemoveNoiseShells", e.Message);
                }
            }
        }

        public static Mesh PerformFillHoles(Mesh mesh)
        {
            return PerformAutoFix(mesh, MtlsIds34.MeshFix.AutoFixMethod.FillHoles);
        }

        public static Mesh PerformStitch(Mesh mesh)
        {
            return PerformAutoFix(mesh, MtlsIds34.MeshFix.AutoFixMethod.Stitch);
        }

        [HandleProcessCorruptedStateExceptions]
        public static Mesh PerformRemoveOverlappingTriangles(Mesh inmesh)
        {
            if (inmesh.Faces.QuadCount > 0)
            {
                inmesh.Faces.ConvertQuadsToTriangles();
            }

            using (var context = MtlsIds34Globals.CreateContext())
            {
                var vertexArray = inmesh.Vertices.ToDouble2DArray();
                var triangleArray = inmesh.Faces.ToUint64Array();

                var faces = Array2D.Create(context, triangleArray);
                var vertices = Array2D.Create(context, vertexArray);

                var result = MeshDiagnostics.GetOverlappingTriangles(faces, vertices, context);
                var fixedMesh = MeshUtilities.MakeRhinoMesh(vertexArray, triangleArray, false);

                if (result.OverlappingTriangles != null)
                {
                    var indexes = (long[])result.OverlappingTriangles.Data;
                    if (indexes.Length > 0)
                    {
                        var faceIndexes = indexes.Select(i => (int)i);
                        fixedMesh.Faces.DeleteFaces(faceIndexes, true);
                    }
                }

                fixedMesh.Faces.CullDegenerateFaces();
                return fixedMesh;
            }
        }

        [HandleProcessCorruptedStateExceptions]
        public static Mesh PerformRemoveIntersectingTriangles(Mesh inmesh)
        {
            if (inmesh.Faces.QuadCount > 0)
            {
                inmesh.Faces.ConvertQuadsToTriangles();
            }

            using (var context = MtlsIds34Globals.CreateContext())
            {
                var vertexArray = inmesh.Vertices.ToDouble2DArray();
                var triangleArray = inmesh.Faces.ToUint64Array();

                var faces = Array2D.Create(context, triangleArray);
                var vertices = Array2D.Create(context, vertexArray);

                var result = MeshDiagnostics.GetDiagnostics(faces, vertices, context);
                var fixedMesh = MeshUtilities.MakeRhinoMesh(vertexArray, triangleArray, false);

                if (result.IntersectingTriangles != null)
                {
                    var indexes = (long[])result.IntersectingTriangles.Data;
                    if (indexes.Length > 0)
                    {
                        var faceIndexes = indexes.Select(i => (int)i);
                        fixedMesh.Faces.DeleteFaces(faceIndexes, true);
                    }
                }

                fixedMesh.Faces.CullDegenerateFaces();
                return fixedMesh;
            }
        }

        /// <summary>
        /// Fix inverted normals of an STL.
        /// </summary>
        /// <param name="mesh">The mesh to be fix.</param>
        /// <returns></returns>
        [HandleProcessCorruptedStateExceptions]
        public static Mesh PerformFixNormals(Mesh mesh)
        {
            if (mesh.Faces.QuadCount > 0)
            {
                mesh.Faces.ConvertQuadsToTriangles();
            }

            using (var context = MtlsIds34Globals.CreateContext())
            {
                var op = new MtlsIds34.MeshFix.FixNormals();
                op.Triangles = mesh.Faces.ToArray2D(context);
                op.Vertices = mesh.Vertices.ToArray2D(context);

                try
                {

                    var result = op.Operate(context);
                    var vertexArray = mesh.Vertices.ToDouble2DArray();
                    var triangleArray = result.Triangles.ToUint64Array();

                    return MeshUtilities.MakeRhinoMesh(vertexArray, triangleArray);
                }
                catch (Exception e)
                {
                    throw new MtlsException("PerformFixNormals", e.Message);
                }
            }
        }
    }
}