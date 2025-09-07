using IDS.CMFImplantCreation.Configurations;
using IDS.CMFImplantCreation.DataModel;
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
    internal static class ImplantCreationUtilities
    {
        public static IMesh RemeshAndSmoothImplant(IConsole console, IMesh implantMesh)
        {
            var remeshed1 = RemeshV2.PerformRemesh(console, implantMesh, 0.0, 0.2, 0.2, 0.01, 0.3, false, 3);

            var smoothen1 = MeshUtilitiesV2.PerformSmoothing(console, remeshed1, true, true, false, 30.0, 0.7, 3);

            var remeshed2 = RemeshV2.PerformRemesh(console, smoothen1, 0.0, 0.1, 0.2, 0.01, 0.3, false, 3);

            var smoothen2 = MeshUtilitiesV2.PerformSmoothing(console, remeshed2, true, true, false, 30.0, 0.7, 10);

            return smoothen2;
        }

        public static IMesh GetLargestSurfaceAreaShell(IConsole console, IMesh implantMesh)
        {
            MeshDiagnostics.SplitByShells(console, implantMesh, out var surfaceStructure);
            return GetSurfacesAndAreas(console, implantMesh, surfaceStructure).OrderBy(t => t.Item2).Last().Item1;
        }

        public static List<Tuple<IMesh, double>> SplitMeshWithCurveAndSortBySurfaceArea(IConsole console, IMesh mesh, ICurve curve)
        {
            Curves.SplitWithCurve(console, mesh, curve, out var meshWithSurfaceStructure, out var surfaceStructure);
            return GetSurfacesAndAreas(console, meshWithSurfaceStructure, surfaceStructure).OrderBy(t => t.Item2).ToList();
        }

        private static List<Tuple<IMesh, double>> GetSurfacesAndAreas(IConsole console, IMesh mesh, ulong[] surfaceStructure)
        {
            SurfaceDiagnostics.PerformMultiSurfaceDiagnostics(console, mesh, surfaceStructure, out var volumes, out var areas);
            var surfaces = MeshUtilities.GetSurfaces(mesh, surfaceStructure);
            return areas.Select((a, i) => new Tuple<IMesh, double>(surfaces[i], a)).ToList();
        }

        public static IMesh GeneratePastilleCylinderIntersectionMesh(IConsole console, IndividualImplantParams individualImplantParams, Pastille pastille)
        {
            var wrapRatio = individualImplantParams.WrapOperationOffsetInDistanceRatio;
            double compensatePastille;
            double wrapValue;
            CalculatePastilleParameters(pastille, wrapRatio, out wrapValue, out compensatePastille);
            return GeneratePastilleCylinderIntersectionMesh(console, pastille, compensatePastille, 0.5);
        }

        public static IMesh GeneratePastilleExtrudeCylinderIntersectionMesh(IConsole console, IndividualImplantParams individualImplantParams, Pastille pastille)
        {
            var wrapRatio = individualImplantParams.WrapOperationOffsetInDistanceRatio;
            double compensatePastille;
            double wrapValue;
            CalculatePastilleParameters(pastille, wrapRatio, out wrapValue, out compensatePastille);
            return GeneratePastilleCylinderIntersectionMesh(console, pastille, compensatePastille, 0.025);
        }

        public static IMesh GeneratePastilleSphereIntersectionMesh(IConsole console, IndividualImplantParams individualImplantParams, Pastille pastille)
        {
            var wrapRatio = individualImplantParams.WrapOperationOffsetInDistanceRatio;
            double compensatePastille;
            double wrapValue;
            CalculatePastilleParameters(pastille, wrapRatio, out wrapValue, out compensatePastille);
            return GeneratePastilleSphereIntersectionMesh(console, pastille, compensatePastille, 0.5);
        }

        public static IMesh GeneratePastilleExtrudeSphereIntersectionMesh(IConsole console, IndividualImplantParams individualImplantParams, Pastille pastille)
        {
            var wrapRatio = individualImplantParams.WrapOperationOffsetInDistanceRatio;
            double compensatePastille;
            double wrapValue;
            CalculatePastilleParameters(pastille, wrapRatio, out wrapValue, out compensatePastille);
            return GeneratePastilleSphereIntersectionMesh(console, pastille, compensatePastille, 0.025);
        }

        public static void CalculatePastilleParameters(Pastille pastille, double wrapRatio, out double wrapValue, out double compensatePastille)
        {
            var radius = pastille.Diameter * 0.5;
            wrapValue = radius * 0.5 > pastille.Thickness * wrapRatio ? pastille.Thickness : pastille.Diameter;
            compensatePastille = wrapValue * wrapRatio;
        }

        public static IMesh GeneratePastilleCylinderIntersectionMesh(IConsole console, Pastille pastille, double compensatePastille, double cylinderRadiusOffset)
        {
            var radius = pastille.Diameter * 0.5;
            var cylinderRadius = radius - compensatePastille + cylinderRadiusOffset;
            return Primitives.GenerateCylinder(console, pastille.Location, pastille.Direction, cylinderRadius, cylinderRadius * 2, 30, 1, 1);
        }

        public static IMesh GeneratePastilleSphereIntersectionMesh(IConsole console, Pastille pastille, double compensatePastille, double radiusOffset)
        {
            var radius = pastille.Diameter * 0.5 - compensatePastille + radiusOffset;
            return Primitives.GenerateSphere(console, pastille.Location, radius, 1000);
        }

        public static ICurve GetIntersectionCurveForPastille(IConsole console, IMesh intersectionCylinder, IPoint3D refPoint,
            IMesh supportMeshRoI, IVector3D direction)
        {
            var extrudeIntersectionCurves = Curves.IntersectionCurve(console, intersectionCylinder, supportMeshRoI);

            //join curve

            if (extrudeIntersectionCurves.Count == 1)
            {
                return extrudeIntersectionCurves.First();
            }

            var refCurve = Curves.IntersectionsMeshAndPlane(console, intersectionCylinder, refPoint, direction).FirstOrDefault();
            ICurve interCurve = null;
            var interCurveDist = double.MaxValue;

            foreach (var c in extrudeIntersectionCurves)
            {
                var testCurvePoints = GeometryMath.ProjectPointsOnPlane(console, c.Points.ToList(), refPoint, direction);

                var testCurve = new IDSCurve(testCurvePoints);

                var fits = refCurve.Points.Any();
                var curvePoints = refCurve.Points.ToList();
                var closestPoints = Curves.ClosestPoints(console, testCurve, curvePoints);
                for (var i = 0; i < curvePoints.Count; i++)
                {
                    var curvePoint = curvePoints[i];
                    var closestPoint = closestPoints[i];
                    fits &= closestPoint.DistanceTo(curvePoint) < 1.0;
                }

                if (fits)
                {
                    var cCurveClosestPt = Curves.ClosestPoint(console, c, refPoint);
                    var dist = cCurveClosestPt.DistanceTo(refPoint.Add(direction));

                    //If the support mesh is too thin,
                    //you can get the same curve shape on the other side of the mesh. We need to select the one that is closest to the pastille.
                    if (dist < interCurveDist)
                    {
                        interCurve = c;
                        interCurveDist = dist;
                    }
                }
            }

            if (interCurve == null)
            {
                throw new Exception("Common Causes\n - Screw positioned near edge of support." +
                                    "\n - Screw positioned on highly concave/convex area of support." +
                                    "\nPlease refer to the FAQ section on the IDS website for more information.");
            }

            //make curve close

            return interCurve;
        }

        public static IPoint3D EnsureVertexIsOnSameLevelAsThickness(IConsole console, IMesh baseSurface, IPoint3D vertex, double thickness)
        {
            var ensureVertexListIsOnSameLevelAsThickness =
                EnsureVertexListIsOnSameLevelAsThickness(console, baseSurface, 
                    new List<IPoint3D>() { vertex },
                    thickness);
            return ensureVertexListIsOnSameLevelAsThickness.First();
        }

        public static List<IPoint3D> EnsureVertexListIsOnSameLevelAsThickness(IConsole console, IMesh baseSurface, List<IPoint3D> vertexList, double thickness)
        {
            var meshToPointDistanceResults = Distance.PerformMeshToMultiPointsDistance(console, baseSurface, vertexList);

            var result = new List<IPoint3D>();
            for (var index = 0; index < meshToPointDistanceResults.Count; index++)
            {
                var closestPointUpper = meshToPointDistanceResults[index].Point;
                var resultVertex = vertexList[index];
                var dir = resultVertex.Sub(closestPointUpper);
                var dist = dir.GetLength();
                dir.Unitize();

                var distToOffset = Math.Abs(thickness - dist);
                if (dist > thickness)
                {
                    distToOffset = -distToOffset;
                }

                if (Math.Abs(distToOffset - thickness) > 0.0001)
                {
                    resultVertex = resultVertex.Add(dir.Mul(distToOffset));
                }

                result.Add(resultVertex);
            }

            return result;
        }

        public static IMesh BuildSolidMesh(IConsole console, IMesh extrusion, ref IMesh top, ref IMesh bottom,
            out IMesh stitched)
        {
            stitched = new IDSMesh();
            var trimTopCurve = CurveUtilities.GetClosedIntersectionCurve(console, top, extrusion);
            var trimBottomCurve = CurveUtilities.GetClosedIntersectionCurve(console, bottom, extrusion);

            top = MeshUtilities.GetInnerPatch(console, top, trimTopCurve);
            bottom = MeshUtilities.GetInnerPatch(console, bottom, trimBottomCurve);

            var splittedSurfaces = MeshUtilities.SplitMeshWithCurves(console, extrusion, new List<ICurve>() { trimTopCurve, trimBottomCurve }, false, false);
            if (splittedSurfaces == null || splittedSurfaces.Count == 0)
            {
                throw new Exception("Split surface failed!");
            }

            IMesh splitSurface = null;
            foreach (var surface in splittedSurfaces)
            {
                var distances = Distance.PerformMeshToMultiPointsDistance(console, surface, new List<IPoint3D> { trimTopCurve.Points[0], trimBottomCurve.Points[0] });

                var distanceToTop = distances[0].Point.DistanceTo(trimTopCurve.Points[0]);
                var distanceToBottom = distances[1].Point.DistanceTo(trimBottomCurve.Points[0]);

                if (distanceToTop < 0.01 && distanceToBottom < 0.01)
                {
                    splitSurface = surface;
                    break;
                }
            }

            stitched = splitSurface;

            var offsetMesh = new IDSMesh();
            offsetMesh.Append(top);
            offsetMesh.Append(bottom);
            offsetMesh.Append(stitched);

            return offsetMesh;
        }

        public static ICurve GetIntersectionCurveForConnection(IConsole console, IMesh implantComponent, IMesh supportMesh, IVector3D direction)
        {
            var extrudeIntersectionCurves = Curves.IntersectionCurve(console, implantComponent, supportMesh);

            //join curve

            var interCurve = CurveUtilities.FindFurthermostCurveAlongVector(extrudeIntersectionCurves, direction);

            if (!interCurve.IsClosed())
            {
                interCurve.MakeClosed(1);
            }

            if (!interCurve.IsClosed())
            {
                console.WriteErrorLine("Closing the curve to create Plate/Link failed. Please check and do some design adjustments (Usually at sharp angle/slope, control points too near, add more control points in between, etc..)");
            }

            return interCurve;
        }

        public static IMesh GetPatch(IConsole console, 
            IMesh supportRoIMesh, ICurve intersectionCurve,
            bool attractCurve = true)
        {
            if (attractCurve)
            {
                intersectionCurve = Curves.AttractCurve(console, supportRoIMesh, intersectionCurve);
            }

            var splittedSurfacesWithAreas = 
                SplitMeshWithCurveAndSortBySurfaceArea(
                    console, supportRoIMesh, intersectionCurve);
            if (splittedSurfacesWithAreas == null || 
                splittedSurfacesWithAreas.Count == 0)
            {
                throw new Exception(
                    "Patch surface failed to be created");
            }

            if (splittedSurfacesWithAreas.Count == 1)
            {
                return splittedSurfacesWithAreas.First().Item1;
            }

            var biggestSurface = splittedSurfacesWithAreas[splittedSurfacesWithAreas.Count - 1];
            splittedSurfacesWithAreas.Remove(biggestSurface); //remove biggest surface

            double minDistance = 100;
            IMesh closestPatch = null; //Need to prevent from any unrelated shells
            var closestPatchArea = 0.0;
            foreach (var surfaceWithArea in splittedSurfacesWithAreas)
            {
                var surface = surfaceWithArea.Item1;
                var surfaceArea = surfaceWithArea.Item2;
                EdgeDiagnostics.PerformEdgeDiagnostics(
                    console, surface, out var numberOfBoundaryEdges);
                if (numberOfBoundaryEdges == 0) //It can possibly be on separate shells where the split doesnt occur.
                {
                    continue;
                }

                var borders = EdgeDiagnostics.FindHoleBorders(
                    console, surface);
                var isFit = CurveFitsOnMeshBiggestBorder(
                    console, intersectionCurve, borders, 1.0);
                if (isFit)
                {
                    closestPatch = surface;
                    closestPatchArea = surfaceArea;
                    break;
                }

                var results =
                    Distance.PerformMeshToMultiPointsDistance(
                        console, surface, 
                        new List<IPoint3D> { intersectionCurve.Points[0] });
                var distance = results[0].Distance;
                if (distance < minDistance)
                {
                    if (borders.Count == 1)
                    {
                        if (closestPatch == null)
                        {
                            closestPatch = surface;
                            closestPatchArea = surfaceArea;
                            continue;
                        }

                        if (surfaceArea < closestPatchArea)
                        {
                            minDistance = distance;
                            closestPatch = surface;
                            closestPatchArea = surfaceArea;
                        }
                    }
                    else
                    {
                        if (borders.Count == 0)
                        {
                            //TODO what to do here?
                        }
                        else if (splittedSurfacesWithAreas.Count == 1)
                        {
                            closestPatch = surface;
                            closestPatchArea = surfaceArea;
                            break;
                        }
                    }
                }
            }

            return closestPatch;
        }

        private static bool CurveFitsOnMeshBiggestBorder(IConsole console,
            ICurve curve, List<ICurve> borders, double tolerance)
        {
            var longestCurve = borders[0];
            var maxLength = 0.0;

            foreach (var border in borders)
            {
                var length = Curves.GetCurveLength(console, border);
                if (length > maxLength)
                {
                    longestCurve = border;
                    maxLength = length;
                }
            }

            var diff = Math.Abs(maxLength -
                                Curves.GetCurveLength(console, curve));

            if (diff < tolerance)
            {
                var curvePoints = longestCurve.Points.ToList();
                var closestPoints = Curves.ClosestPoints(console, curve, curvePoints);
                for (var i = 0; i < curvePoints.Count; i++)
                {
                    var curvePoint = curvePoints[i];
                    var closestPoint = closestPoints[i];
                    var dist = closestPoint.DistanceTo(curvePoint);
                    if (dist > tolerance)
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }
    }
}
