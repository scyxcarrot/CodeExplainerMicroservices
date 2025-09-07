using IDS.Core.Drawing;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Geometry;
using Rhino.Input;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Glenius.Operations
{
    public class SolidWallCurveCreator
    {
        public enum EResult
        {
            Canceled,
            Failed,
            Success
        }

        private readonly RhinoDoc document;
        private readonly Curve topCurve;
        private readonly Curve bottomCurve;
        private readonly Mesh topDownMesh;

        public Curve SolidWallCurve { get; private set; }

        public SolidWallCurveCreator(RhinoDoc doc, Curve topCurve, Curve bottomCurve, Mesh topDownMesh)
        {
            document = doc;
            this.topCurve = topCurve;
            this.bottomCurve = bottomCurve;
            this.topDownMesh = topDownMesh;
        }

        private bool IsEndPointOnTopCurve(Curve curve)
        {
            double dummy;
            var closest = CurveUtilities.GetClosestPoint(topCurve, curve.PointAtEnd, out dummy);
            return curve.PointAtEnd.DistanceTo(closest) <= 0.01;
        }

        private bool IsOnCurve(Point3d pt, Curve curve)
        {
            double dummy;
            return (CurveUtilities.GetClosestPoint(curve, pt, out dummy) - pt).Length < 0.001;
        }

        public EResult Edit(Curve existingCurve, out Curve editedCurve)
        {
            var filteredCurve = CurveUtilities.TrimOverlappedSection(existingCurve, topCurve, false);

            DrawCurve dc = new DrawCurve(document);
            dc.SetExistingCurve(filteredCurve, false, false);
            dc.SetIsClosedCurve(false);
            dc.SnapCurves = new List<Curve>() { bottomCurve };
            dc.ConstraintMesh = topDownMesh;
            dc.AlwaysOnTop = true;
            dc.AcceptNothing(true); // Pressing ENTER is allowed
            dc.AcceptUndo(true); // Enables ctrl-z
            
            dc.OnDynamicDrawPreRebuildCurve += (currPt, movingPointIndex) =>
            {
                double d;
                var clstPt = CurveUtilities.GetClosestPoint(topCurve, currPt, out d);
                if (!IsOnCurve(dc.StartingPoint, topCurve) || movingPointIndex == 0)
                {
                    dc.StartingPoint = clstPt;
                }

                if (!IsOnCurve(dc.EndPoint, topCurve) || movingPointIndex == dc.NumberOfControlPoints - 1)
                {
                    dc.EndPoint = clstPt;
                }
            };

            editedCurve = dc.Draw();

            if (dc.Result() == GetResult.Nothing && editedCurve != null)
            {
                if (!HandleCurveCreation(editedCurve, out editedCurve))
                {
                    return EResult.Failed;
                }
                return EResult.Success;
            }

            return EResult.Canceled;
        }

        public EResult Draw()
        {
            DrawCurve dc = new DrawCurve(document);
            SolidWallCurve = null;

            double startingPointOnCurveParam = double.NaN, endPointOnCurveParam = double.NaN;

            dc.UniqueCurves = true;
            dc.AlwaysOnTop = false;
            dc.SnapCurves = new List<Curve>() { topCurve, bottomCurve };
            dc.ConstraintMesh = topDownMesh;
            dc.AcceptNothing(true); // Pressing ENTER is allowed
            dc.AcceptUndo(true); // Enables ctrl-z
            dc.SetIsClosedCurve(false);

            //Preview
            dc.OnDynamicDrawing += (currPt) =>
            {
                if (dc.GetNumberOfControlPoints() < 2)
                {
                    dc.StartingPoint = CurveUtilities.GetClosestPoint(topCurve, currPt, out startingPointOnCurveParam);
                }
            };

            //On adding new point when drawing curve
            dc.OnNewCurveAddPoint += (currPt) =>
            {
                var ptClosest = CurveUtilities.GetClosestPoint(topCurve, currPt, out endPointOnCurveParam);

                //When the user add point on top curve
                if (currPt.DistanceTo(ptClosest) < 0.01)
                {
                    dc.AddPoint(ptClosest);
                    return false; //Stop asking user input to add point
                }

                return true;
            };

            var sideWallCurve = dc.Draw();
            if (dc.Result() == GetResult.Nothing)
            {
                return EResult.Failed;
            }

            if (dc.Result() != GetResult.Cancel && IsEndPointOnTopCurve(sideWallCurve) &&
                     !double.IsNaN(startingPointOnCurveParam) && !double.IsNaN(endPointOnCurveParam))
            {
                Curve createdCurve;
                if (HandleCurveCreation(sideWallCurve, out createdCurve))
                {
                    SolidWallCurve = createdCurve;
                    return EResult.Success;
                }

                return EResult.Failed;
            }

            return EResult.Canceled;
        }

        private bool HandleCurveCreation(Curve sideWallCurve, out Curve createdCurve)
        {
            createdCurve = null;
            double startingPointOnCurveParam, endPointOnCurveParam;

            var ctrlPoints = CurveUtilities.GetCurveControlPoints(sideWallCurve);
            CurveUtilities.GetClosestPoint(topCurve, ctrlPoints[0], out startingPointOnCurveParam);
            CurveUtilities.GetClosestPoint(topCurve, ctrlPoints[ctrlPoints.Length - 1], out endPointOnCurveParam);

            Curve topCurveCopy = topCurve.DuplicateCurve();
            Curve[] trimmedCurves = { topCurveCopy.Trim(endPointOnCurveParam, startingPointOnCurveParam), topCurveCopy.Trim(startingPointOnCurveParam, endPointOnCurveParam) };

            var trimmedCurve = trimmedCurves.OrderBy(x => x.GetLength()).FirstOrDefault();
            var joinedCurves = Curve.JoinCurves(new[] { sideWallCurve, trimmedCurve });

            if (joinedCurves.Length == 1)
            {

                createdCurve = joinedCurves.FirstOrDefault();

                var success = createdCurve?.MakeClosed(10);

                if (success.HasValue && success.Value)
                {
                    return true;
                }

                return false;
            }

            return false;
        }

    }
}
