using IDS.CMF.Constants;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.Core.Utilities;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Operations
{
    public class ScrewCalibrator
    {
        private readonly Mesh mesh;

        public Screw CalibratedScrew { get; private set; }

        public ScrewCalibrator(Mesh mesh)
        {
            this.mesh = mesh;
        }

        public bool LevelHeadBasedOnScrewContainer(Screw screw, double step, bool skipInsideMeshCheck = false)
        {
            var container = MeshUtilities.ConvertBrepToMesh(screw.GetScrewContainer());
            CalibratedScrew = LevelHeadUpBasedOnScrewEntity(screw, container, step, skipInsideMeshCheck);
            return CalibratedScrew != null;
        }

        public Screw LevelHeadUpBasedOnScrewEntity(Screw screw, Mesh screwEntity, double step, bool skipInsideMeshCheck = false)
        {
            Point3d calibratedHeadPoint;
            if (!CalibrateScrew(screwEntity, mesh, -screw.Direction, screw.HeadPoint, step, 10, skipInsideMeshCheck, out calibratedHeadPoint)) //move up only
            {
                return null;
            }

            var translationLevel = calibratedHeadPoint - screw.HeadPoint;
            var calibratedLevelTipPoint = screw.TipPoint + translationLevel;
            var resScrew = new Screw(screw.Director, calibratedHeadPoint, calibratedLevelTipPoint,
                screw.ScrewAideDictionary, screw.Index, screw.ScrewType, screw.BarrelType);

            return resScrew;
        }
        
        public bool LevelHeadOnTopOfMesh(Screw screw, double offset, int calibrationSteps, bool skipInsideMeshCheck = false, bool isImplantScrew = true)
        {
            return LevelHeadOnTopOfMesh(screw, offset, calibrationSteps, 0.01, skipInsideMeshCheck, isImplantScrew);
        }

        public bool LevelEyeOnTopOfMesh(Screw screw, List<Point3d> eyeRefPoints, double offset, int calibrationSteps)
        {
            Func<double, Vector3d> getMoveDirection = (currDist) => -screw.Direction; //move up only!
            Func <List<Point3d>, Point3d> getNextPointToCompare = (refPoints) => PointUtilities.FindClosestPointOutsideFromMesh(refPoints, mesh);
            Func<Point3d, double> getClosestDistance = (newRefPt) => MathUtilities.CalculatePointClosestDistanceToMesh(newRefPt, mesh);
            Func<double, bool> isDistanceFulfilled = (currDist) => currDist > offset;

            return LevelRefOnTopOfMesh(screw, eyeRefPoints, getMoveDirection, getNextPointToCompare, getClosestDistance, isDistanceFulfilled, calibrationSteps);
        }

        public bool LevelHeadOnTopOfMesh(Screw screw, double offset, int calibrationSteps, double containerCalibrationStep, bool skipInsideMeshCheck = false, bool isImplantScrew = true)
        {
            var tolerance = 0.09;

            var screwHeadRef = screw.GetScrewHeadRef();
            var screwHeadRefPts = CurveUtilities.GetCurveControlPoints(screwHeadRef).ToList();

            Func<double, Vector3d> getMoveDirection = (currDist) =>
            {
                return currDist >= offset ? screw.Direction : -screw.Direction;
            };
            Func<List<Point3d>, Point3d> getNextPointToCompare = (refPoints) => PointUtilities.FindFarthestPointFromMesh(refPoints, mesh);
            Func<Point3d, double> getClosestDistance = (newRefPt) => MathUtilities.CalculatePointClosestDistanceToMesh(newRefPt, mesh);
            Func<double, bool> isDistanceFulfilled = (currDist) => MathUtilities.IsWithin(currDist, offset, offset + tolerance);

            var onPlateThickness = LevelRefOnTopOfMesh(screw, screwHeadRefPts, getMoveDirection, getNextPointToCompare, getClosestDistance, isDistanceFulfilled, calibrationSteps);         
            
            if (!onPlateThickness)
            {
                return false;
            }

            var levelScrew = CalibratedScrew;
            CalibratedScrew = null;
            return LevelHeadBasedOnScrewContainer(levelScrew, containerCalibrationStep, skipInsideMeshCheck);
        }
        
        public bool LevelRefOnTopOfMesh(Screw screw, List<Point3d> refPoints, Func<double, Vector3d> getMoveDirection, Func<List<Point3d>, Point3d> getNextPointToCompare, 
            Func<Point3d, double> getClosestDistance, Func<double, bool> isDistanceFulfilled, int maxIterations)
        {
            var first = new Point3d(refPoints[0]);
            var refPt = getNextPointToCompare(refPoints);

            var currDist = getClosestDistance(refPt);

            var levelScrew = new Screw();
            var moveDir = getMoveDirection(currDist);
            var onFulfilledDistance = isDistanceFulfilled(currDist);
            var count = 0;

            if (onFulfilledDistance)
            {
                levelScrew = new Screw(screw.Director, screw.HeadPoint, screw.TipPoint, screw.ScrewAideDictionary,
                    screw.Index, screw.ScrewType, screw.BarrelType);
            }

            while (!onFulfilledDistance && count < maxIterations)
            {
                for (var i = 0; i < refPoints.Count; i++)
                {
                    refPoints[i] += (moveDir * ScrewCalibratorConstants.CalibrationStepSize);
                }

                var newRefPt = getNextPointToCompare(refPoints);

                var tmpDist = getClosestDistance(newRefPt);

                if (isDistanceFulfilled(tmpDist))
                {
                    onFulfilledDistance = true;
                    var translation = refPoints[0] - first;
                    var newHeadPt = screw.HeadPoint + translation;
                    var newTipPt = screw.TipPoint + translation;
                    levelScrew = new Screw(screw.Director, newHeadPt, newTipPt, screw.ScrewAideDictionary, screw.Index,
                        screw.ScrewType, screw.BarrelType);
                }
                count++;
            }

            if (!onFulfilledDistance)
            {
                CalibratedScrew = null;
                return false;
            }

            CalibratedScrew = levelScrew;
            return true;
        }

        //TODO This can be improved better by
        //(1) Project Head Point to Mesh surface and translate entire screw to the Projected Point
        //(2)Proceed with calibration with only moving the screw to -screw.Direction instead of both ways.
        public bool LevelHeadOnTopOfMesh(Screw screw, double offset, bool skipInsideMeshCheck = false, bool isImplantScrew = true)
        {
            return LevelHeadOnTopOfMesh(screw, offset, (int)(80*offset), skipInsideMeshCheck, isImplantScrew);
        }
       
        public bool FastLevelHeadOnTopOfMesh(Screw screw, double offset, bool skipInsideMeshCheck = false, bool isImplantScrew = true)
        {
            return LevelHeadOnTopOfMesh(screw, offset, (int)(80 * offset), 0.1, skipInsideMeshCheck, isImplantScrew);
        }

        private bool CalibrateScrew(Mesh screwEntity, Mesh targetEntity, Vector3d screwDirection, Point3d initialHeadPt, double step, double maxMoveDistance, bool skipInsideMeshCheck, out Point3d calibratedHeadPt)
        {
            screwEntity.Vertices.UseDoublePrecisionVertices = false;
            calibratedHeadPt = initialHeadPt;
            for (int i = 0; i < (int)(maxMoveDistance/step); ++i)
            {
                var hasCollision = MeshUtilities.HasCollisionThroughIntersectionMethod(targetEntity, screwEntity, 0.001, skipInsideMeshCheck);
                if (!hasCollision)
                {
                    //return when no collision detected
                    return true;
                }

                // Move head and screw entity
                calibratedHeadPt += step * screwDirection;
                screwEntity.Translate(step * screwDirection);
            }
            return false;
        }
    }
}
