using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Core.V2.MTLS.Operation;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMFImplantCreation.Utilities
{
    public static class CurveUtilities
    {
        public static ICurve GetClosedIntersectionCurve(IConsole console, IMesh mesh, IMesh extrusion)
        {
            var intersectedCurves = Curves.IntersectionCurve(console, mesh, extrusion);

            intersectedCurves = FilterNoiseCurves(console, intersectedCurves, 2);

            if (intersectedCurves.Count > 1)
            {
                var epsilon = 0.0001;

                var tempCurves = intersectedCurves.ToList();
                for (var i = tempCurves.Count - 1; i > 0; i--)
                {
                    // Simple join
                    var curve = tempCurves[i];
                    var prevCurve = tempCurves[i - 1];
                    if (curve.Points.First().EpsilonEquals(prevCurve.Points.Last(), epsilon))
                    {
                        var points = new List<IPoint3D>();
                        points.AddRange(prevCurve.Points.ToList());
                        points.AddRange(curve.Points.ToList());
                        tempCurves.Remove(prevCurve);
                        tempCurves.Remove(curve);
                        var joinedCurve = new IDSCurve(points);
                        tempCurves.Insert(i - 1, joinedCurve);
                    }
                }

                intersectedCurves = tempCurves;
            }

            var intersectedCurve = intersectedCurves.First();

            if (!intersectedCurve.IsClosed())
            {
                intersectedCurve.MakeClosed(1);
            }

            return intersectedCurve;
        }

        public static List<ICurve> FilterNoiseCurves(IConsole console, List<ICurve> curves, double tolerance)
        {
            var filtered = new List<ICurve>();
            var lengths = Curves.GetCurvesLength(console, curves);

            for (var i = 0; i < lengths.Count; i++)
            {
                if (lengths[i] > tolerance)
                {
                    filtered.Add(curves[i]);
                }
            }

            return filtered;
        }

        public static ICurve FindFurthermostCurveAlongVector(List<ICurve> curves, IVector3D direction)
        {
            var curveStartPoints = new List<IPoint3D>();
            curves.ForEach(x =>
            {
                curveStartPoints.AddRange(x.Points);
            });

            var furthermostPoint = PointUtilities.FindFurthermostPointAlongVector(curveStartPoints.ToArray(), direction);

            return curves.First(l =>
            {
                var pts = l.Points;
                return pts.Any(pt => pt.X == furthermostPoint.X && pt.Y == furthermostPoint.Y && pt.Z == furthermostPoint.Z);
            });
        }

        public static List<ICurve> GetSharpAngleCurves(List<IPoint3D> points, int startDiviate, int endDiviate)
        {
            var curves = new List<ICurve>();
            if (startDiviate != -1 && endDiviate != -1)
            {
                var sharpCurvePoints = 
                    points
                        .Skip(startDiviate)
                        .Take(endDiviate - startDiviate + 1)
                        .ToList();
                var sharpCurve = new IDSCurve(sharpCurvePoints);

                curves.Add(sharpCurve);
            }
            return curves;
        }
    }
}
