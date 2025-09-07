using IDS.CMF.CasePreferences;
using IDS.CMF.Constants;
using IDS.CMF.CustomMainObjects;
using IDS.CMF.DataModel;
using IDS.CMF.Factory;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.RhinoFree.Utilities;
using IDS.CMF.V2.DataModel;
using IDS.CMF.V2.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.Operations;
using IDS.Core.Plugin;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Core.V2.MTLS.Operation;
using IDS.Core.V2.Utilities;
using IDS.Interface.Geometry;
using IDS.Interface.Implant;
using IDS.RhinoInterface.Converter;
using IDS.RhinoInterfaces.Converter;
using Rhino.Collections;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using RhinoMtlsCore.Operations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CurveUtilities = IDS.Core.Utilities.CurveUtilities;
using ImplantCreationErrorUtilities = IDS.CMFImplantCreation.Configurations.ErrorUtilities;
using Plane = Rhino.Geometry.Plane;
#if (INTERNAL)
using IDS.Core.NonProduction;
using System.Drawing;
#endif

namespace IDS.CMF.Utilities
{
    public static class ImplantCreationUtilities
    {
        public const string ImplantSupportRoIKeyBaseString = "ImplantSupport_ROI";

        public static IConnection FindClosestConnection(List<IConnection> connections, Point3d point)
        {
            var nearestPoint = Point3d.Unset;
            IConnection closestConnection = null;

            connections.ForEach(conn =>
            {
                var connLine = DataModelUtilities.CreateLine(conn.A.Location, conn.B.Location);
                var closest = connLine.ClosestPoint(point, true);

                if (nearestPoint == Point3d.Unset)
                {
                    closestConnection = conn;
                    nearestPoint = closest;
                }
                else
                {
                    if (closest.DistanceTo(point) < nearestPoint.DistanceTo(point))
                    {
                        closestConnection = conn;
                        nearestPoint = closest;
                    }
                }
            });

            return closestConnection;
        }

        public static IConnection CreateConnection(IDot dotA, IDot dotB, double thickness, double width,
            bool isCreatePlate)
        {
            IConnection connection;
            //factory
            if (isCreatePlate)
            {
                connection = new ConnectionPlate
                {
                    A = dotA,
                    B = dotB,
                    Thickness = thickness,
                    Width = width,
                    Id = Guid.NewGuid(),
                };

            }
            else
            {
                connection = new ConnectionLink
                {
                    A = dotA,
                    B = dotB,
                    Thickness = thickness,
                    Width = width,
                    Id = Guid.NewGuid(),
                };
            }

            return connection;
        }

        public static List<IConnection> GetAllImplantConnections(CMFImplantDirector director)
        {
            var connections = new List<IConnection>();
            director.CasePrefManager.CasePreferences.Select(c => c.ImplantDataModel).ToList().ForEach(x =>
            {
                x.ConnectionList.ToList().ForEach(d =>
                {
                    connections.Add(d);
                });
            });

            return connections;
        }

        public static IDot FindClosestControlPoint(List<IDot> dots, Point3d point)
        {
            IDot nearest = null;

            dots.ForEach(x =>
            {
                if (nearest == null && x is DotControlPoint)
                {
                    nearest = x;
                }
                else if (x is DotControlPoint && DataModelUtilities.DistanceBetween(x.Location, point) < DataModelUtilities.DistanceBetween(nearest.Location, point))
                {
                    nearest = x;
                }
            });

            return nearest;
        }

        public static DotPastille FindClosestDotPastille(List<IDot> dots, Point3d point)
        {
            DotPastille nearest = null;

            dots.ForEach(x =>
            {
                var pastille = x as DotPastille;
                if (pastille == null)
                {
                    return;
                }
                if (nearest == null)
                {
                    nearest = pastille;
                }
                else if (DataModelUtilities.DistanceBetween(pastille.Location, point) < DataModelUtilities.DistanceBetween(nearest.Location, point))
                {
                    nearest = pastille;
                }
            });

            return nearest;
        }

        public static double DistanceToImplantDataModel(ImplantDataModel implantDataModel, Point3d point)
        {
            var cumulativeLines = new List<Line>();
            implantDataModel.ConnectionList.ForEach(l =>
            {
                cumulativeLines.Add(DataModelUtilities.CreateLine(l.A.Location, l.B.Location));
            });

            var closestPoint = LineUtilities.GetClosestPoint(cumulativeLines, point);
            if (closestPoint == Point3d.Unset)
            {
                return double.MaxValue;
            }

            return closestPoint.DistanceTo(point);
        }

        public static ImplantPreferenceModel GetNearestImplantCasePreferenceModel(CMFImplantDirector director, Point3d point,
            double maxDistance)
        {
            var implantDataModel = GetNearestImplantDataModel(director, point);
            var distance = DistanceToImplantDataModel(implantDataModel, point);

            if (distance > maxDistance)
            {
                return null;
            }

            return (ImplantPreferenceModel)director.CasePrefManager.CasePreferences.FirstOrDefault(x => x.ImplantDataModel == implantDataModel);
        }

        public static ImplantDataModel GetNearestImplantDataModel(CMFImplantDirector director, Point3d point,
            double maxDistance)
        {
            var implantDataModel = GetNearestImplantDataModel(director, point);
            var distance = DistanceToImplantDataModel(implantDataModel, point);

            return distance > maxDistance ? null : implantDataModel;
        }

        //Todo  Can simplify using DistanceToImplantDataModel
        public static ImplantDataModel GetNearestImplantDataModel(CMFImplantDirector director, Point3d point)
        {
            ImplantDataModel dataModel = null;
            var nearestModel = Point3d.Unset;

            director.CasePrefManager.CasePreferences.Select(c => c.ImplantDataModel).ToList().ForEach(x =>
            {
                var implantDataModel = x;

                var cumulativeLines = new List<Line>();
                implantDataModel.ConnectionList.ForEach(l =>
                {
                    cumulativeLines.Add(DataModelUtilities.CreateLine(l.A.Location, l.B.Location));
                });

                var closestPoint = LineUtilities.GetClosestPoint(cumulativeLines, point);
                if (closestPoint == Point3d.Unset)
                {
                    return;
                }

                if (dataModel == null)
                {
                    dataModel = implantDataModel;
                    nearestModel = closestPoint;
                }
                else
                {
                    if (closestPoint.DistanceTo(point) < nearestModel.DistanceTo(point))
                    {
                        dataModel = implantDataModel;
                        nearestModel = closestPoint;
                    }
                }
            });

            return dataModel;
        }

        public class SplitConnectionDataModel
        {
            public IConnection FirstHalf { get; set; }
            public IConnection SecondHalf { get; set; }
            public IDot NewDot { get; set; }
        }

        public static SplitConnectionDataModel SplitConnectionByAddingControlPoint(IConnection connection, Point3d point, Vector3d normal, double tolerance)
        {
            var newDot = DataModelUtilities.CreateDotControlPoint(point, normal);
            return SplitConnection(connection, point, newDot, tolerance);
        }

        public static SplitConnectionDataModel SplitConnectionByAddingScrew(IConnection connection, Point3d point, Vector3d normal, double tolerance, double pastilleDiameter)
        {
            var newDot = DataModelUtilities.CreateDotPastille(point, normal, connection.Thickness, pastilleDiameter);
            return SplitConnection(connection, point, newDot, tolerance);
        }

        private static SplitConnectionDataModel SplitConnection(IConnection connection, Point3d point, IDot newDot, double tolerance)
        {
            var res = new SplitConnectionDataModel();
            var connLine = DataModelUtilities.CreateLine(connection.A.Location, connection.B.Location);
            var closest = connLine.ClosestPoint(point, true);

            if (closest.DistanceTo(point) > tolerance)
            {
                return null;
            }

            res.NewDot = newDot;

            if (DataModelUtilities.EpsilonEquals(point, connection.A.Location, 0.0001) ||
                DataModelUtilities.EpsilonEquals(point, connection.B.Location, 0.0001))
            {
                return null;
            }

            if (connection.GetType() == typeof(ConnectionPlate))
            {
                res.FirstHalf = new ConnectionPlate
                {
                    A = connection.A,
                    B = res.NewDot,
                    Thickness = connection.Thickness,
                    Width = connection.Width,
                    Id = Guid.NewGuid(),
                };

                res.SecondHalf = new ConnectionPlate
                {
                    A = res.NewDot,
                    B = connection.B,
                    Thickness = connection.Thickness,
                    Width = connection.Width,
                    Id = Guid.NewGuid(),
                };
            }
            else if (connection.GetType() == typeof(ConnectionLink))
            {
                res.FirstHalf = new ConnectionLink
                {
                    A = connection.A,
                    B = res.NewDot,
                    Thickness = connection.Thickness,
                    Width = connection.Width,
                    Id = Guid.NewGuid(),
                };

                res.SecondHalf = new ConnectionLink
                {
                    A = res.NewDot,
                    B = connection.B,
                    Thickness = connection.Thickness,
                    Width = connection.Width,
                    Id = Guid.NewGuid(),
                };
            }
            else
            {
                return null;
            }

            return res;
        }

        public static IDot FindFurthestMostDot(List<IDot> dots, Vector3d direction)
        {
            if (!dots.Any())
            {
                return null;
            }

            var points = dots.Select(x => RhinoPoint3dConverter.ToPoint3d(x.Location)).ToArray();
            var found = PointUtilities.FindFurthermostPointAlongVector(points, direction);
            return dots.Find(x => x.Location.EpsilonEquals(RhinoPoint3dConverter.ToIPoint3D(found), 0.0001));
        }

        public static void CalculatePastilleParameters(DotPastille pastille, double wrapRatio, out double wrapValue, out double compensatePastille)
        {
            var radius = pastille.Diameter * 0.5;
            wrapValue = radius * 0.5 > pastille.Thickness * wrapRatio ? pastille.Thickness : pastille.Diameter;
            compensatePastille = wrapValue * wrapRatio;
        }

        public static Mesh GeneratePastilleCylinderIntersectionMesh(DotPastille pastille, double compensatePastille, double cylinderRadiusOffset)
        {
            var radius = pastille.Diameter * 0.5;
            var axis = RhinoVector3dConverter.ToVector3d(pastille.Direction);
            var cylinderRadius = radius - compensatePastille + cylinderRadiusOffset;
            var plane = new Rhino.Geometry.Plane(RhinoPoint3dConverter.ToPoint3d(pastille.Location), axis);
            var circle = new Circle(plane, cylinderRadius);
            circle.Translate(-axis * cylinderRadius);
            var cylinder = new Cylinder(circle, cylinderRadius * 2);
            return MeshUtilities.ConvertBrepToMesh(Brep.CreateFromCylinder(cylinder, true, true), true);
        }

        public static Mesh GeneratePastilleSphereIntersectionMesh(DotPastille pastille, double compensatePastille, double radiusOffset)
        {
            var radius = pastille.Diameter * 0.5 - compensatePastille + radiusOffset;

            var sphere = new Sphere(RhinoPoint3dConverter.ToPoint3d(pastille.Location), radius);
            return MeshUtilities.ConvertBrepToMesh(Brep.CreateFromSphere(sphere), true);
        }

        public static Mesh FindSupportShell(ImplantDataModel implantDataModel, Mesh supportMesh, IEnumerable<Screw> screws)
        {
            if (supportMesh.DisjointMeshCount <= 1)
            {
                return supportMesh;
            }

            var pastilles = implantDataModel.DotList.Where(d => d is DotPastille).Cast<DotPastille>().Where(d => d.Screw != null);

            var shells = supportMesh.SplitDisjointPieces();
            foreach (var pastille in pastilles)
            {
                if (pastille?.Screw != null)
                {
                    var screwToTest = screws.First(s => s.Id == pastille.Screw.Id);
                    var intersectedShell = ScrewUtilities.FindIntersection(shells, screwToTest);
                    if (intersectedShell != null)
                    {
                        return intersectedShell;
                    }
                }
            }

            return supportMesh;
        }
        private static List<Curve> JointCurveFromPoly(List<Curve> intersectionPoly, int implantNum, int compt)
        {
            if (!intersectionPoly.Any())
            {
                throw new IDSException("No intersection happen between implant component and mesh!");
            }

#if (INTERNAL)
            for (var i = 0; i < intersectionPoly.Count; i++)
            {
                InternalUtilities.AddCurve(intersectionPoly[i], $"Curve Intersect of {compt} at {i}", $"Test Implant::Implant {implantNum}", Color.Magenta);
            }
#endif

            var joinCurves = Curve.JoinCurves(intersectionPoly, 1).ToList();
            foreach (var curve in intersectionPoly)
            {
                curve.Dispose();
            }
            intersectionPoly.Clear();

            return joinCurves;
        }

        public static Curve GetIntersectionCurve(Mesh implantComponent, Mesh supportMesh, Vector3d direction, int implantNum, int compt)
        {
            var intersectionPoly = MeshIntersectionCurve.IntersectionCurve(implantComponent, supportMesh);
            var joinCurves = JointCurveFromPoly(intersectionPoly, implantNum, compt);

            var interCurve = CurveUtilities.FindFurthermostCurveAlongVector(joinCurves, direction);

            foreach (var curve in joinCurves)
            {
                if (curve != interCurve)
                {
                    curve.Dispose();
                }
            }

            if (!interCurve.IsClosed)
            {
                interCurve.MakeClosed(1);
            }

            if (!interCurve.IsClosed)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Closing the curve to create Plate/Link failed. Please check and do some design adjustments (Usually at sharp angle/slope, control points too near, add more control points in between, etc..)");
            }

            return interCurve;
        }

        public static Curve GetIntersectionCurveForPastille(Mesh intersectionCylinder, Point3d refPoint,
            Mesh supportMeshRoI, Mesh supportMeshFull, Vector3d direction, int implantNum, int compt)
        {
            var intersectionPoly = MeshIntersectionCurve.IntersectionCurve(intersectionCylinder, supportMeshRoI);
            var joinCurves = JointCurveFromPoly(intersectionPoly, implantNum, compt);

            var testPlane = new Rhino.Geometry.Plane(refPoint, direction);
            var refPoly = Rhino.Geometry.Intersect.Intersection.MeshPlane(intersectionCylinder, testPlane).FirstOrDefault();
            var refCurve = refPoly.ToNurbsCurve();
            Curve interCurve = null;
            var interCurveDist = double.MaxValue;

            if (joinCurves.Count == 1)
            {
                interCurve = joinCurves.FirstOrDefault();
            }
            else
            {
                foreach (var c in joinCurves)
                {
                    var testCurve = CurveUtilities.ProjectContourToPlane(testPlane, c);

                    var fits = refCurve.Points.Any();
                    refCurve.Points.ToList().ForEach(pt =>
                    {
                        double closestPtParam;
                        testCurve.ClosestPoint(pt.Location, out closestPtParam);
                        var closestPt = testCurve.PointAt(closestPtParam);

                        fits &= closestPt.DistanceTo(pt.Location) < 1.0;
                    });

                    if (fits)
                    {
                        var cClosestParam = 0.0;
                        c.ClosestPoint(refPoint, out cClosestParam);
                        var cCurveClosestPt = c.PointAt(cClosestParam);
                        var dist = cCurveClosestPt.DistanceTo(refPoint + direction);

                        //If the support mesh is too thin,
                        //you can get the same curve shape on the other side of the mesh. We need to select the one that is closest to the pastille.
                        if (dist < interCurveDist)
                        {
                            interCurve = c;
                            interCurveDist = dist;
                        }
                    }
                }
            }

            if (interCurve == null)
            {
#if (INTERNAL)
                foreach (var curve in joinCurves)
                {
                    InternalUtilities.AddCurve(curve, "ErrorAnalysis", $"Test Implant", Color.Red);
                }               
#endif

                throw new IDSException(ImplantCreationErrorUtilities.ImplantCreationErrorCutoutCurveNotFound);
            }

            foreach (var curve in joinCurves)
            {
                if (curve != interCurve)
                {
                    curve.Dispose();
                }
            }

            if (!interCurve.IsClosed)
            {
                interCurve.MakeClosed(1);
            }

            if (!interCurve.IsClosed)
            {
                //Why not throw exception or return null??
                IDSPluginHelper.WriteLine(LogCategory.Error, "Closing the curve to create Pastille failed. Please check and do some design adjustments (Usually at sharp angle/slope, control points too near, add more control points in between, etc..)");
            }

            return interCurve;
        }

        public static Point3d EnsureVertexIsOnSameLevelAsThickness(Mesh baseSurface, Point3d vertex, double thickness)
        {
            var res = vertex;

            var cPtUpper = PointUtilities.FindClosestPointToMesh(res, baseSurface);
            var dir = res - cPtUpper;
            var dist = dir.Length;
            dir.Unitize();

            var distToOffset = Math.Abs(thickness - dist);
            if (dist > thickness)
            {
                distToOffset = -distToOffset;
            }

            if (Math.Abs(distToOffset - thickness) > 0.0001)
            {
                res = res + distToOffset * dir;
            }

            return res;
        }

        public static void OptimizeOffset(List<List<Point3d>> offsettedVertices, Mesh connectionSurface,
            out Mesh top, out Mesh bottom)
        {
            List<Mesh> offsetedMeshes = new List<Mesh>();
            foreach (var vertices in offsettedVertices)
            {
                var offsetted = new Mesh();
                offsetted.Vertices.AddVertices(vertices);
                offsetted.Faces.AddFaces(connectionSurface.Faces);
                offsetted.Vertices.CombineIdentical(true, true);
                offsetted.Weld(2 * Math.PI);
                offsetted.Compact();

                offsetedMeshes.Add(offsetted);
            }

            top = offsetedMeshes.Last();
            bottom = offsetedMeshes.First();

            top.Faces.CullDegenerateFaces();
            bottom.Faces.CullDegenerateFaces();

            offsetedMeshes.Clear();
        }

        public static Mesh OptimizeOffsetForPastille(List<List<Point3d>> offsettedVertices, Mesh connectionSurface, Mesh extrusion,
            out Mesh top, out Mesh bottom, out Mesh stitched)
        {
            OptimizeOffset(offsettedVertices, connectionSurface, out top, out bottom);

            if (extrusion != null)
            {
                var offsetMesh = BuildSolidMesh(extrusion, ref top, ref bottom, out stitched);
                return offsetMesh;
            }
            else
            {
                stitched = MeshOperations.StitchMeshSurfaces(top, bottom, false);

                var res = new Mesh();
                res.Append(top);
                res.Append(stitched);
                res.Append(bottom);

                var offset =  AutoFix.PerformAutoFix(res, 3);
                return offset;
            }
        }

        private static Mesh ScaleUpSurfaceForLandmark(Mesh supportMesh, double offsetDistance, Mesh surface)
        {
            var scaledSurface = surface.DuplicateMesh();
            var centroid = AreaMassProperties.Compute(scaledSurface).Centroid;
            scaledSurface.Transform(Transform.Scale(centroid, 1.2));

            var offsetted = new Mesh();
            foreach (var vertex in scaledSurface.Vertices)
            {
                var pt = EnsureVertexIsOnSameLevelAsThickness(supportMesh, vertex, offsetDistance);
                offsetted.Vertices.Add(pt);
            }

            offsetted.Faces.AddFaces(scaledSurface.Faces);
            offsetted.Vertices.CombineIdentical(true, true);
            offsetted.Weld(2 * Math.PI);
            offsetted.Compact();
            offsetted.Vertices.UseDoublePrecisionVertices = false;

            return offsetted;
        }

        public static Mesh OptimizeOffsetForLandmark(List<List<Point3d>> offsettedVertices, Mesh connectionSurface, Mesh extrusion, Mesh supportMesh, double offsetDistanceUpper,
            out Mesh top, out Mesh bottom, out Mesh stitched)
        {
            OptimizeOffset(offsettedVertices, connectionSurface, out top, out bottom);

            top = ScaleUpSurfaceForLandmark(supportMesh, offsetDistanceUpper, top);
            var offsetMesh = BuildSolidMesh(extrusion, ref top, ref bottom, out stitched);
            return offsetMesh;
        }

        private static Mesh BuildSolidMesh(Mesh extrusion, ref Mesh top, ref Mesh bottom, out Mesh stitched)
        {
            var trimTopCurve = GetIntersectionCurve(top, extrusion);
            var trimBottomCurve = GetIntersectionCurve(bottom, extrusion);

#if (INTERNAL)
            InternalUtilities.AddCurve(trimTopCurve, $"Curve Top", $"Test OffsetForPastille", Color.Magenta);
            InternalUtilities.AddCurve(trimBottomCurve, $"Curve Bottom", $"Test OffsetForPastille", Color.Magenta);
            InternalUtilities.AddObject(extrusion, $"Extrusion", $"Test OffsetForPastille");
            InternalUtilities.AddObject(top, $"Top", $"Test OffsetForPastille");
            InternalUtilities.AddObject(bottom, $"Bottom", $"Test OffsetForPastille");
#endif

            top = GetInnerPatch(top, trimTopCurve);
            bottom = GetInnerPatch(bottom, trimBottomCurve);

            var splittedSurfaces = MeshOperations.SplitMeshWithCurves(extrusion, new List<Curve>() { trimTopCurve, trimBottomCurve }, false, 100, 0.01, true);
            if (splittedSurfaces == null || splittedSurfaces.Count == 0)
            {
                throw new IDSException("Split surface failed!");
            }

            Mesh splitSurface = null;
            foreach (var surface in splittedSurfaces)
            {
                var distanceToTop = surface.ClosestPoint(trimTopCurve.PointAtStart).DistanceTo(trimTopCurve.PointAtStart);
                var distanceToBottom = surface.ClosestPoint(trimBottomCurve.PointAtStart).DistanceTo(trimBottomCurve.PointAtStart);

                if (distanceToTop < 0.01 && distanceToBottom < 0.01)
                {
                    splitSurface = surface;
                    break;
                }
            }

            stitched = splitSurface;

            var offsetMesh = new Mesh();
            offsetMesh.Append(top);
            offsetMesh.Append(bottom);
            offsetMesh.Append(stitched);
            offsetMesh.UnifyNormals();

            return offsetMesh;
        }

        public static Mesh WrapOffset(Mesh offsetMesh, double smallestDetail, double gapClosingDistance, double wrapValue)
        {
            Mesh wrappedMesh = null;
            if (!Wrap.PerformWrap(new Mesh[] { offsetMesh }, smallestDetail, gapClosingDistance, wrapValue, false, false, false, false, out wrappedMesh))
            {
                throw new IDSException("wrapped plate tube failed.");
            }
            if (wrappedMesh == null)
            {
                throw new IDSException("wrapped implant plate failed.");
            }

            offsetMesh.Dispose();

            return wrappedMesh;
        }

        public static Mesh OptimizeOffsetandWrap(List<List<Point3d>> offsettedVertices, Mesh connectionSurface,
            double smallestDetail, double gapClosingDistance, double wrapValue)
        {
            Mesh top;
            Mesh bottom;
            OptimizeOffset(offsettedVertices, connectionSurface, out top, out bottom);

            var stitched = MeshOperations.StitchMeshSurfaces(top, bottom, false);
            var offsetMesh = new Mesh();
            offsetMesh.Append(top);
            offsetMesh.Append(bottom);
            offsetMesh.Append(stitched);
            offsetMesh.UnifyNormals();

            var wrappedMesh = WrapOffset(offsetMesh, smallestDetail, gapClosingDistance, wrapValue);

            return wrappedMesh;
        }

        private static Mesh GetInnerPatch(Mesh mesh, Curve curve)
        {
            var tmpCurve = curve.PullToMesh(mesh, 1);

            if (!tmpCurve.IsClosed)
            {
                throw new IDSException(ImplantCreationErrorUtilities.ImplantCreationErrorCurveNotClosed);
            }

            var splittedSurfaces = MeshOperations.SplitMeshWithCurves(mesh, new List<Curve>() { tmpCurve }, true);
            if (splittedSurfaces == null || splittedSurfaces.Count == 0)
            {
                throw new IDSException("Split surface failed!");
            }

            if (splittedSurfaces.Count == 1)
            {
                return splittedSurfaces.First();
            }

            foreach (var surface in splittedSurfaces)
            {
                var contourCurves = CurveUtilities.GetValidContourCurve(surface, false);

                if (contourCurves.Count == 1 &&
                    MathUtilities.IsWithin(contourCurves[0].GetLength(), curve.GetLength() - 1, curve.GetLength() + 1))
                {
                    return surface;
                }
            }

            return splittedSurfaces.LastOrDefault();
        }

        private static Curve GetIntersectionCurve(Mesh mesh1, Mesh mesh2)
        {
            var curves = MeshIntersectionCurve.IntersectionCurve(mesh1, mesh2);
            var joinCurves = Curve.JoinCurves(curves, 1).ToList();
            var curve = joinCurves.OrderBy(c => c.GetLength()).Last();
            if (!curve.IsClosed)
            {
                curve.MakeClosed(1);
            }
            return curve;
        }

        public static List<IDot> GetAllExistingDots(CMFImplantDirector director)
        {
            var dots = new List<IDot>();

            director.CasePrefManager.CasePreferences.ForEach(cp =>
            {
                dots.AddRange(cp.ImplantDataModel.DotList);
            });

            return dots;
        }

        public static DotPastille GetDotPastille(Screw screw)
        {
            DotPastille pastille = null;

            foreach (var cp in screw.Director.CasePrefManager.CasePreferences)
            {
                var dotList = cp.ImplantDataModel.DotList;
                foreach (var dot in dotList)
                {
                    var dotPastille = dot as DotPastille;
                    if (dotPastille != null && dotPastille.Screw != null && dotPastille.Screw.Id == screw.Id)
                    {
                        pastille = dotPastille;
                        break;
                    }
                }
            }

            return pastille;
        }

        public static List<Curve> CreateImplantConnectionCurves(IEnumerable<IConnection> ConnectionList, 
            IEnumerable<IConnection> additionalLink = null, bool isUsingV2Creator = true, bool linkIConnectionToRhinoCurve = false)
        {
            var res = new List<Curve>();

            List<DotCurveDataModel> dataModels;
            if (isUsingV2Creator)
            {
                dataModels =
                    CreateImplantConnectionCurveDataModelsV2(ConnectionList, false, linkIConnectionToRhinoCurve);
            }
            else
            {
                dataModels =
                    CreateImplantConnectionCurveDataModels(ConnectionList, false, linkIConnectionToRhinoCurve);
            }
            res.AddRange(dataModels.Select(dm => dm.Curve));

            if (additionalLink != null)
            {
                ImplantCreationUtilitiesRhinoFree.CreateDotCluster(additionalLink.ToList()).ForEach(cluster =>
                {
                    var points = cluster.Select(x => RhinoPoint3dConverter.ToPoint3d(x.Location)).ToList();
                    var curve = CurveUtilities.BuildCurve(points, 3, false);
                    res.Add(curve);
                });
            }

            return res;
        }

        public static List<DotCurveDataModel> CreateImplantConnectionCurveDataModelsV2(IEnumerable<IConnection> connectionEnumerable, 
            bool getConnectionProperties = true, bool linkIConnectionToRhinoCurve = false)
        {
            var res = new List<DotCurveDataModel>();
            var connectionList = connectionEnumerable.ToList();

            var dotCluster =
                ConnectionUtilities.CreateDotCluster(connectionList);
            foreach (var cluster in dotCluster)
            {
                if (cluster.Count < 2)
                {
                    continue;
                }
                var points = cluster.Select(dot => dot.Location).ToList();

                var curve = CreateSplineUtilities.FitCurve(points, 3, 0.001, SimplificationAlgorithm.Linear);
                var rhinoCurve = curve.ToRhinoPolyCurve();

                if (linkIConnectionToRhinoCurve)
                {
                    if (!AddArchivableDictionaryToCurve(ref rhinoCurve, cluster, connectionList))
                    {
                        throw new Exception("Failed to serialize Dots in Connection Curve creation result!");
                    }
                }

                var dataModel = new DotCurveDataModel
                {
                    Curve = rhinoCurve,
                    Dots = cluster
                };
                if (getConnectionProperties)
                {
                    GetConnectionProperties(
                        rhinoCurve, connectionList,
                        out var connectionWidth,
                        out var connectionThickness,
                        out var averageVector);
                    dataModel.ConnectionWidth = connectionWidth;
                    dataModel.ConnectionThickness = connectionThickness;
                    dataModel.AverageVector = averageVector;
                }
                res.Add(dataModel);
            }
            return res;
        }

        public static List<DotCurveDataModel> CreateImplantConnectionCurveDataModels(IEnumerable<IConnection> connectionEnumerable, 
            bool getConnectionProperties = true, bool linkIConnectionToRhinoCurve = false)
        {
            var res = new List<DotCurveDataModel>();
            var connectionList = connectionEnumerable.ToList();

            ImplantCreationUtilitiesRhinoFree.CreateDotCluster(connectionList).ForEach(cluster =>
            {
                var points = cluster.Select(x => RhinoPoint3dConverter.ToPoint3d(x.Location)).ToList();
                var curve = CurveUtilities.BuildCurve(points, 3, false);

                if (linkIConnectionToRhinoCurve)
                {
                    if (!AddArchivableDictionaryToCurve(ref curve, cluster, connectionList))
                    {
                        throw new Exception("Failed to serialize Dots in Connection Curve creation result!");
                    }
                }

                var dataModel = new DotCurveDataModel 
                {
                    Curve = curve,
                    Dots = cluster
                };

                if (getConnectionProperties)
                {
                    GetConnectionProperties(curve, connectionList, 
                        out var connectionWidth, 
                        out var connectionThickness, 
                        out var averageVector);
                    dataModel.ConnectionWidth = connectionWidth;
                    dataModel.ConnectionThickness = connectionThickness;
                    dataModel.AverageVector = averageVector;
                }

                res.Add(dataModel);
            });

            return res;
        }
        
        private static void GetConnectionProperties(Curve connectionCurve, List<IConnection> connectionList,
            out double connectionWidth, out double connectionThickness, out Vector3d averageVector)
        {
            var vectorList = new List<Vector3d>();
            var totalVector = new Vector3d(0, 0, 0);

            var connections = DataModelUtilities.GetConnections(connectionCurve, connectionList);

            foreach (var connection in connections)
            {
                vectorList.Add(RhinoVector3dConverter.ToVector3d(connection.A.Direction));
                vectorList.Add(RhinoVector3dConverter.ToVector3d(connection.B.Direction));
            }

            connectionWidth = connections[0].Width;
            connectionThickness = connections[0].Thickness;


            vectorList.ForEach(vec => totalVector = Vector3d.Add(totalVector, vec));
            averageVector = Vector3d.Divide(totalVector, vectorList.Count);
        }

        public static double GetImplantPointCheckRoICreationTriggerTolerance(double pastilleDiameter)
        {
            var toleranceBase = pastilleDiameter / 2 > Constants.ImplantCreation.DotMeshDistancePullTolerance
                ? pastilleDiameter / 2
                : Constants.ImplantCreation.DotMeshDistancePullTolerance;
            toleranceBase += 0.5;
            return toleranceBase;
        }

        public static bool IsNeedCreateNewImplantRoIMetadata(Point3d testPt, double pastilleDiameter, Mesh implantSupportRoISurface)
        {
            //The point is out of the implantSupportRoISurface
            if (implantSupportRoISurface.ClosestPoint(testPt).DistanceTo(testPt) > Constants.ImplantCreation.DotMeshDistancePullTolerance)
            {
                return true;
            }

            var toleranceBase = GetImplantPointCheckRoICreationTriggerTolerance(pastilleDiameter);
            //If the sphere intersected with the border of implantSupportRoISurface then it is too near the boundary, need to regenrate the RoI
            var sphere = new Sphere(testPt, toleranceBase);
            var sphereBrep = Brep.CreateFromSphere(sphere);
            return IsIntersectWithImplantRoI(sphereBrep, implantSupportRoISurface);
        }

        public static bool IsNeedCreateNewImplantRoIMetadata(Curve testCurve, double pipeRadius, Mesh implantSupportRoISurface)
        {
            var pipeBrep = BrepUtilities.Append(Brep.CreatePipe(testCurve, pipeRadius, false, PipeCapMode.Round, false, 0.1, 0.1));
            return IsIntersectWithImplantRoI(pipeBrep, implantSupportRoISurface);
        }

        private static bool IsIntersectWithImplantRoI(Brep brep, Mesh implantSupportRoISurface)
        {
            var nakedEdges = implantSupportRoISurface.GetNakedEdges();

            if (nakedEdges == null) //Only true if the surface is a closed mesh
            {
                return true;
            }

            var borders = nakedEdges.Select(x => x.ToNurbsCurve()).Where(x => x.IsClosed);

            foreach (var nurbsCurve in borders)
            {
                Curve[] dummyCurves;
                Point3d[] dummyPts;
                Intersection.CurveBrep(nurbsCurve, brep, 0.1, out dummyCurves, out dummyPts);

                if (dummyPts.Any())
                {
                    return true;
                }
            }

            return false;
        }

        public static string GenerateImplantRoIVolumeKey(CasePreferenceDataModel cp)
        {
            return $"{ImplantSupportRoIKeyBaseString}_Volume_{cp.CaseGuid}";
        }

        public static string GenerateImplantRoISurfaceKey(CasePreferenceDataModel cp)
        {
            return $"{ImplantSupportRoIKeyBaseString}_Surface_{cp.CaseGuid}";
        }

        public static Mesh InvalidateImplantRoIMetadataHelper(CMFObjectManager objectManager, ref RhinoObject implantSupportRhObj,
            CasePreferenceDataModel casePrefDataModel, IEnumerable<IConnection> additionalLink = null)
        {
            Msai.TrackDevEvent($"RoI generation for {casePrefDataModel.CaseName}", "CMF");
            IDSPluginHelper.WriteLine(LogCategory.Default, $"CREATING - Implant RoI for {casePrefDataModel.CaseName}");
            Mesh roiLowLoDSurface = null, roiVolume = null;
            Mesh impSupportLowLoD = null, impSupportFullLoD = null;
            var createRoiSuccess = true;
            var supportRoiFailedMessage =
                "Implant Support is not matching with design! Please create a support that sits below the implant designs. If this is not the case kindly add more control points so the connections are closer to the surface." +
                "\nAs a work-around the entire support is used.";
            var keyRoiSurfaceString = GenerateImplantRoISurfaceKey(casePrefDataModel);

            impSupportFullLoD = ((Mesh)implantSupportRhObj.Geometry).DuplicateMesh();

            try
            {
                if (!objectManager.GetBuildingBlockLoDLow(implantSupportRhObj.Id, out impSupportLowLoD))
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, "Low LoD of implant support failed to be obtained!");
                    throw new Exception("Implant Support low LoD Failed to get!");
                }

                var roiSuccess = false;
                
                if ((additionalLink != null) && 
                    (implantSupportRhObj.Attributes.UserDictionary.ContainsKey(keyRoiSurfaceString)))
                {
                    roiLowLoDSurface = (Mesh)implantSupportRhObj.Attributes.UserDictionary[keyRoiSurfaceString];
                    if (additionalLink.Any())
                        roiLowLoDSurface = GrowthImplantRoISurface(impSupportLowLoD, roiLowLoDSurface, casePrefDataModel.CasePrefData.PastilleDiameter + Constants.ImplantCreation.RoIAreaRadiusOffsetModifier,
                        additionalLink);
                }
                else
                {
                    roiLowLoDSurface = CreateImplantRoISurface(impSupportLowLoD, casePrefDataModel.CasePrefData.PastilleDiameter + Constants.ImplantCreation.RoIAreaRadiusOffsetModifier,
                                    casePrefDataModel.ImplantDataModel.ConnectionList);
                }

                if (roiLowLoDSurface != null)
                {
                    var roiGapClosingDistance = 0;
                    var roiLevelOfDetail = 5;
                    var roiReduceTriangles = true; // \todo double check
                    var roiPreserveSharpFeatures = false;
                    var roiProtectThinWalls = false;
                    var roiPreserveSurfaces = false;

                    roiSuccess = Wrap.PerformWrap(new[] { roiLowLoDSurface }, roiLevelOfDetail, roiGapClosingDistance, 4,
                        roiProtectThinWalls, roiReduceTriangles, roiPreserveSharpFeatures, roiPreserveSurfaces, out roiVolume);
                }

                if (roiSuccess)
                {
                    roiVolume = Booleans.PerformBooleanIntersection(roiVolume, impSupportFullLoD);
                }
                else
                {
                    createRoiSuccess = false;
                }
            }
            catch (Exception e)
            {
                Msai.TrackException(e, "CMF");
                createRoiSuccess = false;
            }

            var keyRoiVolumeString = GenerateImplantRoIVolumeKey(casePrefDataModel);
            keyRoiSurfaceString = GenerateImplantRoISurfaceKey(casePrefDataModel);

            var toRemoveKeys = new List<string>()
            {
                keyRoiVolumeString,
                keyRoiSurfaceString,
            };

            foreach (var removeKey in toRemoveKeys)
            {
                if (implantSupportRhObj.Attributes.UserDictionary.ContainsKey(removeKey))
                {
                    implantSupportRhObj.Attributes.UserDictionary.Remove(removeKey);
                }
            }

            if (!createRoiSuccess)
            {

                IDSPluginHelper.WriteLine(LogCategory.Error, supportRoiFailedMessage);
                return impSupportFullLoD;
            }

            implantSupportRhObj.Attributes.UserDictionary.Set(keyRoiVolumeString, roiVolume);
            implantSupportRhObj.Attributes.UserDictionary.Set(keyRoiSurfaceString, roiLowLoDSurface);
            impSupportFullLoD.Dispose();

            return roiVolume;
        }

        public static Mesh CreateImplantRoISurface(Mesh fullImplantSupport, double offset, IEnumerable<IConnection> links)
        {
            var allTubes = new Mesh();
            var checkingTubes = new Mesh();
            var connectionCurves = CreateImplantConnectionCurves(links);

            foreach (var curve in connectionCurves)
            {
                var pulledCurve = curve.PullToMesh(fullImplantSupport, 2.0);

                Mesh tube;
                var succesMeshFromPolyline = TubeFromPolyline.PerformMeshFromPolyline(pulledCurve, offset, out tube);
                if (!succesMeshFromPolyline)
                {
                    continue;
                }

                allTubes.Append(tube);

                Mesh smallTube;
                succesMeshFromPolyline = TubeFromPolyline.PerformMeshFromPolyline(pulledCurve, 1.0, out smallTube);
                if (!succesMeshFromPolyline)
                {
                    continue;
                }

                checkingTubes.Append(smallTube);
            }

            allTubes = AutoFix.PerformUnify(allTubes);

            var intersectionCurves = MeshIntersectionCurve.IntersectionCurve(allTubes, fullImplantSupport);

            if (!intersectionCurves.Any())
            {
                return null;
            }

            var curves = intersectionCurves.OrderBy(x => x.GetLength()).ToList();
            curves.ForEach(x =>
            {
                if (!x.IsClosed)
                {
                    x.MakeClosed(2);
                }
            });

            curves = CurveUtilities.FilterNoiseCurves(curves).Where(x => x.IsClosed).ToList();

            var meshNoiseShellsRemoved = AutoFix.RemoveNoiseShells(fullImplantSupport);
            var res = new List<Mesh>();
            var splittedSurfaces = MeshOperations.SplitMeshWithCurves(meshNoiseShellsRemoved, curves, true);
            foreach (var surface in splittedSurfaces)
            {
                var lines = Intersection.MeshMeshFast(surface, checkingTubes);
                if (lines == null || !lines.Any())
                {
                    continue;
                }
                res.Add(surface);
            }

            var roi = MeshUtilities.AppendMeshes(res);

            //MeshRepair - Partially...
            roi.ExtractNonManifoldEdges(true);
            roi.Faces.ExtractDuplicateFaces();

            return roi;
        }

        public static Mesh GrowthImplantRoISurface(Mesh fullImplantSupport, Mesh oldRoI, double offset, IEnumerable<IConnection> newLink)
        {
            var newRoI = CreateImplantRoISurface(fullImplantSupport, offset, newLink);
            var RoIs = new List<Mesh> { oldRoI, newRoI };

            var splittedCurves = new List<Curve>();
            foreach (var RoI in RoIs)
            {
                var nakedEdges = RoI.GetNakedEdges()
                    .Select(x => x.ToNurbsCurve()).ToList();
                var borders = nakedEdges.Where(x => x.IsClosed).ToList();

                foreach (var border in borders)
                {
                    var pulledCurves = border.PullToMesh(fullImplantSupport, 0.1);
                    if (pulledCurves.IsClosed)
                    {
                        splittedCurves.Add(pulledCurves);
                    }
                }
            }

            splittedCurves = CurveUtilities.FilterNoiseCurves(splittedCurves).Where(x => x.IsClosed).ToList();

            if (!splittedCurves.Any())
            {
                return null;
            }

            var meshNoiseShellsRemoved = AutoFix.RemoveNoiseShells(fullImplantSupport);
            var splittedSurfaces = MeshOperations.SplitMeshWithCurves(meshNoiseShellsRemoved, splittedCurves, true);

            var res = new List<Mesh>();
            foreach (var surface in splittedSurfaces)
            {
                var isNakedEdge = surface.GetNakedEdgePointStatus();
                for (var i = 0; i < surface.Vertices.Count; i++)
                {
                    if (!isNakedEdge[i])
                    {
                        var verticeInner = surface.Vertices[i];
                        foreach (var RoI in RoIs)
                        {
                            if (RoI.ClosestMeshPoint(verticeInner, 0.001) != null)
                            {
                                res.Add(surface);
                                break;
                            }
                        }
                        break;
                    }
                }
            }

            var finalRoI = MeshUtilities.AppendMeshes(res);

            //MeshRepair - Partially...
            finalRoI.ExtractNonManifoldEdges(true);
            finalRoI.Faces.ExtractDuplicateFaces();

            return finalRoI;
        }

        public static void GenerateAllImplantRoIVolume(CMFObjectManager objectManager, List<CasePreferenceDataModel> casePrefDataModels, ref RhinoObject implantSupportRhObj)
        {
            foreach (var cp in casePrefDataModels)
            {
                GetImplantRoIVolume(objectManager, cp, ref implantSupportRhObj);
            }
        }

        //TODO Can remove additionalDotsToCheckWith as it might not be useful to
        //create new RoI. Either improve RoI creation that can be created by dots alone, or remove the additional dots.
        public static Mesh GetImplantRoIVolume(CMFObjectManager objectManager, CasePreferenceDataModel casePrefDataModel, ref RhinoObject implantSupportRhObj,
            List<IDot> additionalDotsToCheckWith = null, List<IConnection> additionalConnectionsToCheckWith = null)
        {
            try
            {
                var keyRoiSurfaceString = GenerateImplantRoISurfaceKey(casePrefDataModel);
                if (implantSupportRhObj.Attributes.UserDictionary.ContainsKey(keyRoiSurfaceString))
                {
                    var roiLowLoDSurface = (Mesh)implantSupportRhObj.Attributes.UserDictionary[keyRoiSurfaceString];
                    bool needToInvalidate = false;

                    var dotsToCheckWith = new List<IDot>();

                    if (additionalDotsToCheckWith != null)
                    {
                        dotsToCheckWith.AddRange(additionalDotsToCheckWith);
                    }

                    if (additionalConnectionsToCheckWith != null && additionalConnectionsToCheckWith.Any())
                    {
                        additionalConnectionsToCheckWith.ForEach(c =>
                        {
                            dotsToCheckWith.Add(c.A);
                            dotsToCheckWith.Add(c.B);
                        });
                    }

                    var pastilleDiameter = casePrefDataModel.CasePrefData.PastilleDiameter +
                            Constants.ImplantCreation.RoIAreaRadiusOffsetModifier;

                    foreach (var dot in dotsToCheckWith)
                    {
                        var testPt = RhinoPoint3dConverter.ToPoint3d(dot.Location);
                        if (IsNeedCreateNewImplantRoIMetadata(testPt, pastilleDiameter, roiLowLoDSurface))
                        {
                            needToInvalidate = true;
                            break;
                        }
                    }

                    var defaultConnectionWidth = casePrefDataModel.CasePrefData.LinkWidthMm > casePrefDataModel.CasePrefData.PlateWidthMm
                                ? casePrefDataModel.CasePrefData.LinkWidthMm : casePrefDataModel.CasePrefData.PlateWidthMm;

                    var defaultConnectionTubeDiameter = defaultConnectionWidth + Constants.ImplantCreation.RoIAreaRadiusOffsetModifier;

                    if (!needToInvalidate)
                    {
                        //check connections
                        var connectionsToCheckWith = new List<IConnection>();

                        if (additionalConnectionsToCheckWith != null)
                        {
                            connectionsToCheckWith.AddRange(additionalConnectionsToCheckWith);
                        }

                        foreach (var connection in connectionsToCheckWith)
                        {
                            var testCurve = new LineCurve(RhinoPoint3dConverter.ToPoint3d(connection.A.Location), RhinoPoint3dConverter.ToPoint3d(connection.B.Location));
                            var connectionWidth = connection.Width > defaultConnectionWidth ? connection.Width : defaultConnectionWidth;
                            var connectionTubeDiameter = connectionWidth + Constants.ImplantCreation.RoIAreaRadiusOffsetModifier;
                            if (IsNeedCreateNewImplantRoIMetadata(testCurve, connectionTubeDiameter / 2, roiLowLoDSurface))
                            {
                                needToInvalidate = true;
                                break;
                            }
                        }
                    }

                    if (!needToInvalidate)
                    {
                        var planningFactory = new PlanningImplantBrepFactory();
                        var brep = planningFactory.CreateImplantRoiDefinition(casePrefDataModel.ImplantDataModel, pastilleDiameter, defaultConnectionTubeDiameter);
                        needToInvalidate = IsIntersectWithImplantRoI(brep, roiLowLoDSurface);
                    }

                    if (needToInvalidate)
                    {
                        InvalidateImplantRoIMetadataHelper(objectManager, ref implantSupportRhObj, casePrefDataModel,
                            additionalConnectionsToCheckWith);
                    }

                    var keyRoiVolumeString = GenerateImplantRoIVolumeKey(casePrefDataModel);
                    return (Mesh)implantSupportRhObj.Attributes.UserDictionary[keyRoiVolumeString];
                }

                return InvalidateImplantRoIMetadataHelper(objectManager, ref implantSupportRhObj, casePrefDataModel);
            }
            catch (Exception e)
            {
                Msai.TrackException(e, "CMF");
                return ((Mesh)implantSupportRhObj.Geometry).DuplicateMesh();
            }
        }

        public static Mesh GetImplantRoIVolumeWithoutCheck(CMFObjectManager objectManager, CasePreferenceDataModel casePrefDataModel, ref RhinoObject implantSupportRhObj)
        {
            try
            {
                var keyRoiVolumeString = GenerateImplantRoIVolumeKey(casePrefDataModel);
                if (implantSupportRhObj.Attributes.UserDictionary.ContainsKey(keyRoiVolumeString))
                {
                    return (Mesh)implantSupportRhObj.Attributes.UserDictionary[keyRoiVolumeString];
                }

                return InvalidateImplantRoIMetadataHelper(objectManager, ref implantSupportRhObj, casePrefDataModel);
            }
            catch (Exception e)
            {
                Msai.TrackException(e, "CMF");
                return ((Mesh)implantSupportRhObj.Geometry).DuplicateMesh();
            }
        }

        //If Failed, return entire Implant Support
        public static Mesh GetImplantRoISurfaceWithoutCheck(CMFObjectManager objectManager, CasePreferenceDataModel casePrefDataModel,
            ref RhinoObject implantSupportRhObj)
        {
            GetImplantRoIVolumeWithoutCheck(objectManager, casePrefDataModel, ref implantSupportRhObj);

            var keyRoiSurfaceString = GenerateImplantRoISurfaceKey(casePrefDataModel);
            if (implantSupportRhObj.Attributes.UserDictionary.ContainsKey(keyRoiSurfaceString))
            {
                return (Mesh)implantSupportRhObj.Attributes.UserDictionary[keyRoiSurfaceString];
            }

            return ((Mesh)implantSupportRhObj.Geometry).DuplicateMesh();
        }

        public static Mesh GetImplantRoIForImplantCreation(CMFObjectManager objectManager,
            CasePreferenceDataModel casePrefDataModel,
            ref RhinoObject implantSupportRhObj, out Mesh biggerImplantRoI)
        {
            var biggerRoI = GetImplantRoIVolume(objectManager, casePrefDataModel, ref implantSupportRhObj);

            var tubeRadius = casePrefDataModel.CasePrefData.LinkWidthMm > casePrefDataModel.CasePrefData.PlateWidthMm ?
                casePrefDataModel.CasePrefData.LinkWidthMm / 2 + 1 : casePrefDataModel.CasePrefData.PlateWidthMm / 2 + 1.2;
            var pastilleRadius = casePrefDataModel.CasePrefData.PastilleDiameter / 2;
            var smallerRoI = GenerateImplantRoI(casePrefDataModel, biggerRoI, tubeRadius, pastilleRadius + 1.2, pastilleRadius, out _);

            biggerImplantRoI = biggerRoI;
            
            return smallerRoI;
        }

        public static Mesh GenerateImplantRoI(CasePreferenceDataModel casePrefDataModel, Mesh biggerRoI, double defaultTubeRadius, double sphereRadius, double landmarkSphereRadius, out Mesh unifiedTubeMesh)
        {
            return GenerateImplantRoI(casePrefDataModel.ImplantDataModel, biggerRoI, defaultTubeRadius, sphereRadius, landmarkSphereRadius, out unifiedTubeMesh);
        }

        public static Mesh GenerateImplantRoI(ImplantDataModel implantDataModel, Mesh biggerRoI, double defaultTubeRadius, double sphereRadius, double landmarkSphereRadius, out Mesh unifiedTubeMesh)
        {
            unifiedTubeMesh = GenerateImplantRoITube(implantDataModel, biggerRoI, defaultTubeRadius, sphereRadius, landmarkSphereRadius);
            var smallerRoI = Booleans.PerformBooleanIntersection(biggerRoI, unifiedTubeMesh);

            return smallerRoI;
        }

        public static Mesh GenerateImplantRoITube(ImplantDataModel implantDataModel, Mesh biggerRoI, double defaultTubeRadius, double sphereRadius, double landmarkSphereRadius, double radiusScale = 1)
        {
            var connectionCurves = CreateImplantConnectionCurves(implantDataModel.ConnectionList).Where(c => c.PointAtStart != c.PointAtEnd).ToList();
            var tubeMesh = new Mesh();

            connectionCurves.ForEach(x =>
            {
                var segment = DataModelUtilities.GetConnections(x, implantDataModel.ConnectionList);

                var givenWidth = segment[0].Width;
                var givenTubeRadius = givenWidth / 2 + 0.7;
                var tubeRadius = givenTubeRadius > defaultTubeRadius ? givenTubeRadius : defaultTubeRadius;

                var pulled = x.PullToMesh(biggerRoI, 0.01);
                var tubeUnit = GuideSurfaceUtilities.CreateCurveTube(pulled, tubeRadius * radiusScale);
                tubeMesh.Append(tubeUnit);
            });

            implantDataModel.DotList.ForEach(x =>
            {
                if (!(x is DotPastille dotPastille))
                {
                    return;
                }

                var sphereRadiusScaled = sphereRadius * radiusScale;
                if (sphereRadiusScaled > defaultTubeRadius)
                {
                    var pt = RhinoPoint3dConverter.ToPoint3d(dotPastille.Location);
                    var sphere = new Sphere(pt, sphereRadius);
                    var sphereMesh = Mesh.CreateFromSphere(sphere, 20, 20);
                    tubeMesh.Append(sphereMesh);
                }

                var ldmark = dotPastille.Landmark;

                if (ldmark != null && ldmark.Id != Guid.Empty)
                {
                    var lmarkPt = RhinoPoint3dConverter.ToPoint3d(ldmark.Point);
                    var lmarkSphere = new Sphere(lmarkPt, landmarkSphereRadius);
                    var lmarkSphereMesh = Mesh.CreateFromSphere(lmarkSphere, 150, 150);

                    tubeMesh.Append(lmarkSphereMesh);
                }
            });

            var console = new IDSRhinoConsole();
            var tubeIdsMesh = RhinoMeshConverter.ToIDSMesh(tubeMesh);
            var unified = AutoFixV2.PerformUnify(console, tubeIdsMesh);
            return RhinoMeshConverter.ToRhinoMesh(unified);
        }

        private static void CreateDotPastilleTubeMeshes(IDot dot, double defaultTubeRadius, double sphereRadius,
            double landmarkSphereRadius, ref List<Mesh> tubeMeshes)
        {
            if (!(dot is DotPastille dotPastille))
            {
                return;
            }

            if (sphereRadius > defaultTubeRadius)
            {
                var pt = RhinoPoint3dConverter.ToPoint3d(dotPastille.Location);
                var sphere = new Sphere(pt, sphereRadius);
                var sphereMesh = Mesh.CreateFromSphere(sphere, 20, 20);
                tubeMeshes.Append(sphereMesh);
            }

            var ldmark = dotPastille.Landmark;

            if (ldmark != null && ldmark.Id != Guid.Empty)
            {
                var lmarkPt = RhinoPoint3dConverter.ToPoint3d(ldmark.Point);
                var lmarkSphere = new Sphere(lmarkPt, landmarkSphereRadius);
                var lmarkSphereMesh = Mesh.CreateFromSphere(lmarkSphere, 150, 150);

                tubeMeshes.Append(lmarkSphereMesh);
            }
        }

        public static Dictionary<Curve, Mesh> GenerateImplantPatchRoI(
            CMFObjectManager objectManager, CasePreferenceDataModel casePrefDataModel, 
            Mesh biggerRoI,
            double defaultTubeRadius, double sphereRadius, double landmarkSphereRadius, out Dictionary<Curve, string> finalTrackingReport)
        {
            var connectionRoI = new Dictionary<Curve, Mesh>();
            var trackingReport = new Dictionary<Curve, string>();

            var connectionCurves = CreateImplantConnectionCurves(casePrefDataModel.ImplantDataModel.ConnectionList);

            // only generate if patch support not present
            var implantCaseComponent = new ImplantCaseComponent();
            var patchSupportEIbb =
                implantCaseComponent.GetImplantBuildingBlock(
                    IBB.PatchSupport, casePrefDataModel);
            var connectionCurvesToGenerate =
                connectionCurves.Where(curve =>
                {
                    if (!objectManager.GetAllBuildingBlocks(patchSupportEIbb).Any())
                    {
                        return true;
                    }

                    var patchSupportObjects = 
                        objectManager.GetAllBuildingBlocks(patchSupportEIbb);

                    foreach (var patchSupportObject in patchSupportObjects)
                    {
                        var patchSupportKeyExists = patchSupportObject.Attributes.UserDictionary
                            .ContainsKey(PatchSupportKeys.PatchSupportCurveKey);

                        if (!patchSupportKeyExists)
                        {
                            continue;
                        }

                        if (GeometryBase.GeometryEquals(curve, ((Curve)patchSupportObject.Attributes.UserDictionary[PatchSupportKeys.PatchSupportCurveKey])))
                        {
                            return false;
                        }
                    }

                    return true;
                });

            var console = new IDSRhinoConsole();
            var biggerRoIIdsMesh = RhinoMeshConverter.ToIDSMesh(biggerRoI);
            foreach (var curve in connectionCurvesToGenerate)
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                var connections = DataModelUtilities.GetConnections(curve, casePrefDataModel.ImplantDataModel.ConnectionList);

                var givenWidth = connections[0].Width;
                var givenTubeRadius = givenWidth / 2 + 0.7;
                var tubeRadius = givenTubeRadius > defaultTubeRadius ? givenTubeRadius : defaultTubeRadius;

                var pulled = curve.PullToMesh(biggerRoI, 0.01);
                var tubeMeshes = new List<Mesh>() { GuideSurfaceUtilities.CreateCurveTube(pulled, tubeRadius) };

                foreach (var connection in connections)
                {
                    var dotA = connection.A;
                    var dotB = connection.B;
                    CreateDotPastilleTubeMeshes(dotA, defaultTubeRadius, sphereRadius, landmarkSphereRadius,
                        ref tubeMeshes);
                    CreateDotPastilleTubeMeshes(dotB, defaultTubeRadius, sphereRadius, landmarkSphereRadius,
                        ref tubeMeshes);
                }
                
                var tubeIdsMeshes = tubeMeshes.Select(RhinoMeshConverter.ToIDSMesh).ToArray();
                var unifiedTubeIdsMesh = AutoFixV2.PerformUnify(console, MeshUtilitiesV2.AppendMeshes(tubeIdsMeshes));
                
                var smallerRoIIdsMesh = BooleansV2.PerformBooleanIntersection(console,
                    biggerRoIIdsMesh, unifiedTubeIdsMesh);
                stopwatch.Stop();
                trackingReport.Add(curve, $"{StringUtilitiesV2.ElapsedTimeSpanToString(stopwatch.Elapsed)}");

                var smallerRoI = RhinoMeshConverter.ToRhinoMesh(smallerRoIIdsMesh);
                connectionRoI.Add(curve, smallerRoI);
            }
            
            finalTrackingReport = trackingReport;
            return connectionRoI;
        }

        public static Mesh RemeshAndSmoothImplant(Mesh implantMesh)
        {
            var remeshed1 = Remesh.PerformRemesh(implantMesh, 0.0, 0.2, 0.2, 0.01, 0.3, false, 3);
            var smoothen1 = ExternalToolInterop.PerformSmoothing(remeshed1, true, true, false, 30.0, 0.7, 3);
            remeshed1.Dispose();

            var remeshed2 = Remesh.PerformRemesh(smoothen1, 0.0, 0.1, 0.2, 0.01, 0.3, false, 3);
            smoothen1.Dispose();
            var smoothen2 = ExternalToolInterop.PerformSmoothing(remeshed2, true, true, false, 30.0, 0.7, 10);

            remeshed2.Dispose();
            return smoothen2;
        }

        public static IMesh RemeshAndSmoothImplant(IMesh implantMesh)
        {
            var console = new IDSRhinoConsole();
            var remeshed1 = RemeshV2.PerformRemesh(console, implantMesh, 0.0, 0.2, 0.2, 0.01, 0.3, false, 3);
            var smoothen1 = MeshUtilitiesV2.PerformSmoothing(console, remeshed1, true, true, false, 30.0, 0.7, 3);

            var remeshed2 = RemeshV2.PerformRemesh(console, smoothen1, 0.0, 0.1, 0.2, 0.01, 0.3, false, 3);
            var smoothen2 = MeshUtilitiesV2.PerformSmoothing(console, remeshed2, true, true, false, 30.0, 0.7, 10);
            return smoothen2;
        }

        public static Mesh SubstractImplantWithSupport(Mesh implantMesh, Mesh supportMesh)
        {
            var subtractedSupportMesh = Booleans.PerformBooleanSubtraction(implantMesh, supportMesh);
            if (!subtractedSupportMesh.IsValid)
            {
                throw new IDSException("Support mesh and implant subtractions failed.");
            }

            return subtractedSupportMesh;
        }

        public static IMesh SubtractImplantWithSupport(IMesh implantMesh, IMesh supportMesh)
        {
            var console = new IDSRhinoConsole();
            var subtractedSupportMesh = BooleansV2.PerformBooleanSubtraction(console, implantMesh, supportMesh);
            if (!subtractedSupportMesh.Vertices.Any())
            {
                throw new IDSException("Support mesh and implant subtractions failed.");
            }

            return subtractedSupportMesh;
        }

        public static List<CasePreferenceDataModel> FindImplantsThatPastillePreviewIsNotCreated(CMFImplantDirector director)
        {
            var res = new List<CasePreferenceDataModel>();
            var dataModels = director.CasePrefManager.CasePreferences;
            var helper = new PastillePreviewHelper(director);

            foreach (var casePreferenceDataModel in dataModels)
            {
                if (!helper.HasPastillePreviewBuildingBlock(casePreferenceDataModel))
                {
                    res.Add(casePreferenceDataModel);
                }
            }

            return res;
        }

        public static List<CasePreferenceDataModel> FindImplantWithMissingPastillePreview(CMFImplantDirector director)
        {
            var res = new List<CasePreferenceDataModel>();

            var helper = new PastillePreviewHelper(director);
            var dataModels = director.CasePrefManager.CasePreferences;

            foreach (var casePreferenceDataModel in dataModels)
            {
                var nDots = casePreferenceDataModel.ImplantDataModel.DotList.OfType<DotPastille>().Count();
                var nPastilleMeshes = helper.GetIntermediatePastillePreviews(casePreferenceDataModel).Count;

                if (nDots != nPastilleMeshes)
                {
                    res.Add(casePreferenceDataModel);
                }
            }

            return res;
        }

        public static void DeleteImplantSupportAttributes(CMFImplantDirector director, CasePreferenceDataModel cp)
        {
            var objManager = new CMFObjectManager(director);
            var implantSupportManager = new ImplantSupportManager(objManager);
            var impSupport = implantSupportManager.GetImplantSupportRhObj(cp);
            if (impSupport == null)
            {
                return;
            }

            var keyRoiSurf = GenerateImplantRoISurfaceKey(cp);
            var keyRoiVol = GenerateImplantRoIVolumeKey(cp);

            if (impSupport.Attributes.UserDictionary.ContainsKey(keyRoiSurf))
            {
                impSupport.Attributes.UserDictionary.Remove(keyRoiSurf);
            }

            if (impSupport.Attributes.UserDictionary.ContainsKey(keyRoiVol))
            {
                impSupport.Attributes.UserDictionary.Remove(keyRoiVol);
            }
        }

        static internal void GetDotInformation(Curve connectionCurve, List<IDot> dotList, IEnumerable<Screw> screws, out DotSearchResult dotA, out DotSearchResult dotB)
        {
            dotA = DotSearchResult.CreateDefault();
            dotB = DotSearchResult.CreateDefault();

            if (!dotList.Any())
            {
                return;
            }

            dotA = SearchDot(connectionCurve.PointAtStart, dotList, screws);
            dotB = SearchDot(connectionCurve.PointAtEnd, dotList, screws);

            if (!dotA.FoundLocation || !dotB.FoundLocation)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Unable to find pastille/control point with matching end points of curve.");
                return;
            }

            if ((dotA.IsPastille && !dotA.HasScrewInfo) || (dotB.IsPastille && !dotB.HasScrewInfo))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Pastille(s) found do not have screw information.");
            }
        }

        private static DotSearchResult SearchDot(Point3d location, List<IDot> dotList, IEnumerable<Screw> screws)
        {
            var result = DotSearchResult.CreateDefault();

            var dot = dotList.FirstOrDefault(con => location == RhinoPoint3dConverter.ToPoint3d(con.Location));
            if (dot == null)
            {
                return result;
            }

            result.FoundLocation = true;

            if (!(dot is DotPastille pastille))
            {
                return result;
            }

            result.IsPastille = true;

            if (pastille.Screw == null)
            {
                return result;
            }

            result.HasScrewInfo = true;
            result.ScrewIndex = screws.First(s => s.Id == pastille.Screw.Id).Index;
            return result;
        }

        static internal string FormatDotDisplayString(DotSearchResult dot, int implantNum)
        {
            if (!dot.FoundLocation)
            {
                return $"locationnotfound";
            }
            else if (!dot.IsPastille)
            {
                return $"controlpoint.I{implantNum}";
            }
            else if (!dot.HasScrewInfo)
            {
                return $"noscrewinfo.I{implantNum}";
            }
            return $"{dot.ScrewIndex}.I{implantNum}";
        }

        private static bool AddArchivableDictionaryToCurve(ref Curve curve, List<IDot> clusterDots, List<IConnection> connectionList)
        {
            var archivable = new ArchivableDictionary();

            // Find all connections where both endpoints are in the cluster
            var clusterDotIds = new HashSet<Guid>(clusterDots.Select(d => d.Id));
            var matchedConnections = connectionList
                .Where(c => clusterDotIds.Contains(c.A.Id) && clusterDotIds.Contains(c.B.Id))
                .ToList();

            var connectionCounter = 0;
            foreach (var connection in matchedConnections)
            {
                ArchivableDictionary connArc = null;
                if (connection is ConnectionPlate plate)
                {
                    connArc = ConnectionPlateSerializer.Serialize(plate);
                }
                else if (connection is ConnectionLink link)
                {
                    connArc = ConnectionLinkSerializer.Serialize(link);
                }

                if (connArc == null)
                {
                    return false;
                }

                archivable.Set(AttributeKeys.KeyConnection + $"_{connectionCounter}", connArc);
                connectionCounter++;
            }

            curve.UserDictionary.Set(AttributeKeys.KeyIConnections, archivable);

            return true;
        }
    }
}
