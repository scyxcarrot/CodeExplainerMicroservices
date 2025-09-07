using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Operations
{
    public class GuideFixationScrewCalibrator
    {
        public Point3d GetNewScrewHeadPoint(Mesh constraintMesh, Screw referenceScrew)
        {
            var projectedScrewHeadPoints = Intersection.ProjectPointsToMeshes(new List<Mesh>() { constraintMesh }, new List<Point3d>() { referenceScrew.HeadPoint }, referenceScrew.Direction, 0.001).ToList();

            if (!projectedScrewHeadPoints.Any())
            {
                return Point3d.Unset;
            }

            var newHeadPoint = projectedScrewHeadPoints.FirstOrDefault();
            projectedScrewHeadPoints.ForEach(x =>
            {
                if (!x.EpsilonEquals(newHeadPoint, 0.001) && x.DistanceTo(referenceScrew.HeadPoint) < newHeadPoint.DistanceTo(referenceScrew.HeadPoint))
                {
                    newHeadPoint = x;
                }
            });

            return newHeadPoint;
        }

        //returns null if calibration fails
        //referenceScrew is used to retrieve label tag information (if available)
        //referenceScrew can be null (e.g: when placing a new screw)
        public Screw LevelScrew(Screw screwToLevel, Mesh constraintMesh, Screw referenceScrew)
        {
            return LevelScrew(screwToLevel, constraintMesh, referenceScrew, true);
        }

        public Screw FastLevelScrew(Screw screwToLevel, Mesh constraintMesh, Screw referenceScrew)
        {
            return LevelScrew(screwToLevel, constraintMesh, referenceScrew, false);
        }

        //constraintSurface provided should be the remeshed surface cutout that would be used as input to generate lightweight
        public Screw RelevelScrewOnSurface(Screw screwToLevel, Mesh constraintSurface, Mesh guideSupport, Screw referenceScrew)
        {
            var offset = 0.5;
            var accuracyStep = 0.01;
            var curveSegmentLength = offset;

            var labelTagInfo = CreateLabelTagInfo(referenceScrew);
            var screwRefBrep = GetScrewRefBrep(screwToLevel, labelTagInfo);
            var screwRef = Curve.JoinCurves(screwRefBrep.Curves3D).First();
            var refPts = GetCurvePoints(screwRef, curveSegmentLength);

            bool wasLeveledDown;
            var eyeLeveledScrew = LevelEyeTwoWayOnTopOfSurface(screwToLevel, refPts, offset, 100, constraintSurface, out wasLeveledDown);
            if (eyeLeveledScrew == null)
            {
                return null;
            }
            
            var calibrator = new ScrewCalibrator(constraintSurface);
            var offsetEntity = CreateScrewRefOffset(GetScrewRefBrep(eyeLeveledScrew, labelTagInfo), offset);
            eyeLeveledScrew = calibrator.LevelHeadUpBasedOnScrewEntity(eyeLeveledScrew, offsetEntity, accuracyStep, true);
            if (eyeLeveledScrew == null)
            {
                return null;
            }

            //if screw was levelled down, make sure that its eye/label tag ref is still above guide support
            var calibratorOnSupport = new ScrewCalibrator(guideSupport);
            var minOffsetEntity = CreateScrewRefOffset(GetScrewRefBrep(eyeLeveledScrew, labelTagInfo), 0.05);
            eyeLeveledScrew = calibratorOnSupport.LevelHeadUpBasedOnScrewEntity(eyeLeveledScrew, minOffsetEntity, accuracyStep);
            if (eyeLeveledScrew == null)
            {
                return null;
            }

            //if screw was levelled down, make sure that its container is still above guide support
            calibratorOnSupport.LevelHeadBasedOnScrewContainer(eyeLeveledScrew, accuracyStep);
            return calibratorOnSupport.CalibratedScrew;
        }

        public Screw LevelScrewWithLabelTag(Screw screwToLevel, Mesh constraintMesh, double labelTagAngle)
        {
            var labelTagInfo = new LabelTagInfo
            {
                IsAvailable = true,
                Angle = labelTagAngle
            };

            return LevelScrew(screwToLevel, constraintMesh, labelTagInfo, true);
        }

        private Screw LevelScrew(Screw screwToLevel, Mesh constraintMesh, Screw referenceScrew, bool accurateLeveling)
        {
            var labelTagInfo = CreateLabelTagInfo(referenceScrew);
            return LevelScrew(screwToLevel, constraintMesh, labelTagInfo, accurateLeveling);
        }

        private Screw LevelScrew(Screw screwToLevel, Mesh constraintMesh, LabelTagInfo labelTagInfo, bool accurateLeveling)
        {
            if (!constraintMesh.IsClosed) //Solid Orientation is 0 when there is non manifold face, but is a closed mesh actually.
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Constraint mesh is not solid!");
                return null;
            }

            var offset = 0.45;
            var accuracyStep = accurateLeveling ? 0.01 : 0.1;
            var curveSegmentLength = accurateLeveling ? offset : offset * 2;

            var calibrator = new ScrewCalibrator(constraintMesh);

            var screwRefBrep = GetScrewRefBrep(screwToLevel, labelTagInfo);
            var screwRef = Curve.JoinCurves(screwRefBrep.Curves3D).First();
            var refPts = GetCurvePoints(screwRef, curveSegmentLength);
            if (!calibrator.LevelEyeOnTopOfMesh(screwToLevel, refPts, offset, 100))
            {
                return null;
            }

            var eyeLeveledScrew = calibrator.CalibratedScrew;
            if (accurateLeveling)
            {
                var offsetEntity = CreateScrewRefOffset(GetScrewRefBrep(eyeLeveledScrew, labelTagInfo), offset);
                eyeLeveledScrew = calibrator.LevelHeadUpBasedOnScrewEntity(eyeLeveledScrew, offsetEntity, accuracyStep);
                if (eyeLeveledScrew == null)
                {
                    return null;
                }
            }

            if (!calibrator.LevelHeadBasedOnScrewContainer(eyeLeveledScrew, accuracyStep))
            {
                return null;
            }

            return calibrator.CalibratedScrew;
        }

        private LabelTagInfo CreateLabelTagInfo(Screw referenceScrew)
        {
            var info = new LabelTagInfo
            {
                IsAvailable = false,
                Angle = 0.0
            };

            if (referenceScrew != null)
            {
                var screwLabelTagHelper = new ScrewLabelTagHelper(referenceScrew.Director);
                info.Angle = screwLabelTagHelper.GetLabelTagAngle(referenceScrew);
                if (!double.IsNaN(info.Angle))
                {
                    info.IsAvailable = true;
                }
            }

            return info;
        }

        private Brep GetScrewRefBrep(Screw screw, LabelTagInfo labelTagInfo)
        {
            if (labelTagInfo.IsAvailable)
            {
                var screwLabelTagHelper = new ScrewLabelTagHelper(screw.Director);
                return screwLabelTagHelper.GetLabelTagRef(screw, labelTagInfo.Angle);
            }
            else
            {
                return screw.GetScrewEyeRef();
            }
        }

        private Mesh CreateScrewRefOffset(Brep screwRef, double refOffset)
        {
            var offsetEntity = MeshUtilities.ConvertBrepToMesh(Brep.CreateFromOffsetFace(screwRef.Faces[0], refOffset, 0.0, false, true), true);
            return offsetEntity;            
        }

        private List<Point3d> GetCurvePoints(Curve curve, double segmentLength)
        {
            Point3d[] points;
            curve.DivideByLength(segmentLength, true, out points);
            return points.ToList();
        }

        private Screw LevelEyeOnTopOfSurface(Screw screw, List<Point3d> eyeRefPoints, double offset, int calibrationSteps, Mesh surface)
        {
            Func<double, Vector3d> getMoveDirection = (currDist) => -screw.Direction; //move up only!
            Func<List<Point3d>, Point3d> getNextPointToCompare = (refPoints) => FindClosestPointFromSurface(refPoints, surface, offset);
            Func<Point3d, double> getClosestDistance = (newRefPt) => CalculatePointClosestDistanceToSurface(newRefPt, surface, offset);
            Func<double, bool> isDistanceFulfilled = (currDist) => currDist > offset;

            var calibrator = new ScrewCalibrator(surface);
            calibrator.LevelRefOnTopOfMesh(screw, eyeRefPoints, getMoveDirection, getNextPointToCompare, getClosestDistance, isDistanceFulfilled, calibrationSteps);
            return calibrator.CalibratedScrew;
        }

        private Screw LevelEyeTwoWayOnTopOfSurface(Screw screw, List<Point3d> eyeRefPoints, double offset, int calibrationSteps, Mesh surface, out bool isLeveledDown)
        {
            var levelDown = false;
            var maxDistanceFromSurface = offset;

            Func<double, Vector3d> getMoveDirection = (currDist) =>
            {
                if (currDist >= offset)
                {
                    levelDown = true;
                    maxDistanceFromSurface = currDist;
                }
                return levelDown ? screw.Direction : -screw.Direction; //move up or down - depending on initial distance
            };
            Func<List<Point3d>, Point3d> getNextPointToCompare = (refPoints) => FindClosestPointFromSurface(refPoints, surface, maxDistanceFromSurface);
            Func<Point3d, double> getClosestDistance = (newRefPt) => CalculatePointClosestDistanceToSurface(newRefPt, surface, maxDistanceFromSurface);
            Func<double, bool> isDistanceFulfilled = (currDist) =>
            {
                return levelDown ? (currDist <= offset) : (currDist >= offset);
            };

            var calibrator = new ScrewCalibrator(surface);
            calibrator.LevelRefOnTopOfMesh(screw, eyeRefPoints, getMoveDirection, getNextPointToCompare, getClosestDistance, isDistanceFulfilled, calibrationSteps);

            isLeveledDown = levelDown;

            return calibrator.CalibratedScrew;
        }

        private Point3d FindClosestPointFromSurface(List<Point3d> points, Mesh surface, double offset)
        {
            if (!points.Any())
            {
                return Point3d.Unset;
            }

            var distance = double.MaxValue;
            Point3d closestPt = points[0];
            foreach (var refPt in points)
            {
                var distanceFromPlane = CalculatePointClosestDistanceToSurface(refPt, surface, offset);
                if (distanceFromPlane > 0 && distanceFromPlane <= distance)
                {
                    distance = distanceFromPlane;
                    closestPt = refPt;
                }
            }

            return closestPt;
        }

        private double CalculatePointClosestDistanceToSurface(Point3d point, Mesh surface, double offset)
        {
            var pointOnMesh = surface.ClosestMeshPoint(point, offset);
            if (pointOnMesh != null)
            {
                var normal = surface.FaceNormals[pointOnMesh.FaceIndex];
                var dir = point - pointOnMesh.Point;
                var length = dir.Length;
                dir.Unitize();
                if (Vector3d.Multiply(dir, normal) < 0)
                {
                    return -length;
                }

                return length;
            }
            return double.MaxValue;
        }

        struct LabelTagInfo
        {
            public bool IsAvailable;
            public double Angle;
        }
    }
}