using IDS.Core.Utilities;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Utilities
{
    public class GuideFlangeOutlineHelper
    {
        private readonly List<Curve> _intersectionCurves;
        public GuideFlangeOutlineHelper(List<Curve> intersectionCurves)
        {
            _intersectionCurves = intersectionCurves;
        }

        //trim out flange outline into 2 curve. 1 for editable and another 1 is fixed
        public bool TrimCurveCloseToInterOutlines(Curve flangeOutline,
            out Curve curveToEdit, out Curve curveConstraint)
        {
            curveToEdit = null;
            curveConstraint = null;            

            var intersectionPts = new List<Point3d>();
            foreach (var interCurve in _intersectionCurves)
            {
                CurveUtilities.GetCurveControlPoints(interCurve).ToList().ForEach(x => intersectionPts.Add(x));
            }
            var flangeControlPts = CurveUtilities.GetCurveControlPoints(flangeOutline);
            var startEndPtList = GetIntersectionCurvePoints(flangeControlPts, intersectionPts);
            if(startEndPtList.Count == 0)
            {
                return false;
            }
         
            double tolerance = 0.001;
            var dupeCurveFirst = flangeOutline.DuplicateCurve();
            var dupeCurveSecond = flangeOutline.DuplicateCurve();

            double tStart, tEnd;
            var success = true;
            success &= flangeOutline.ClosestPoint(startEndPtList.First(), out tStart, tolerance);
            success &= flangeOutline.ClosestPoint(startEndPtList.Last(), out tEnd, tolerance);
            if (!success)
            {
                return false;
            }

            var firstCurve = dupeCurveFirst.Trim(tStart, tEnd);
            var secondCurve = dupeCurveSecond.Trim(tEnd, tStart);
            if (firstCurve == null || secondCurve == null)
            {
                return false;
            }
            double firstCurveDistance;
            double secondCurveDistance;
            if (!GetAverageDistance(firstCurve, secondCurve, flangeOutline, out firstCurveDistance, out secondCurveDistance))
            {
                return false;
            }

            curveToEdit = (firstCurveDistance < secondCurveDistance) ? secondCurve : firstCurve;
            curveConstraint = (firstCurveDistance < secondCurveDistance) ? firstCurve : secondCurve;
            return true;
        }

        private List<Point3d> GetIntersectionCurvePoints(Point3d[] controlPoints, List<Point3d> orginalInterPts)
            {
            var startEndPtList = new List<Point3d>();
            foreach (var flangePt in controlPoints)
            {
                if (orginalInterPts.Any(x => x.DistanceTo(flangePt) < 0.001))
                {
                    startEndPtList.Add(flangePt);
                }
                else
                {
                    if (startEndPtList.Count != 0)
                    {
                        break;
                    }
                }
            }

            foreach (var reverseFlangePt in controlPoints.Reverse())
            {
                if (orginalInterPts.Any(x => x.DistanceTo(reverseFlangePt) < 0.01))
                {
                    startEndPtList.Insert(0, reverseFlangePt);
                }
                else
                {
                    if (startEndPtList.Count != 0)
                    {
                        break;
                    }
                }
            }
            return startEndPtList;
        }

        private bool GetAverageDistance(Curve firstCurve, Curve secondCurve, Curve targetCurve,
            out double firstCurveDistance, out double secondCurveDistance)
        {
            List<double> firstCurveDistances = new List<double>();
            List<double> secondCurveDistances = new List<double>();

            foreach (var targetPt in CurveUtilities.GetCurveControlPoints(targetCurve))
            {
                var firstCurvePts = CurveUtilities.GetCurveControlPoints(firstCurve);
                firstCurvePts.ToList().ForEach(pt => firstCurveDistances.Add(pt.DistanceTo(targetPt)));

                var secondCurvePts = CurveUtilities.GetCurveControlPoints(secondCurve);
                secondCurvePts.ToList().ForEach(pt => secondCurveDistances.Add(pt.DistanceTo(targetPt)));
            }

            firstCurveDistance = 0.00;
            secondCurveDistance = 0.00;
            if (firstCurveDistances.Count == 0 || secondCurveDistances.Count == 0)
            {
                return false;
            }
            firstCurveDistance = firstCurveDistances.Sum() / firstCurveDistances.Count;
            secondCurveDistance = secondCurveDistances.Sum() / secondCurveDistances.Count;
            return true;
        }
    }
}
