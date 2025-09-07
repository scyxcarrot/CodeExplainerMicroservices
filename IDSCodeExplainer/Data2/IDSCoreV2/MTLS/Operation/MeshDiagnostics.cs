using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Core.V2.Utilities;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using MtlsIds34.Array;
using MtlsIds34.Core;
using MtlsIds34.MeshDesign;
using MtlsIds34.MeshFix;
using MtlsIds34.MeshInspect;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace IDS.Core.V2.MTLS.Operation
{
    public static class MeshDiagnostics
    {
        public sealed class MeshDiagnosticsResult
        {
            public long NumberOfInvertedNormal { get; set; }
            public long NumberOfBadEdges { get; set; }
            public long NumberOfBadContours { get; set; }
            public long NumberOfNearBadEdges { get; set; }
            public long NumberOfHoles { get; set; }
            public long NumberOfShells { get; set; }
            public long NumberOfOverlappingTriangles { get; set; }
            public long NumberOfIntersectingTriangles { get; set; }
            public long NumberOfDegenerateTriangles { get; set; }
        }

        public sealed class MeshDimensionsResult
        {
            public double Volume;
            public double Area;
            public long NumberOfVertices;
            public long NumberOfTriangles;
            public double[] CenterOfGravity;
            public double[] BoundingBoxMin;
            public double[] BoundingBoxMax;
            public double[] Size;
        }

        /// <summary>
        /// Performs mesh diagnostics.
        /// </summary>
        /// <param name="console">The console for MTLS.</param>
        /// <param name="mesh">The mesh.</param>
        [HandleProcessCorruptedStateExceptions]
        internal static DiagnosticsResult GetDiagnostics(IConsole console, Context context, IMesh mesh)
        {
            var diagnostics = new Diagnostics()
            {
                Triangles = Array2D.Create(context, mesh.Faces.ToFacesArray2D()),
                Vertices = Array2D.Create(context, mesh.Vertices.ToVerticesArray2D())
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

        internal static OverlappingTrianglesResult GetOverlappingTriangles(IConsole console, Context context, IMesh mesh)
        {
            var overlappingTriangles = new OverlappingTriangles()
            {
                Triangles = Array2D.Create(context, mesh.Faces.ToFacesArray2D()),
                Vertices = Array2D.Create(context, mesh.Vertices.ToVerticesArray2D())
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

        [HandleProcessCorruptedStateExceptions]
        public static List<IMesh> SplitByShells(IConsole console, IMesh mesh, out ulong[] surfaceStructure)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                var shells = GetShells(console, context, mesh);

                var splitStructure = (ulong[])shells.SurfaceStructure.Data;
                surfaceStructure = splitStructure;
                var shellMeshes = MeshUtilitiesV2.GetSurfaces(mesh, splitStructure);
                return shellMeshes;
            }
        }

        private static SplitByShellsResult GetShells(IConsole console, Context context, IMesh mesh)
        {
            var splitByShells = new SplitByShells()
            {
                Triangles = Array2D.Create(context, mesh.Faces.ToFacesArray2D()),
                Vertices = Array2D.Create(context, mesh.Vertices.ToVerticesArray2D())
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
        public static MeshDimensionsResult GetMeshDimensions(IConsole console, IMesh mesh)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                var meshDimensions = new Dimensions()
                {
                    Triangles = Array2D.Create(context, mesh.Faces.ToFacesArray2D()),
                    Vertices = Array2D.Create(context, mesh.Vertices.ToVerticesArray2D()),
                    MeasureVolumeAndArea = true,
                    MeasureInertia = true,
                    MeasureBoundingBox = true
                };

                try
                {
                    var meshDimensionsResult = meshDimensions.Operate(context);
                    return new MeshDimensionsResult()
                    {
                        Volume = meshDimensionsResult.Volume,
                        Area = meshDimensionsResult.Area,
                        NumberOfVertices = meshDimensionsResult.NumberOfVertices,
                        NumberOfTriangles = meshDimensionsResult.NumberOfTriangles,
                        CenterOfGravity = new double[3] {
                            meshDimensionsResult.Inertia.a14,
                            meshDimensionsResult.Inertia.a24,
                            meshDimensionsResult.Inertia.a34
                        },
                        BoundingBoxMin = new double[3] {
                            meshDimensionsResult.BoundingBox.min.x,
                            meshDimensionsResult.BoundingBox.min.y,
                            meshDimensionsResult.BoundingBox.min.z
                        },
                        BoundingBoxMax = new double[3] {
                            meshDimensionsResult.BoundingBox.max.x,
                            meshDimensionsResult.BoundingBox.max.y,
                            meshDimensionsResult.BoundingBox.max.z
                        },
                        Size = new double[3] {
                            meshDimensionsResult.Size.x,
                            meshDimensionsResult.Size.y,
                            meshDimensionsResult.Size.z,
                        }
                    };
                }
                catch (Exception e)
                {
                    throw new MtlsException("GetMeshDimensions", e.Message);
                }
            }
        }

        [HandleProcessCorruptedStateExceptions]
        public static MeshDiagnosticsResult GetMeshDiagnostics(IConsole console, IMesh mesh)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                var diagnosticResult = GetDiagnostics(console, context, mesh);
                var edgeDiagnostic = GetEdgeDiagnostics(context, mesh);
                var overlappingTrianglesResult = GetOverlappingTriangles(console, context, mesh);
                var shells = GetShells(console, context, mesh);
                var holes = GetHoles(context, mesh);

                return new MeshDiagnosticsResult
                {
                    NumberOfInvertedNormal = diagnosticResult.NumberOfInvertedTriangles,
                    NumberOfBadEdges = diagnosticResult.NumberOfBadEdges,
                    NumberOfBadContours = diagnosticResult.NumberOfBadContours,
                    NumberOfNearBadEdges = edgeDiagnostic.NumberOfNearBadEdges,
                    NumberOfHoles = holes.BorderRanges?.GetLength(0) ?? 0,
                    NumberOfShells = shells.NumberOfShells,
                    NumberOfOverlappingTriangles =
                        overlappingTrianglesResult.OverlappingTriangles?.GetLength(0) ?? 0,
                    NumberOfIntersectingTriangles =
                        diagnosticResult.IntersectingTriangles?.GetLength(0) ?? 0,
                    NumberOfDegenerateTriangles = 
                        diagnosticResult.DegenerateTriangles?.GetLength(0) ?? 0,
                };
            }
        }

        private static EdgeDiagnosticsResult GetEdgeDiagnostics(Context context, IMesh mesh)
        {
            var edgeDiagnostics = new MtlsIds34.MeshInspect.EdgeDiagnostics()
            {
                Triangles = mesh.Faces.ToFacesArray2D(),
                Vertices = mesh.Vertices.ToVerticesArray2D()
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

        private static FindHoleBordersResult GetHoles(Context context, IMesh mesh)
        {
            var findHoleBorders = new FindHoleBorders()
            {
                Triangles = mesh.Faces.ToFacesArray2D(),
                Vertices = mesh.Vertices.ToVerticesArray2D(),
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

        [HandleProcessCorruptedStateExceptions]
        public static List<ICurve> FindSurfaceBorders(IConsole console, IMesh mesh)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                var findSurfaceBorders = new FindSurfaceBorders()
                {
                    Triangles = Array2D.Create(context, mesh.Faces.ToFacesArray2D()),
                    Vertices = Array2D.Create(context, mesh.Vertices.ToVerticesArray2D())
                };

                try
                {
                    var surfaceBorderResult = findSurfaceBorders.Operate(context);
                    var vertexIndices = (ulong[])surfaceBorderResult.BorderVertexIndices.Data;
                    var ranges = (ulong[,])surfaceBorderResult.BorderRanges.Data;
                    var curves = new List<ICurve>();

                    for (var i = 0; i < ranges.RowCount(); i++)
                    {
                        var points = new List<IPoint3D>();
                        for (var a = Convert.ToInt32(ranges[i, 0]); a < Convert.ToInt32(ranges[i, 1]); a++)
                        {
                            var index = Convert.ToInt32(vertexIndices[a]);
                            points.Add(new IDSPoint3D(mesh.Vertices[index].X,
                                mesh.Vertices[index].Y, mesh.Vertices[index].Z));
                        }

                        curves.Add(new IDSCurve(points));
                    }

                    return curves;
                }
                catch (Exception e)
                {
                    throw new MtlsException("FindBoundaryEdges", e.Message);
                }
            }
        }
    }
}
