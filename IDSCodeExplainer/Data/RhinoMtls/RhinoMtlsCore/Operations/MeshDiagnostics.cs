using MtlsIds34.Array;
using MtlsIds34.Core;
using MtlsIds34.MeshDesign;
using MtlsIds34.MeshFix;
using MtlsIds34.MeshInspect;
using Rhino.Geometry;
using RhinoMtlsCore.Common;
using RhinoMtlsCore.Utilities;
using System;
using System.Runtime.ExceptionServices;

namespace RhinoMtlsCore.Operations
{
    public class MeshDiagnostics
    {
        [HandleProcessCorruptedStateExceptions]
        public static MeshDiagnosticsResult GetMeshDiagnostics(Mesh mesh)
        {
            if (mesh.Faces.QuadCount > 0)
            {
                mesh.Faces.ConvertQuadsToTriangles();
            }

            using (var context = MtlsIds34Globals.CreateContext())
            {
                var faces = mesh.Faces.ToArray2D(context);
                var vertices = mesh.Vertices.ToArray2D(context);

                var diagnosticResult = GetDiagnostics(faces, vertices, context);
                var edgeDiagnostic = GetEdgeDiagnostics(faces, vertices, context);
                var overlappingTrianglesResult = GetOverlappingTriangles(faces, vertices, context);
                var shells = GetShells(faces, vertices, context);
                var holes = GetHoles(faces, vertices, context);

                return new MeshDiagnosticsResult
                {
                    NumberOfInvertedNormal = diagnosticResult.NumberOfInvertedTriangles,
                    NumberOfBadEdges = diagnosticResult.NumberOfBadEdges,
                    NumberOfBadContours = diagnosticResult.NumberOfBadContours,
                    NumberOfNearBadEdges = edgeDiagnostic.NumberOfNearBadEdges,
                    NumberOfHoles = holes.BorderRanges == null ? 0 : holes.BorderRanges.GetLength(0),
                    NumberOfShells = shells.NumberOfShells,
                    NumberOfOverlappingTriangles = overlappingTrianglesResult.OverlappingTriangles == null
                        ? 0
                        : overlappingTrianglesResult.OverlappingTriangles.GetLength(0),
                    NumberOfIntersectingTriangles = diagnosticResult.IntersectingTriangles == null
                        ? 0
                        : diagnosticResult.IntersectingTriangles.GetLength(0),
                };
            }
        }

        public static DiagnosticsResult GetDiagnostics(Array2D faces, Array2D vertices, Context context)
        {
            var diagnostics = new Diagnostics
            {
                Triangles = faces,
                Vertices = vertices
            };

            try
            {
                return diagnostics.Operate(context);

            }
            catch (Exception e)
            {
                throw new MtlsException("Diagnostics", e.Message);
            }
        }

        public static DiagnosticsResult GetFastDiagnostics(Array2D faces, Array2D vertices, Context context)
        {
            var diagnostics = new Diagnostics
            {
                Triangles = faces,
                Vertices = vertices,
                // It will skip the check for NumberOfBadEdges, NumberOfBadContours, NumberOfInvertedTriangles  
                MeasureLegacyDiagnostics = false
            };

            try
            {
                return diagnostics.Operate(context);

            }
            catch (Exception e)
            {
                throw new MtlsException("Diagnostics", e.Message);
            }
        }

        private static EdgeDiagnosticsResult GetEdgeDiagnostics(Array2D faces, Array2D vertices, Context context)
        {
            var edgeDiagnostics = new EdgeDiagnostics
            {
                Triangles = faces,
                Vertices = vertices
            };

            try
            {
                return edgeDiagnostics.Operate(context);

            }
            catch (Exception e)
            {
                throw new MtlsException("EdgeDiagnostics", e.Message);
            }
        }

        public static OverlappingTrianglesResult GetOverlappingTriangles(Array2D faces,
            Array2D vertices, Context context)
        {
            var overlappingTriangles = new OverlappingTriangles
            {
                Triangles = faces,
                Vertices = vertices
            };

            //parameter values are default values for fix wizard 3-matic Medical 15
            var angleToleranceInDegrees = 5.0000;
            overlappingTriangles.AngleTolerance = angleToleranceInDegrees;
            overlappingTriangles.DistanceTolerance = 0.1000;
            overlappingTriangles.CheckFaceToBack = true;
            overlappingTriangles.CheckFaceToFace = true;

            try
            {
                return overlappingTriangles.Operate(context);
            }
            catch (Exception e)
            {
                throw new MtlsException("OverlappingTriangles", e.Message);
            }
        }

        private static FindHoleBordersResult GetHoles(Array2D faces, Array2D vertices, Context context)
        {
            var findHoleBorders = new FindHoleBorders
            {
                Triangles = faces,
                Vertices = vertices
            };

            try
            {
                return findHoleBorders.Operate(context);
            }
            catch (Exception e)
            {
                throw new MtlsException("FindHoleBorders", e.Message);
            }
        }

        private static SplitByShellsResult GetShells(Array2D faces, Array2D vertices, Context context)
        {
            var splitByShells = new SplitByShells
            {
                Triangles = faces,
                Vertices = vertices
            };
            //splitByShells.Connectivity = SplitByShellsConnectivity.Manifold;

            try
            {
                return splitByShells.Operate(context);
            }
            catch (Exception e)
            {
                throw new MtlsException("SplitByShells", e.Message);
            }
        }

        [HandleProcessCorruptedStateExceptions]
        public static Mesh GetOverlappingTrianglesInMesh(Mesh inmesh)
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

                var result = GetOverlappingTriangles(faces, vertices, context);

                if (result.OverlappingTriangles == null)
                {
                    return null;
                }

                var indexes = (long[])result.OverlappingTriangles.Data;
                if (indexes.Length <= 0)
                {
                    return null;
                }

                return MeshUtilities.MakeRhinoMesh(vertexArray, triangleArray, indexes);
            }
        }

        public sealed class MeshDiagnosticsResult
        {
            public long NumberOfInvertedNormal;
            public long NumberOfBadEdges;
            public long NumberOfBadContours;
            public long NumberOfNearBadEdges;
            public long NumberOfHoles;
            public long NumberOfShells;
            public long NumberOfOverlappingTriangles;
            public long NumberOfIntersectingTriangles;
        }
    }
}
