using Rhino.Geometry;
using System.Collections.Generic;

namespace IDS.Core.Utilities
{
    public static class LineUtilities
    {
        public static Line CreateLine(Point3d startPoint, Vector3d direction, double length)
        {
            direction.Unitize();

            var line = new Line(startPoint, direction, length);
            return line;
        }

        public static Point3d GetClosestPoint(List<Line> lines, Point3d point)
        {
            var nearestPt = Point3d.Unset;

            lines.ForEach(x =>
            {
                var closest = x.ClosestPoint(point, true);

                if (nearestPt == Point3d.Unset)
                {
                    nearestPt = closest;
                }
                else
                {
                    if (closest.DistanceTo(point) < nearestPt.DistanceTo(point))
                    {
                        nearestPt = closest;
                    }
                }
            });

            return nearestPt;

        }
    }
}