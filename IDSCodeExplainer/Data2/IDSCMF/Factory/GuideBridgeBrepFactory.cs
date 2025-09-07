using IDS.CMF.Constants;
using IDS.CMF.Preferences;
using IDS.Core.Utilities;
using IDS.Core.V2.Utilities;
using Rhino;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using Plane = Rhino.Geometry.Plane;
#if (INTERNAL)
using IDS.Core.NonProduction;
#endif

namespace IDS.CMF.Factory
{
    public class GuideBridgeBrepFactory
    {
        private const double tolerance = 0.001;
        private string TypeBridge;
        private bool BridgeGenio;

        public GuideBridgeBrepFactory(string guideBridgeType = null, bool bridgeGenio = false)
        {
            TypeBridge = guideBridgeType;
            BridgeGenio = bridgeGenio;
        }

        public Brep CreateGuideBridgeWithRatio(Point3d startPoint, Point3d endPoint, Vector3d upDirection, 
            double compensate = 0.0, int sides = 6, double width = 4.0, double innerWidthRatio = 0.8, double thickness = 1.5, 
            double diameter = 10.0)
        {
            // width & thickness for Genio bridge
            if (BridgeGenio)
            {
                width = 2.0;
                thickness = 1.75;
                innerWidthRatio = 1.0;
            }

            width -= compensate;
            thickness -= compensate;
            diameter -= compensate;
            var innerWidth = width * innerWidthRatio;
            return CreateGuideBridge(startPoint, endPoint, upDirection, sides, width, innerWidth, thickness, diameter);
        }

        public Brep CreateCompensatedGuideBridgeForLightweight(Point3d startPoint, Point3d endPoint, Vector3d upDirection,
            double lightweightSegmentRadius, double diameter = 0.0)
        {
            var midPt = new Point3d((startPoint + endPoint) / 2);
            var mainAxis = startPoint - endPoint;
            var compensatedRadius = (mainAxis.Length / 2) - lightweightSegmentRadius;
            mainAxis.Unitize();
            var compensatedStartPoint = Point3d.Add(midPt, Vector3d.Multiply(mainAxis, compensatedRadius));
            var compensatedEndPoint = Point3d.Add(midPt, Vector3d.Multiply(-mainAxis, compensatedRadius));

            var compensate = lightweightSegmentRadius * 2;
            return CreateGuideBridgeWithRatio(compensatedStartPoint, compensatedEndPoint, upDirection, compensate, diameter: diameter);
        }

        public Brep CreateGuideBridge(Point3d startPoint, Point3d endPoint, Vector3d upDirection, int sides, double width, double innerWidth, double thickness, double diameter)
        {
            if (width <= innerWidth && !BridgeGenio)
            {
                throw new Exception($"Width must be larger than Inner Width. Given: Width={width}, InnerWidth={innerWidth}");
            }

            var radius = (startPoint - endPoint).Length / 2;
            var bridge = new Brep();

            if (TypeBridge != null && TypeBridge == GuideBridgeType.OctagonalBridge)
            { 
                bridge = CreateOctagonalGuideBridgeAtOrigin(radius, width, innerWidth, thickness, diameter);
            }
            else
            {
                bridge = CreateGuideBridgeAtOrigin(radius, sides, width, innerWidth, thickness);
            }

            upDirection.Unitize();
            var vertDir = new Vector3d(startPoint - endPoint);
            vertDir.Unitize();
            var yDir = Vector3d.CrossProduct(vertDir, upDirection);
            yDir.Unitize();

            var centerPoint = (startPoint + endPoint) / 2;

            if (TypeBridge != null && TypeBridge == GuideBridgeType.OctagonalBridge)
            {
                var centerBridge = BrepUtilities.GetGravityCenter(bridge);
                var orientTransform = Transform.Rotation(RhinoMath.ToRadians(90), Plane.WorldXY.XAxis, Plane.WorldXY.Origin);
                var rotateTransform = Transform.Rotation(Plane.WorldXY.XAxis, Plane.WorldXY.YAxis, Plane.WorldXY.ZAxis, upDirection, yDir, vertDir);

                // Calibrate origin to one of the contact point
                var translateTransform = Transform.Translation(new Vector3d(centerPoint));
                var translateToOrigin = Transform.Translation(-new Vector3d(centerBridge));
                bridge.Transform(translateToOrigin);
                bridge.Transform(translateTransform * rotateTransform * orientTransform);
            }
            else
            {
                var orientTransform = Transform.Rotation(RhinoMath.ToRadians(90), Plane.WorldXY.XAxis, Plane.WorldXY.Origin);
                var rotateTransform = Transform.Rotation(Plane.WorldXY.XAxis, Plane.WorldXY.YAxis, Plane.WorldXY.ZAxis, vertDir, yDir, upDirection);
                var translateTransform = Transform.Translation(new Vector3d(centerPoint));
                bridge.Transform(translateTransform * rotateTransform * orientTransform);
            }

            AddFreePointsToBrep(bridge, new List<Point3d> {startPoint, endPoint});

            return bridge;
        }

        private Brep CreateGuideBridgeAtOrigin(double radius, int sides, double width, double innerWidth, double thickness)
        {
            var halfOfWidth = width / 2;
            var halfOfInnerWidth = innerWidth / 2;

            var upDirAtOrigin = Plane.WorldXY.ZAxis;
            var downDirAtOrigin = -Plane.WorldXY.ZAxis;

            var outerPointsAtOrigin = 
                CreateGuideBridgePointsAtOrigin(sides, radius);
            var reinforcedOuterPointsAtOrigin = AddOuterReinforcementPoints(outerPointsAtOrigin, 14.5);
            var outerCurveAtOrigin = new PolylineCurve(reinforcedOuterPointsAtOrigin);
            var outerCurveTop = outerCurveAtOrigin.DuplicateCurve();
            outerCurveTop.Translate(upDirAtOrigin * halfOfWidth);
            var outerCurveBottom = outerCurveAtOrigin.DuplicateCurve();
            outerCurveBottom.Translate(downDirAtOrigin * halfOfWidth);

            //thickness provided is actually for slated (outer corner point to inner corner point)
            var height = halfOfWidth - halfOfInnerWidth;
            var slatedThickness = thickness;
            var flatThickness = Math.Sqrt(Math.Pow(slatedThickness, 2) - Math.Pow(height, 2));

            var innerPointsAtOrigin = CreateGuideBridgePointsAtOrigin(sides, radius - flatThickness);
            var reinforcedInnerPointsAtOrigin = AddInnerReinforcementPoints(
                outerPointsAtOrigin,
                innerPointsAtOrigin,
                reinforcedOuterPointsAtOrigin);
            var innerCurveAtOrigin = new PolylineCurve(reinforcedInnerPointsAtOrigin); 
            
            var innerCurveTop = innerCurveAtOrigin.DuplicateCurve();
            innerCurveTop.Translate(upDirAtOrigin * halfOfInnerWidth);
            var innerCurveBottom = innerCurveAtOrigin.DuplicateCurve();
            innerCurveBottom.Translate(downDirAtOrigin * halfOfInnerWidth);

            var outerExtrusionBrep = CreateGuideBridgeFaceAtOrigin(outerCurveTop, outerCurveBottom);
            FlipToOrient(outerExtrusionBrep, true);
            
            var innerExtrusionBrep = CreateGuideBridgeFaceAtOrigin(innerCurveTop, innerCurveBottom);
            FlipToOrient(innerExtrusionBrep, false);
           
            var topBridgeFace = CreateGuideBridgeFaceAtOrigin(outerCurveTop, innerCurveTop);
            FlipToAlign(topBridgeFace, upDirAtOrigin);

            var bottomBridgeFace = CreateGuideBridgeFaceAtOrigin(outerCurveBottom, innerCurveBottom);
            FlipToAlign(bottomBridgeFace, downDirAtOrigin);

            var bridge = new Brep();
            bridge.Append(topBridgeFace);
            bridge.Append(bottomBridgeFace);
            bridge.Append(outerExtrusionBrep);
            bridge.Append(innerExtrusionBrep);
            bridge.JoinNakedEdges(tolerance);

            return bridge;
        }

        private Brep CreateOctagonalGuideBridgeAtOrigin(double radius, double width, double innerWidth, double thickness, double diameter)
        {
            var halfOfWidth = width / 2;
            var halfOfInnerWidth = innerWidth / 2;

            var upDirAtOrigin = Plane.WorldXY.ZAxis;
            var downDirAtOrigin = -Plane.WorldXY.ZAxis;

            //thickness provided is actually for slated (outer corner point to inner corner point)
            var height = halfOfWidth - halfOfInnerWidth;
            var slatedThickness = thickness;
            var flatThickness = Math.Sqrt(Math.Pow(slatedThickness, 2) - Math.Pow(height, 2));

            var outerOctagonalPoints = CreateOctagonalGuideBridgePoints(radius, diameter);
            var parameter = CMFPreferences.GetActualGuideParameters();
            var maxSideLength = 14.5 - parameter.LightweightParams.OctagonalBridgeCompensation;
            var reinforcedOuterOctagonalPoints = 
                AddOuterReinforcementPoints(outerOctagonalPoints,
                    maxSideLength);
            var outerOctagonalCurve = new PolylineCurve(
                reinforcedOuterOctagonalPoints);

            var innerOctagonalPoints = 
                CreateInnerOctagonalGuideBridgePoints(
                outerOctagonalPoints, flatThickness);
            var reinforcedInnerOctagonalPoints =
                AddInnerReinforcementPoints(
                    outerOctagonalPoints,
                    innerOctagonalPoints,
                    reinforcedOuterOctagonalPoints);
            var innerOctagonalCurve = new PolylineCurve(
                reinforcedInnerOctagonalPoints);

            var outerCurveTop = outerOctagonalCurve.DuplicateCurve();
            outerCurveTop.Translate(upDirAtOrigin * halfOfWidth);
            var outerCurveBottom = outerOctagonalCurve.DuplicateCurve();
            outerCurveBottom.Translate(downDirAtOrigin * halfOfWidth);

            var innerCurveTop = innerOctagonalCurve.DuplicateCurve();
            innerCurveTop.Translate(upDirAtOrigin * halfOfInnerWidth);
            var innerCurveBottom = innerOctagonalCurve.DuplicateCurve();
            innerCurveBottom.Translate(downDirAtOrigin * halfOfInnerWidth);

            var outerExtrusionBrep = CreateGuideBridgeFaceAtOrigin(outerCurveTop, outerCurveBottom);
            FlipToOrient(outerExtrusionBrep, true);

            var innerExtrusionBrep = CreateGuideBridgeFaceAtOrigin(innerCurveTop, innerCurveBottom);
            FlipToOrient(innerExtrusionBrep, false);

            var topBridgeFace = CreateGuideBridgeFaceAtOrigin(outerCurveTop, innerCurveTop);
            FlipToAlign(topBridgeFace, upDirAtOrigin);

            var bottomBridgeFace = CreateGuideBridgeFaceAtOrigin(outerCurveBottom, innerCurveBottom);
            FlipToAlign(bottomBridgeFace, downDirAtOrigin);

            var bridge = new Brep();
            bridge.Append(topBridgeFace);
            bridge.Append(bottomBridgeFace);
            bridge.Append(outerExtrusionBrep);
            bridge.Append(innerExtrusionBrep);
            bridge.JoinNakedEdges(tolerance);

            return bridge;
        }

        #region Octagonal Dimension Reference

        /*       Octagonal Point Reference
         *
         *        diameter (min 10mm)
         *         <--------------->
         *          B "_________" C     ^
         *            / _______ \       |
         *           / /       \ \      |
         *       A "/ /         \ \" D  | 
         *          | |         | |     |   height (min 5.85mm)
         *          | |         | |     |   - This is derived from 
         *          | |         | |     |     the start and end point
         *       H "\ \         / /" E  |
         *           \ \_______/ /      |
         *            \_________/       |
         *          G "         " F     v
         *
         *      - Each " denotes the point
         *      - The angle on A, D, E and H are fixed at 130 degrees
         *      - The angle on B, C, G and F are fixed at 140 degrees
         *      - Length of AB, CD, EF and GH are fixed at 3.2mm
         *
         *      - PS. It is NOT advisable to change any of the calculations below,
         *        unless you have a better way of doing it.
         */

        #endregion

        private List<Point3d> CreateOctagonalGuideBridgePoints(double radius, double diameter)
        {
            var length = 3.2;
            var topHeight = MathUtilitiesV2.FindRightTriangleA(length, 40);
            var height = (radius * 2) - (topHeight * 2);
            var topBottomLength = diameter - MathUtilitiesV2.FindRightTriangleB(topHeight, length) * 2;
            var origin = Point3d.Origin;
            var initialVector = new Vector3d(0, topHeight, 0);
            var points = new List<Point3d>();
            var startEndPoint = Point3d.Add(origin, initialVector);

            // We'll start at Point A
            var currentPoint = startEndPoint;

            // Point B
            currentPoint.Transform(TranslatePointOnXYPlane(130, length));
            points.Add(currentPoint);

            // Point C
            currentPoint.Transform(TranslatePointOnXYPlane(90, topBottomLength));
            points.Add(currentPoint);

            // Point D
            currentPoint.Transform(TranslatePointOnXYPlane(50, length));
            points.Add(currentPoint);

            // Point E
            currentPoint.Transform(Transform.Translation(0, height, 0));
            points.Add(currentPoint);

            // Point F
            currentPoint.Transform(TranslatePointOnXYPlane(180 + 130, length));
            points.Add(currentPoint);

            // Point G
            currentPoint.Transform(Transform.Translation(-topBottomLength, 0, 0));
            points.Add(currentPoint);

            // Point H
            currentPoint.Transform(TranslatePointOnXYPlane(230, length));
            points.Add(currentPoint);

            //add start and end point
            points.Insert(0, startEndPoint);
            points.Add(startEndPoint);

            return points;
        }

        private List<Point3d> CreateInnerOctagonalGuideBridgePoints(
            List<Point3d> outerOctagonalPointsAtOrigin, double thickness)
        {
            var innerPoints = new List<Point3d>();
            var curve = new PolylineCurve(outerOctagonalPointsAtOrigin);

            // We want the inner points to be exactly half of the angle from the outer curves + thickness.
            // Using offset towards the center point of the outer curves will skew the thickness of the octagon's side (Curve AH & Curve DE)

            // Point A
            var point0 = curve.Point(0);
            point0.Transform(TranslatePointOnXYPlane(130 / 2, thickness));
            innerPoints.Add(point0);

            // Point B
            var point1 = curve.Point(1);
            point1.Transform(TranslatePointOnXYPlane(90 - (140 / 2), thickness));
            innerPoints.Add(point1);

            // Point C
            var point2 = curve.Point(2);
            point2.Transform(TranslatePointOnXYPlane(360 - (90 - 140 / 2), thickness));
            innerPoints.Add(point2);

            // Point D
            var point3 = curve.Point(3);
            point3.Transform(TranslatePointOnXYPlane(-(130 / 2), thickness));
            innerPoints.Add(point3);

            // Point E
            var point4 = curve.Point(4);
            point4.Transform(TranslatePointOnXYPlane(180 + (130 / 2), thickness));
            innerPoints.Add(point4);

            // Point F
            var point5 = curve.Point(5);
            point5.Transform(TranslatePointOnXYPlane(270 - (140 / 2), thickness));
            innerPoints.Add(point5);

            // Point G
            var point6 = curve.Point(6);
            point6.Transform(TranslatePointOnXYPlane(90 + (140 / 2), thickness));
            innerPoints.Add(point6);

            // Point H
            var point7 = curve.Point(7);
            point7.Transform(TranslatePointOnXYPlane(180 - (130 / 2), thickness));
            innerPoints.Add(point7);

            // Close the curve
            innerPoints.Add(point0);

            return innerPoints;
        }

        private Transform TranslatePointOnXYPlane(double angle, double distance)
        {
            // Angle is calculated anti clockwise from the Y plane
            var rad  = MathUtilitiesV2.ToRadians(angle);
            return Transform.Translation(distance * Math.Sin(rad), distance * Math.Cos(rad), 0);
        }

        private List<Point3d> CreateGuideBridgePointsAtOrigin(
            int sides, double radius)
        {
            if (sides % 2 != 0)
            {
                throw new Exception($"Guide bridge is not symmetrical. Sides given: {sides}");
            }

            var degreeStep = RhinoMath.ToRadians(360.0 / sides);

            var origin = Point3d.Origin;
            var initialVector = new Vector3d(-radius, 0, 0);
            var points = new List<Point3d>();
            var rotateTransform = Transform.Rotation(degreeStep, origin);
            var startEndPoint = Point3d.Add(origin, initialVector);
            var currentPoint = startEndPoint;

            for (var i = 1; i < sides; i++)
            {
                currentPoint.Transform(rotateTransform);
                points.Add(currentPoint);
            }

            //add start and end point
            points.Insert(0, startEndPoint);
            points.Add(startEndPoint);

            return points;
        }

        private List<Point3d> AddOuterReinforcementPoints(
            List<Point3d> curvePoints,
            double maxSideLength)
        {
            if (!curvePoints.Any())
            {
                return new List<Point3d>();
            }

            var reinforcedCurvePoints = new List<Point3d> { curvePoints[0] };

            for (var curvePointIndex = 1; 
                 curvePointIndex < curvePoints.Count; 
                 curvePointIndex++)
            {
                var previousPoint = curvePoints[curvePointIndex - 1];
                var currentPoint = curvePoints[curvePointIndex];

                var previousToCurrentVector = currentPoint - previousPoint;
                var previousToCurrentLength = 
                    previousToCurrentVector.Length;

                var numberOfReinforcementPointsNeeded = 
                    Math.Floor(previousToCurrentLength / maxSideLength);
                for (var index = 0; index < numberOfReinforcementPointsNeeded; index++)
                {
                    var reinforcementPoint =
                        previousPoint + previousToCurrentVector / (
                            numberOfReinforcementPointsNeeded + 1) * (index + 1);
                    reinforcedCurvePoints.Add(reinforcementPoint);
                }

                reinforcedCurvePoints.Add(currentPoint);
            }

            return reinforcedCurvePoints;
        }

        // outerPoints and innerPoints must have same number of points
        // outerPoints and innerPoints must be arranged similarly
        private List<Point3d> AddInnerReinforcementPoints(
            List<Point3d> outerPoints,
            List<Point3d> innerPoints,
            List<Point3d> reinforcedOuterPoints)
        {
            if (outerPoints.Count != innerPoints.Count)
            {
                throw new ArgumentException("outerPoints and innerPoints " +
                                            "must have the same number of points");
            }

            var reinforcedInnerPoints = new List<Point3d>();
            foreach (var reinforcedOuterPoint in reinforcedOuterPoints)
            {
                // check if its one of the vertices
                var outerPointsIndex = outerPoints.IndexOf(reinforcedOuterPoint);
                if (outerPointsIndex != -1)
                {
                    reinforcedInnerPoints.Add(innerPoints[outerPointsIndex]);
                    continue;
                }

                // otherwise its one of the reinforcement points
                var innerReinforcementPointCalculated = false;
                for (var index = 1; index < outerPoints.Count; index++)
                {
                    var previousOuterPoint = outerPoints[index - 1];
                    var currentOuterPoint = outerPoints[index];

                    var outerLineVector = currentOuterPoint - previousOuterPoint;
                    var lineVectorLength = outerLineVector.Length;
                    outerLineVector.Unitize();
                    var reinforcedLineVector = 
                        reinforcedOuterPoint - previousOuterPoint;
                    var reinforcedLineVectorLength = reinforcedLineVector.Length;
                    reinforcedLineVector.Unitize();
                    if (outerLineVector.EpsilonEquals(reinforcedLineVector, 0.01))
                    {
                        var previousInnerPoint = innerPoints[index - 1];
                        var currentInnerPoint = innerPoints[index];
                        var innerLineVector = 
                            currentInnerPoint - previousInnerPoint;
                        var reinforcementInnerVector =
                            innerLineVector *
                            reinforcedLineVectorLength / lineVectorLength;
                        var reinforcementInnerPoint =
                            previousInnerPoint + reinforcementInnerVector;
                        reinforcedInnerPoints.Add(reinforcementInnerPoint);
                        innerReinforcementPointCalculated = true;
                    }
                }

                if (!innerReinforcementPointCalculated)
                {
                    throw new Exception("Unable to calculate innerReinforcementPoint");
                }
            }

            return reinforcedInnerPoints;
        }

        private Brep CreateGuideBridgeFaceAtOrigin(Curve curveA, Curve curveB)
        {
            var pointsA = Core.Utilities.CurveUtilities.GetCurveControlPoints(curveA);
            var pointsB = Core.Utilities.CurveUtilities.GetCurveControlPoints(curveB);

            if (pointsA.Length != pointsB.Length)
            {
                throw new Exception($"Number of points for curve A and curve B should be the same");
            }

            if (pointsA.Length < 3)
            {
                throw new Exception($"Number of points for curve A and curve B should be more than 3");
            }

            if (pointsA[0] != pointsA[pointsA.Length - 1])
            {
                throw new Exception($"First and last point for curve A should be same");
            }

            if (pointsB[0] != pointsB[pointsB.Length - 1])
            {
                throw new Exception($"First and last point for curve B should be same");
            }

            var startEndPointA = pointsA[0];
            var startEndPointB = pointsB[0];
            var currentPointA = startEndPointA;
            var currentPointB = startEndPointB;

            var bridge = new Brep();

            for (var i = 1; i < pointsA.Length; i++)
            {
                var previousPointA = currentPointA;
                var previousPointB = currentPointB;

                currentPointA = pointsA[i];
                currentPointB = pointsB[i];

                var surface1 = Brep.CreateFromCornerPoints(previousPointA, previousPointB, currentPointA, tolerance);
                bridge.Append(surface1);
                var surface2 = Brep.CreateFromCornerPoints(currentPointA, previousPointB, currentPointB, tolerance);
                bridge.Append(surface2);
            }

            return bridge;
        }

        private void FlipToAlign(Brep brep, Vector3d directionToAlign)
        {
            var brepFace = brep.Faces[0];
            double u, v;
            if (brepFace.ClosestPoint(Point3d.Origin, out u, out v))
            {
                var direction = brepFace.NormalAt(u, v);
                if (RhinoMath.ToDegrees(Vector3d.VectorAngle(direction, directionToAlign)) > 90)
                {
                    brep.Flip();
                }
            }
        }

        private void FlipToOrient(Brep brep, bool shouldFaceOutward)
        {
            var brepFace = brep.Faces[0];
            double u, v;
            if (brepFace.ClosestPoint(Point3d.Origin, out u, out v))
            {
                var point = brepFace.PointAt(u, v);
                var direction = brepFace.NormalAt(u, v);

                Curve[] curves;
                Point3d[] intersectedPoints;
                //offset start point slightly in order not to have intersection with current brep face
                var curve = Curve.CreateControlPointCurve(new []{ Point3d.Add(point, direction * 0.01), Point3d.Add(point, direction * 50) });
                Intersection.CurveBrep(curve, brep, tolerance, out curves, out intersectedPoints);

                if (shouldFaceOutward && intersectedPoints.Length > 0)
                {
                    brep.Flip();
                }
                else if (!shouldFaceOutward && intersectedPoints.Length == 0)
                {
                    brep.Flip();
                }
            }
        }

        private void AddFreePointsToBrep(Brep brep, List<Point3d> points)
        {
            foreach (var point in points)
            {
                brep.Vertices.Add(point, tolerance);
            }
        }

        public Vector3d SetUpBridgeDirection(Point3d startPt, Point3d endPt, Mesh constraintMesh)
        {
            constraintMesh.FaceNormals.ComputeFaceNormals();
            var centerPoint = (startPt + endPt) / 2;
            var meshPoint = constraintMesh.ClosestMeshPoint(centerPoint, 30.0);
            var normalAtPoint = constraintMesh.FaceNormals[meshPoint.FaceIndex];
            var lineDirection = endPt - startPt;
            lineDirection.Unitize();
            var sideDirection = Vector3d.CrossProduct(normalAtPoint, lineDirection);
            var upDirection = Vector3d.CrossProduct(lineDirection, sideDirection);

#if (INTERNAL)
            if (CMFImplantDirector.IsDebugMode)
            {
                InternalUtilities.AddPoint(startPt, "GuideBridge::StartPoint", Color.Black);
                InternalUtilities.AddPoint(endPt, "GuideBridge::EndPoint", Color.Black);
                InternalUtilities.AddVector(centerPoint, lineDirection, 25.0, Color.Red);
                InternalUtilities.AddVector(centerPoint, sideDirection, 25.0, Color.Green);
                InternalUtilities.AddVector(centerPoint, upDirection, 25.0, Color.Blue);
            }
#endif
            return upDirection;
        }
    }
}
