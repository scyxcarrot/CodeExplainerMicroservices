using IDS.CMF.DataModel;
using IDS.CMF.Preferences;
using IDS.CMF.V2.DataModel;
using Rhino.Geometry;
using System;

namespace IDS.CMF.Factory
{
    public class LandmarkBrepFactory
    {
        private const double tolerance = 0.001;
        private LandmarkImplantParams landmarkImplantParams;

        public LandmarkBrepFactory(LandmarkImplantParams landmarkImplantParams)
        {
            this.landmarkImplantParams = landmarkImplantParams;
        }

        //Landmark is positioned at Plane.WorldXY and centered at Point3d.Origin
        public Brep CreateLandmark(LandmarkType landmarkType, double thickness, double pastilleRadius)
        {
            Brep brep = null;

            switch (landmarkType)
            {
                case LandmarkType.Circle:
                    brep = CreateCircleLandmark(thickness, pastilleRadius);
                    break;
                case LandmarkType.Rectangle:
                    brep = CreateRectangleLandmark(thickness, pastilleRadius);
                    break;
                case LandmarkType.Triangle:
                    brep = CreateTriangleLandmark(thickness, pastilleRadius);
                    break;
            }
            
            return brep;
        }

        public Brep CreateLandmarkAdjustedByWrapOffset(LandmarkType landmarkType, double thickness, double pastilleRadius, double wrapOffset)
        {
            Brep brep = null;

            switch (landmarkType)
            {
                case LandmarkType.Circle:
                    brep = CreateCircleLandmark(thickness, pastilleRadius, wrapOffset);
                    break;
                case LandmarkType.Rectangle:
                    brep = CreateRectangleLandmark(thickness, pastilleRadius, wrapOffset);
                    break;
                case LandmarkType.Triangle:
                    brep = CreateTriangleLandmark(thickness, pastilleRadius, wrapOffset);
                    break;
            }

            return brep;
        }

        private Brep CreateCircleLandmark(double thickness, double pastilleRadius, double wrapOffset = 0.0)
        {
            var plane = Plane.WorldXY;
            var baseCircleCenter = Point3d.Add(Point3d.Origin, new Vector3d(0, 0, -thickness / 2));
            var landmarkRadius = Math.Abs((pastilleRadius * landmarkImplantParams.CircleRadiusRatioWithPastilleRadius) / 2 - wrapOffset);
            var circle = new Circle(plane, baseCircleCenter, landmarkRadius);
            var cylinder = new Cylinder(circle, thickness);
            var brep = Brep.CreateFromCylinder(cylinder, true, true);
            return brep;
        }

        //RectangleWidth is towards Plane.WorldXY.XAxis and RectangleLength is towards Plane.WorldXY.YAxis
        private Brep CreateRectangleLandmark(double thickness, double pastilleRadius, double wrapOffset = 0.0)
        {
            var landmarkWidth = GetRectangleWidth(pastilleRadius, wrapOffset);
            var landmarkHeight = landmarkWidth;
            var widthVector = Vector3d.Multiply(Plane.WorldXY.XAxis, landmarkWidth);
            var lengthVector = Vector3d.Multiply(Plane.WorldXY.YAxis, landmarkHeight);
            var center = Point3d.Origin;

            var point1 = Point3d.Add(center, Vector3d.Add(-widthVector/2, -lengthVector/2));
            var point2 = Point3d.Add(point1, widthVector);
            var point3 = Point3d.Add(point2, lengthVector);
            var point4 = Point3d.Add(point1, lengthVector);

            var surface = Brep.CreateFromCornerPoints(point1, point2, point3, point4, tolerance);
            var brep = Brep.CreateFromOffsetFace(surface.Faces[0], thickness / 2, tolerance, true, true);
            return brep;
        }

        //TriangleHeight is towards Plane.WorldXY.XAxis and TriangleBaseLength is towards Plane.WorldXY.YAxis
        private Brep CreateTriangleLandmark(double thickness, double pastilleRadius, double wrapOffset = 0.0)
        {
            var compensatedPastilleRadius = pastilleRadius - wrapOffset;

            //take triangleBaseLength as a, use formula: a^2 = b^2 + c^2 where b == c
            var triangleBaseLength = Math.Sqrt(2 * Math.Pow(compensatedPastilleRadius, 2));
            var triangleHeight = triangleBaseLength / 2;

            var heightVector = Vector3d.Multiply(Plane.WorldXY.XAxis, triangleHeight * landmarkImplantParams.TriangleHeightRatioWithDefault);
            var baseVector = Vector3d.Multiply(Plane.WorldXY.YAxis, triangleBaseLength);
            var center = Point3d.Origin;

            var point1 = Point3d.Add(center, Vector3d.Add(-heightVector / 2, -baseVector / 2));
            var point2 = Point3d.Add(point1, Vector3d.Add(heightVector, baseVector / 2));
            var point3 = Point3d.Add(point1, baseVector);

            var surface = Brep.CreateFromCornerPoints(point1, point2, point3, tolerance);
            var brep = Brep.CreateFromOffsetFace(surface.Faces[0], thickness / 2, tolerance, true, true);

            var relativePastilleCenter = Point3d.Add(center, Vector3d.Multiply(-Plane.WorldXY.XAxis, pastilleRadius));
            var actualCenter = Point3d.Add(relativePastilleCenter, Vector3d.Multiply(Plane.WorldXY.XAxis, triangleHeight * 1.5));
            var translateToActualCenter = actualCenter - center;
            var translateTransform = Transform.Translation(translateToActualCenter);
            brep.Transform(translateTransform);

            return brep;
        }

        public Transform GetInitialTransform(LandmarkType landmarkType, Point3d pastillePoint, Vector3d pastilleDirection, double pastilleRadius)
        {
            var worldPlane = Plane.WorldXY;
            var pastillePlane = new Plane(pastillePoint, pastilleDirection);

            //transformation to pastille's center
            var rotateTransform = Transform.Rotation(worldPlane.XAxis, worldPlane.YAxis, worldPlane.ZAxis, pastillePlane.XAxis, pastillePlane.YAxis, pastillePlane.ZAxis);
            var translateTransform = Transform.Translation(new Vector3d(pastillePlane.Origin));

            //transformation to pastille's side
            var moveToSideTransform = Transform.Translation(Vector3d.Multiply(pastillePlane.XAxis, GetDistanceFromPastilleCenterToLandmarkCenter(landmarkType, pastilleRadius)));

            var transform = moveToSideTransform * translateTransform * rotateTransform;
            return transform;
        }

        public Transform GetTransform(LandmarkType landmarkType, Point3d pastillePoint, Vector3d pastilleDirection, Point3d landmarkPoint, double pastilleRadius)
        {
            var pastillePlane = new Plane(pastillePoint, pastilleDirection);
            var direction = landmarkPoint - pastillePlane.Origin;

            //transformation to initial
            var initialTransform = GetInitialTransform(landmarkType, pastillePoint, pastilleDirection, pastilleRadius);

            //transformation to landmark's point
            var moveTransform = Transform.Rotation(pastillePlane.XAxis, direction, pastillePlane.Origin);

            var transform = moveTransform * initialTransform;
            return transform;
        }

        public Brep CreateBaseLandmark(LandmarkType landmarkType, double thickness, double pastilleRadius, double wrapOffset)
        {
            return CreateLandmarkAdjustedByWrapOffset(landmarkType, thickness * 2, pastilleRadius * 1.2, wrapOffset);
        }

        private double GetDistanceFromPastilleCenterToLandmarkCenter(LandmarkType landmarkType, double pastilleRadius)
        {
            var distance = pastilleRadius;

            //for LandmarkType.Triangle, distance is based on pastilleRadius
            switch (landmarkType)
            {
                case LandmarkType.Circle:
                    distance = pastilleRadius * landmarkImplantParams.CircleCenterRatioWithPastilleRadius;
                    break;
                case LandmarkType.Rectangle:
                    var landmarkWidth = GetRectangleWidth(pastilleRadius);
                    distance = pastilleRadius - (landmarkWidth / 2) + landmarkImplantParams.SquareExtensionFromPastilleCircumference;
                    break;
            }
            
            return distance;
        }

        private double GetRectangleWidth(double pastilleRadius, double wrapOffset = 0.0)
        {
            var landmarkWidth = (pastilleRadius * landmarkImplantParams.SquareWidthRatioWithPastilleRadius) - 2 * wrapOffset;
            return landmarkWidth;
        }
    }
}
