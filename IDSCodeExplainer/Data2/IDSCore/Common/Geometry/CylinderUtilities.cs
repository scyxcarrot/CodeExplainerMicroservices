using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Core.Utilities
{
    public static class CylinderUtilities
    {
        public static Cylinder CreateCylinderFromBoundingBox(BoundingBox box, Plane plane, Point3d point, double height, double radiusOffset)
        {
            var xLength = Math.Abs((box.Corner(true, true, true) - box.Corner(false, true, true)).Length);
            var yLength = Math.Abs((box.Corner(true, true, true) - box.Corner(true, false, true)).Length);
            var zLength = Math.Abs((box.Corner(true, true, true) - box.Corner(true, true, false)).Length);
            var orderedList = new List<double> { xLength, yLength, zLength }.OrderBy(length => length);
            var circle = new Circle(plane, point, (orderedList.First() / 2) + radiusOffset);
            var cylinder = new Cylinder(circle, height);
            return cylinder;
        }

        public static Cylinder CreateCylinder(double diameter, Point3d originCenterPoint, Vector3d direction, double height)
        {
            var plane = new Plane(originCenterPoint, direction);
            var circle = new Circle(plane, diameter / 2);
            var cylinder = new Cylinder(circle, height);
            return cylinder;
        }

    }
}