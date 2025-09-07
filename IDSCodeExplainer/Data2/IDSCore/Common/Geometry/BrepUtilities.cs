using IDS.Core.DataTypes;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace IDS.Core.Utilities
{
    /**
     * Utility functions for Brep and surface operations.
     */

    public static class BrepUtilities
    {
        public static Brep CreatePlaneSurfaceBrep(Plane plane, bool invert, int size)
        {
            var tmpPlane = plane;

            if (invert)
            {
                tmpPlane = new Plane(plane.Origin, -plane.Normal);
            }

            var span = new Interval(-size / 2, size / 2);
            return new PlaneSurface(tmpPlane, span, span).ToBrep();
        }


        //Will project the curve on the plane, If appendAsOne is true, it will only return array with one element!
        public static Brep[] CreatePlanarCurvesExtrude(Curve[] curves, double distance, bool extrudeBothSides, bool appendAsOne)
        {
            var modelAbsoluteTolerance = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            var planarBreps = Brep.CreatePlanarBreps(curves).ToList();

            var res = new List<Brep>();

            planarBreps.ForEach(x =>
            {
                var extr = Brep.CreateFromOffsetFace(x.Faces[0], distance, modelAbsoluteTolerance, extrudeBothSides, true);
                res.Add(extr);
            });

            if (!appendAsOne)
            {
                return res.ToArray();
            }

            var appended = Append(res.ToArray());
            res.Clear();
            res.Add(appended);

            return res.ToArray();
        }

        public static Point3d GetGravityCenter(Brep obj)
        {
            var prop = VolumeMassProperties.Compute(obj);
            return prop.Centroid;
        }

        public static Point3d GetGravityCenter(BrepFace obj)
        {
            var prop = AreaMassProperties.Compute(obj);
            return prop.Centroid;
        }

        public static BrepFace FindFurthestBrepFace(List<BrepFace> faces, Vector3d vector)
        {
            var facesWithCenters = new List<KeyValuePair<BrepFace, Point3d>>();

            foreach (var face in faces)
            {
                var center = GetGravityCenter(face);
                facesWithCenters.Add(new KeyValuePair<BrepFace, Point3d>(face, center));
            }

            var furthestPt = PointUtilities.FindFurthermostPointAlongVector(facesWithCenters.Select(x => x.Value).ToArray(), vector);

            var found = facesWithCenters.Find(x => x.Value == furthestPt);

            return found.Key;
        }

        public static bool IsBrepFaceParallelTo(BrepFace face, Point3d testPoint, Vector3d vector)
        {
            var brepFace = face;
            double u, v;
            if (brepFace.ClosestPoint(testPoint, out u, out v))
            {
                var direction = brepFace.NormalAt(u, v);
                if (direction.IsParallelTo(vector, 0.001) == 1)
                {
                    return true;
                }
            }
            return false;
        }

        public static List<BrepFace> FindBrepFacesPerpendicularTo(Brep brep, Vector3d perpendicularToVector, double angleTolerance)
        {
            var result = new List<BrepFace>();

            foreach (var f in brep.Faces)
            {
                if (f.NormalAt(1.0, 1.0).IsPerpendicularTo(perpendicularToVector, angleTolerance))
                {
                    result.Add(f);
                }
            }

            return result;
        }

        public static Brep Append(Brep[] breps)
        {
            var appended = new Brep();
            breps.ToList().ForEach(x => appended.Append(x));
            return appended;
        }

        public static List<Brep> SortBrepBySurfaceArea(Brep[] breps)
        {
            if (breps == null || !breps.Any())
            {
                return null;
            }

            var breList = breps.ToList();

            return breList.OrderBy(x => x.GetArea(0.01, 0.1)).ToList();
        }

        public static List<Brep> ScriptedFilletSurface(RhinoDoc doc, Brep firstSrf, Brep secondSrf, double radius)
        {
            doc.Objects.UnselectAll(true);

            var firstId = doc.Objects.Add(firstSrf);
            var secondId = doc.Objects.Add(secondSrf);

            var radiusStr = radius.ToString("0.00000", CultureInfo.GetCultureInfo("en-US"));

            var sb = new StringBuilder("_VariableFilletSrf ");
            sb.Append($"_R {radiusStr} ");
            sb.Append($"_SelID \"{firstId}\" ");
            sb.Append($"_SelID \"{secondId}\" ");
            sb.Append($"_R _D TrimAndJoin=Yes _Enter");

            var startSn = RhinoObject.NextRuntimeSerialNumber;
            RhinoApp.RunScript(sb.ToString(), true);
            var endSn = RhinoObject.NextRuntimeSerialNumber;

            if (startSn == endSn) // no object created
            {
                doc.Objects.Delete(firstId, true);
                doc.Objects.Delete(secondId, true);
                return null;
            }

            var object_ids = new List<Guid>();
            for (var sn = startSn; sn < endSn; sn++)
            {
                var obj = doc.Objects.Find(sn);
                if (null != obj)
                {
                    object_ids.Add(obj.Id);
                }
            }

            var resultBreps = new List<Brep>();

            object_ids.ForEach(x => resultBreps.Add(((Brep)doc.Objects.Find(x).Geometry).DuplicateBrep()));
            object_ids.ForEach(x => doc.Objects.Delete(x, true));

            doc.Objects.Delete(firstId, true);
            doc.Objects.Delete(secondId, true);

            return resultBreps;
        }

        public static Brep Trim(Brep brep, Plane trimmingPlane, bool inverse)
        {
            var brepCopy = brep.DuplicateBrep();
            Surface planeSurface = PlaneSurface.CreateThroughBox(trimmingPlane, brepCopy.GetBoundingBox(false));

            if (inverse)
            {
                planeSurface = planeSurface.Reverse(0);
            }

            var subtractionBrep = Brep.CreateFromOffsetFace(Brep.CreateFromSurface(planeSurface).Faces[0], 100, 0.01, false, true);
            return Brep.CreateBooleanDifference(brepCopy, subtractionBrep, 0.01).FirstOrDefault();
        }

        /**
         * Get a mesh for testing collisions for a RhinoObject,
         * this mesh has 'Coarse' MeshingParameters.
         */

        /**
         * Get a mesh for testing collisions for a RhinoObject
         */

        public static Mesh GetCollisionMesh(this Brep shape, MeshingParameters mp)
        {
            var colMeshes = Mesh.CreateFromBrep(shape, mp);
            var colMesh = MeshUtilities.UnifyMeshParts(colMeshes);
            return colMesh;
        }
        public static List<double> Brep2BrepDistance(Brep from, Brep to)
        {
            var dists = new List<double>();
            foreach (var fromPoint in from.Vertices)
            {
                var toPoint = to.ClosestPoint(fromPoint.Location);
                dists.Add((toPoint - fromPoint.Location).Length);
            }
            return dists;
        }

        public static double Edge2BrepDistance(BrepEdge fromEdge, Brep to)
        {
            Point3d fromPoint, toPoint;
            int geomentryIndex;
            fromEdge.ClosestPoints(new[] { to }, out fromPoint, out toPoint, out geomentryIndex);

            return (fromPoint - toPoint).Length;
        }

        public static BrepEdge FindClosestEdgeToBrep(Brep from, Brep to, out int index)
        {
            index = -1;
            var edges = from.Edges;
            BrepEdge edge = null;

            var closestDistance = Double.MaxValue;
            foreach (var e in edges)
            {
                var distance = Brep2BrepDistance(from, to).Min();
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    edge = e;
                    index = e.EdgeIndex;
                }
            }

            return edge;
        }

        public static List<KeyValuePair<BrepEdge, double>> GetEdgesWithDistanceToBrep(Brep from, Brep to)
        {
            var edges = from.Edges;
            var edgeDictionary = new Dictionary<BrepEdge, double>();

            foreach (var e in edges)
            {
                var distance = Edge2BrepDistance(e, to);
                edgeDictionary.Add(e, distance);
            }

            var list = edgeDictionary.ToList();
            list.Sort((pair1, pair2) => pair1.Value.CompareTo(pair2.Value));

            return list;
        }

        public static List<BrepEdge> GetContactEdges(Brep from, Brep to, double distanceTolerance)
        {
            var edges = GetEdgesWithDistanceToBrep(from, to);

            var contact = edges.Where(x => x.Value < distanceTolerance).Select(x => x.Key).ToList();
            return contact.OrderBy(x => x.GetLength()).ToList();
        }

        private static bool SplitSurfaceWithCurve(Surface surf, Curve inputContour, out Brep result)
        {
            result = null;
            var contour = inputContour.DuplicateCurve();

            // Defaults
            if (!contour.IsClosed)
            {
                return false;
            }

            // Check for curve self-intersections
            var isects = Intersection.CurveSelf(contour, 0.0001);
            var max_iter = 100;
            for (var i = 0; i < max_iter && isects != null && isects.Count > 0; i++)
            {
                // Remove the first self-intersection
                var isect = isects[0];
                // Try to remove the shortest segment
                var seg_a = new Interval(isect.ParameterA, isect.ParameterB);
                var seg_b = new Interval(isect.ParameterB, isect.ParameterA);
                var len_a = contour.GetLength(seg_a);
                var len_b = contour.GetLength(seg_b);
                var rem_seg = seg_b; // remaining segment
                if (len_a > len_b)
                {
                    rem_seg = seg_a;
                }
                // Keep segment a, cut away segment b
                var trimmed = contour.Trim(rem_seg);
                var closed = trimmed.MakeClosed(0.1);
                if (!closed)
                {
                    return false;
                }
                contour = trimmed;
                isects = Intersection.CurveSelf(contour, 0.0001);
            }

            if (isects == null || isects.Count > 0) // Could not fix self-intersections!
            {
                return false;
            }

            result = surf.ToBrep().Faces[0].Split(new[] { contour }, 0.1);

            return true;
        }


        /**
         * Cut a patch from a surface using a contour curve.
         *
         * @param           surface to cut patch from
         * @param contour   a closed curve constrained to the surface
         * @return          true on success, false otherwise
         */

        public static bool CutPatchFromSurface(Surface surf, Curve contour, out Brep patch)
        {
            // Defaults
            patch = null;

            Brep planeSplit;
            if (SplitSurfaceWithCurve(surf, contour, out planeSplit))
            {
                if (planeSplit.Faces.Count != 2)
                {
                    return false;
                }

                // Get the face without an inner loop, i.e. that has a single loop that is an outer loop
                BrepFace innerface = planeSplit.Faces.FirstOrDefault(f => f.Loops.Count == 1);
                if (null == innerface)
                {
                    return false;
                }
                patch = innerface.DuplicateFace(false);
                return true;
            }

            return false;
        }

        public static bool RemovePatchFromSurface(Surface surf, Curve contour, double curvePullToSurfaceTolerace, out Brep result)
        {
            result = null;

            foreach (var bface in surf.ToBrep().Faces)
            {
                contour.PullToBrepFace(bface, curvePullToSurfaceTolerace);
            }

            Brep planeSplit;
            if (!SplitSurfaceWithCurve(surf, contour, out planeSplit))
            {
                return false;
            }

            if (planeSplit == null || planeSplit.Faces.Count != 2)
            {
                return false;
            }

            // Get the face without an inner loop, i.e. that has a single loop that is an outer loop
            var innerface = planeSplit.Faces.FirstOrDefault(f => f.Loops.Count == 1);

            if (innerface == null)
            {
                return false;
            }

            planeSplit.Faces.RemoveAt(innerface.FaceIndex);
            result = planeSplit;
            return true;
        }

        /**
         * Given a plane and a closed curve, compute the patch cut out by the
         * curve projected onto the plane.
         *
         * @param plane     The plane in which the surface must lie
         * @param planedim  Interval of the plane surface from which the
         *                  patch will be cut
         * @param cutcurve  Closed curve for cutting out the patch. The curve
         *                  will be projected onto the plane
         * @param crvOffset Offset of the projected curve on the plane towards
         *                  its center (in document units).
         * @param[out] cutpatch The patch cut out by the cutting curve
         * @return          true on success, false otherwise
         */

        public static Guid[] ScriptedSweep2(RhinoDoc doc, Guid rail1, Guid rail2, IEnumerable<Guid> shapes)
        {
            doc.Objects.UnselectAll(true);

            var sb = new StringBuilder("_-Sweep2 ");
            sb.Append($"_SelID {rail1} ");
            sb.Append($"_SelID {rail2} ");
            foreach (var shape in shapes)
            {
                sb.Append($"_SelID {shape} ");
            }
            sb.Append("_Enter ");
            sb.Append("_Simplify=_None _Closed=_Yes _MaintainHeight=_Yes _Enter");

            var startSn = Rhino.DocObjects.RhinoObject.NextRuntimeSerialNumber;
            RhinoApp.RunScript(sb.ToString(), true);
            var endSn = Rhino.DocObjects.RhinoObject.NextRuntimeSerialNumber;

            if (startSn == endSn) // no object created
            {
                return new Guid[0];
            }

            var objectIds = new List<Guid>();
            for (var sn = startSn; sn < endSn; sn++)
            {
                var obj = doc.Objects.Find(sn);
                if (null != obj)
                {
                    objectIds.Add(obj.Id);
                }
            }

            return objectIds.ToArray();
        }

        public static Mesh CreateSweepMesh(RhinoDoc doc, RhinoObject rail1, RhinoObject rail2, IEnumerable<RhinoObject> shapes)
        {
            return CreateSweepMesh(doc, rail1, rail2, shapes, true);
        }

        public static Mesh CreateSweepMesh(RhinoDoc doc, RhinoObject rail1, RhinoObject rail2, IEnumerable<RhinoObject> shapes, bool joinRail1)
        {
            // Do sweep2
            var objectIds = ScriptedSweep2(doc, rail1.Id, rail2.Id, shapes?.Select(shape => shape.Id));
            if (objectIds.Count() != 1)
            {
                return null;
            }
            var sweepId = objectIds[0];

            // Get the Brep sweep
            var sweepObj = doc.Objects.Find(sweepId);
            doc.Objects.Unlock(sweepObj, true);
            var sweepBrep = (Brep)sweepObj.Geometry;

            if (joinRail1)
            {
                // rail1 curve planar brep
                var curveObject = rail1 as CurveObject;
                if (curveObject == null)
                {
                    return null;
                }
                var filledSweepAll = Brep.CreatePlanarBreps(curveObject.CurveGeometry);
                var filledSweep = filledSweepAll[0];

                // Join the 2 breps
                var joinedSweep = Brep.JoinBreps(new[] { filledSweep, sweepBrep }, 0.5);
                if (joinedSweep.Length != 1)
                {
                    return null;
                }

                sweepBrep = joinedSweep[0];
            }

            // Mesh the sweep
            var mp = MeshParameters.IDS();
            var sweepMesh = GetCollisionMesh(sweepBrep, mp);

            doc.Objects.Delete(sweepObj, true);
            return sweepMesh;
        }

        public static Brep CreateEntityFromCurves(Line revolveAxis, IEnumerable<Curve> curves)
        {
            // Revolve to create shape
            var joinedCurves = curves.Count() > 1 ? Curve.JoinCurves(curves)[0] : curves.FirstOrDefault();
            var brep = RevSurface.Create(joinedCurves, revolveAxis).ToBrep();

            return brep;
        }

        public static double Brep2MeshDistance(Brep brep, Mesh mesh, double returnImmediatelyIfDistanceLessOrEqualThan = Double.NaN)
        {
            var minDist = double.MaxValue;

            foreach (var meshVertex in mesh.Vertices)
            {
                if (!double.IsNaN(returnImmediatelyIfDistanceLessOrEqualThan) &&
                    minDist <= returnImmediatelyIfDistanceLessOrEqualThan)
                {
                    return minDist;
                }

                if (brep.IsPointInside(meshVertex, 0.001, true))
                {
                    return 0.0;
                }

                var pt = brep.ClosestPoint(meshVertex);

                var dist = pt.DistanceTo(meshVertex);
                if (dist < minDist)
                {
                    minDist = dist;
                }
            }

            return minDist;
        }

        public static Brep CreatePatchOnMeshFromClosedCurve(List<Point3d> curvePoints, Mesh lowLoDMesh)
        {
            if (!curvePoints.Any() || curvePoints.Count < 3 || lowLoDMesh == null)
            {
                return null;
            }

            var curve = CurveUtilities.BuildCurve(new List<Point3d>(curvePoints), 1, true);
            return CreatePatchOnMeshFromClosedCurve(curve, lowLoDMesh);
        }

        public static Brep CreatePatchOnMeshFromClosedCurve(Curve curve, Mesh lowLoDMesh)
        {
            if (!curve.IsClosed || lowLoDMesh == null)
            {
                return null;
            }

            var pulled = curve.PullToMesh(lowLoDMesh, 1.0).ToNurbsCurve();
            return Brep.CreatePatch(new[] { pulled }, null, 10, 10, true, true, 1.0, 100, 0, new[] { true, true, true, true }, 0.01);
        }

        public static Brep CreatePatchFromPoints(List<Point3d> curvePoints)
        {
            var closedCurve = CurveUtilities.BuildCurve(curvePoints, 1, true);
            return Brep.CreatePatch(
                new[] { closedCurve },
                null,
                10,
                10,
                true,
                false,
                0.1,
                10,
                0.1,
                new[] { true, true, true, true },
                0.01);
        }


        public static bool CheckBrepIntersectionBrep(Brep brep1, Brep brep2)
        {
            Curve[] intersectionCurves;
            Point3d[] intersectionPoints;
            Intersection.BrepBrep(brep1, brep2, IntersectionParameters.Tolerance, out intersectionCurves, out intersectionPoints);

            return intersectionPoints.Any() || intersectionCurves.Any();
        }

        // This is a legacy function mainly used in GumballTransform class
        // Not too sure why we are getting the bb this way, leaving it here to avoid any unexpected complications
        public static BoundingBox GetBoundingBoxFromMesh(Brep brep)
        {
            var brepMeshes = Mesh.CreateFromBrep(brep);
            var brepMesh = new Mesh();
            foreach (var mesh in brepMeshes)
            {
                brepMesh.Append(mesh);
            }
            var bbox = brepMesh.GetBoundingBox(true);
            return bbox;
        }
    }
}