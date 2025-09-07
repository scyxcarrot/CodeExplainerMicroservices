using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Core.V2.MTLS.Operation;
using IDS.Core.V2.Utilities;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMFImplantCreation.Utilities
{
    public static class MeshUtilities
    {
        public static IPoint3D GetClosestPointOnMesh(IConsole console, IMesh mesh, IPoint3D point)
        {
            return Distance.PerformMeshToPointDistance(console, mesh, point).Point;
        }

        public static List<IMesh> GetSurfaces(IMesh mesh, ulong[] surfaceStructure)
        {
            var surfaceIndices = surfaceStructure.Distinct().ToArray();
            return surfaceIndices.Select(surfaceIndex => GetSurface(mesh, surfaceStructure, surfaceIndex)).ToList();
        }

        public static IMesh GetSurface(IMesh mesh, ulong[] surfaceStructure, ulong surfaceIndex)
        {
            var subSurface = new IDSMesh(mesh.Vertices.ToVerticesArray2D(), new ulong[0, 3]);

            for (ulong i = 0; i < (ulong)surfaceStructure.Length; i++)
            {
                if (surfaceStructure[i] == surfaceIndex)
                {
                    subSurface.Faces.Add(mesh.Faces[(int)i]);
                }
            }

            return subSurface;
        }

        public static List<IMesh> SplitMeshWithCurves(IConsole console, IMesh mesh, List<ICurve> curves,
            bool usePullToMesh, bool sortSmallestToLargest)
        {
            List<IMesh> parts;

            var fixedCurves = CurveUtilities.FilterNoiseCurves(console, curves, 2);

            if (!OperatorSplitWithCurve(console, mesh, fixedCurves, usePullToMesh, out parts) || parts.Count < 2)
            {
                return null;
            }

            var orderedParts = SortMeshBySurfaceArea(console, parts);
            if (!sortSmallestToLargest)
            {
                orderedParts.Reverse();
            }

            return orderedParts;
        }

        public static bool OperatorSplitWithCurve(IConsole console, IMesh inputMesh, List<ICurve> curves, bool usePullToMesh, out List<IMesh> parts)
        {
            parts = new List<IMesh>() { inputMesh };

            if (!curves.All(curve => curve.IsClosed()))
            {
                console.WriteErrorLine("Not all curves are closed.");
                return false;
            }

            var splittingCurves = new List<ICurve>();
            if (usePullToMesh)
            {
                foreach (var curve in curves)
                {
                    splittingCurves.Add(Curves.AttractCurve(console, inputMesh, curve));
                }
            }
            else
            {
                splittingCurves = curves;
            }

            // TODO: Join splitting curves
            var allCurves = new List<IPoint3D>();
            foreach (var curve in splittingCurves)
            {
                allCurves.AddRange(curve.Points);
            }

            parts = Curves.SplitWithCurve(console, inputMesh, new IDSCurve(allCurves), out _, out _);

            return true;
        }

        private static List<IMesh> SortMeshBySurfaceArea(IConsole console, List<IMesh> meshes)
        {
            return meshes.OrderBy(m => SurfaceDiagnostics.GetMeshArea(console, m)).ToList();
        }

        public static IMesh GetInnerPatch(IConsole console, IMesh mesh, ICurve curve)
        {
            if (!curve.IsClosed())
            {
                throw new Exception("Common Causes\n - Screw positioned near edge of support." +
                                    "\n - Screw positioned on highly concave/convex area of support." +
                                    "\nPlease refer to the FAQ section on the IDS website for more information.");
            }

            var splittedSurfaces =
                MeshUtilities.SplitMeshWithCurves(console, mesh, new List<ICurve> { curve }, false, true);

            if (splittedSurfaces == null || splittedSurfaces.Count == 0)
            {
                throw new Exception("Split surface failed!");
            }

            if (splittedSurfaces.Count == 1)
            {
                return splittedSurfaces.First();
            }

            IMesh singleBorderSurface = null;

            foreach (var surface in splittedSurfaces)
            {
                var contourCurves = MeshDiagnostics.FindSurfaceBorders(console, surface);
                if (contourCurves.Count == 1)                    
                {
                    singleBorderSurface = surface;

                    if (MathUtilitiesV2.IsWithin(contourCurves.First().Points.Count, curve.Points.Count - 1, curve.Points.Count + 1))
                    {
                        return surface;
                    }
                }
            }

            return singleBorderSurface != null ? singleBorderSurface : splittedSurfaces.LastOrDefault();
        }

        /// <summary>
        /// Gets the outer contour
        /// </summary>
        /// <param name="console">Console for logging purposes</param>
        /// <param name="mesh">The mesh.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Mesh has more than one valid contour!</exception>
        public static ICurve GetOuterContour(IConsole console, IMesh mesh)
        {
            int numberOfBoundaryEdges;
            EdgeDiagnostics.PerformEdgeDiagnostics(console, mesh, out numberOfBoundaryEdges);
            if (numberOfBoundaryEdges == 0)
            {
                return new IDSCurve();
            }

            var borders = EdgeDiagnostics.FindHoleBorders(console, mesh);
            if (borders.Count != 1)
            {
                throw new ArgumentException("Mesh has more than one valid contour!");
            }

            return borders[0];
        }

        public static IMesh StitchMeshSurfaces(IConsole console, IMesh top, IMesh bottom)
        {
            var topCurve = GetOuterContour(console, top);
            var bottomCurve = GetOuterContour(console, bottom);

            var stitched = Curves.TriangulateFullyBetweenCurves(
                console, topCurve, bottomCurve);
            return stitched;
        }
    }
}
