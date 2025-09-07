using IDS.CMF;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.Core.Enumerators;
using IDS.Core.Utilities;
using Rhino.Geometry;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Locking = IDS.CMF.Operations.Locking;

namespace IDS.PICMF.Operations
{
    public class CurveOnOsteotomyCutGetter : ICurveGetter
    {
        private readonly CMFImplantDirector _director;

        private TransitionCurveGetterHelper _helper;

        private List<Curve> _constraintCurves;
        private List<Curve> _intersectionCurves;
        private Dictionary<Guid, Curve> _guidingOutlineDictionary;

        public CurveOnOsteotomyCutGetter(CMFImplantDirector director)
        {
            _director = director;

            var objectManager = new CMFObjectManager(_director);
            var implantSupportGuidingBlocks = objectManager.GetAllBuildingBlocks(IBB.ImplantSupportGuidingOutline);
            _guidingOutlineDictionary = new Dictionary<Guid, Curve>();
            _intersectionCurves = new List<Curve>();
            foreach (var guidingBlock in implantSupportGuidingBlocks)
            {
                var curve = (Curve)guidingBlock.Geometry;
                _guidingOutlineDictionary.Add(guidingBlock.Id, curve);
                _intersectionCurves.Add(curve);
            }

            _helper = new TransitionCurveGetterHelper();
        }

        public void OnPreGetting(ref GetPoint getPoints, Color conduitColor)
        {
            var doc = _director.Document;

            Locking.UnlockImplantSupportGuidingOutline(doc);

            _constraintCurves = _intersectionCurves.ToList();
            _helper.SetConstraintCurveConduits(_constraintCurves);
            _helper.SetPointConduits(conduitColor);
            _helper.SetTrimmedCurveConduit(conduitColor);

            if (_constraintCurves.Count == 1)
            {
                getPoints.Constrain(_constraintCurves.First(), false);
            }
        }

        public void OnCancel()
        {
            _helper.DisableAllConduits();
        }

        public void OnPointPicked(ref GetPoint getPoints)
        {
            if (_helper.Point1 == Point3d.Unset)
            {
                Curve pickedCurve = null;
                Point3d pickedPoint;
                if (_constraintCurves.Count == 1)
                {
                    pickedPoint = getPoints.Point();
                    pickedCurve = GetPickedCurve(_constraintCurves, pickedPoint);
                }
                else
                {
                    pickedPoint = GetPointOnCurve(_constraintCurves, getPoints, double.MaxValue, out pickedCurve);
                }

                if (pickedCurve == null)
                {
                    IDSPICMFPlugIn.WriteLine(LogCategory.Error, "Picked Point is invalid!");
                    return;
                }

                _helper.Point1 = pickedPoint;
                _helper.Point1Conduit.Center = _helper.Point1;
                _helper.Point1Conduit.Enabled = true;
                //filter constrain to only the selected curve
                getPoints.ClearConstraints();
                _constraintCurves.Clear();
                _constraintCurves.Add(pickedCurve);
                getPoints.Constrain(pickedCurve, false);

                _helper.ClearConstraintCurveConduits();
                _helper.SetConstraintCurveConduits(new List<Curve> { pickedCurve });
            }
            else if (_helper.Point2 == Point3d.Unset)
            {
                var pickedPoint = getPoints.Point();
                var pickedCurve = GetPickedCurve(_constraintCurves, pickedPoint);

                if (pickedCurve == null)
                {
                    IDSPICMFPlugIn.WriteLine(LogCategory.Error, "Picked Point is invalid! Please check that the point is on the same guiding outline where picked point 1 is!");
                    return;
                }
                else if (_helper.Point1 == pickedPoint)
                {
                    IDSPICMFPlugIn.WriteLine(LogCategory.Error, "Picked Point is identical!");
                    return;
                }

                var trimmedCurve = _helper.TrimCurve(_helper.Point1, pickedPoint, _constraintCurves.First());
                if (trimmedCurve == null)
                {
                    IDSPICMFPlugIn.WriteLine(LogCategory.Error, "Unable to trim curve!");
                    return;
                }

                _helper.Point2 = pickedPoint;
                _helper.Point2Conduit.Center = _helper.Point2;
                _helper.Point2Conduit.Enabled = true;

                _helper.TrimmedCurveConduit.CurvePreview = trimmedCurve;
                _helper.TrimmedCurveConduit.Enabled = true;
            }
        }

        public void OnUndo(ref GetPoint getPoints)
        {
            if (_helper.Point2 != Point3d.Unset)
            {
                //undo point2 selection
                _helper.Point2 = Point3d.Unset;
                _helper.Point2Conduit.Center = _helper.Point2;
                _helper.Point2Conduit.Enabled = false;

                _helper.TrimmedCurveConduit.CurvePreview = null;
                _helper.TrimmedCurveConduit.Enabled = false;
            }
            else if (_helper.Point1 != Point3d.Unset)
            {
                //undo point1 selection
                _helper.Point1 = Point3d.Unset;
                _helper.Point1Conduit.Center = _helper.Point1;
                _helper.Point1Conduit.Enabled = false;

                //add back constrains
                getPoints.ClearConstraints();
                _constraintCurves.Clear();
                _constraintCurves = _intersectionCurves.ToList();
                if (_constraintCurves.Count == 1)
                {
                    getPoints.Constrain(_constraintCurves.First(), false);
                }

                _helper.ClearConstraintCurveConduits();
                _helper.SetConstraintCurveConduits(_constraintCurves);
            }
        }

        public bool OnFinalized(out ImplantTransitionInputCurveDataModel outputDataModel)
        {
            outputDataModel = null;

            if (_helper.Point1 == Point3d.Unset || _helper.Point2 == Point3d.Unset)
            {
                _helper.Point1Conduit.Enabled = false;
                _helper.Point2Conduit.Enabled = false;
                _helper.TrimmedCurveConduit.Enabled = false;
                _helper.ClearConstraintCurveConduits();
                return false;
            }

            var constraintCurve = _constraintCurves.First();
            outputDataModel = new ImplantTransitionInputCurveDataModel();
            outputDataModel.FullCurve = constraintCurve;
            outputDataModel.TrimmedCurve = _helper.TrimCurve(_helper.Point1, _helper.Point2, outputDataModel.FullCurve);

            if (outputDataModel.TrimmedCurve == null)
            {
                _helper.Point1Conduit.Enabled = false;
                _helper.Point2Conduit.Enabled = false;
                _helper.TrimmedCurveConduit.Enabled = false;
                _helper.ClearConstraintCurveConduits();
                IDSPICMFPlugIn.WriteLine(LogCategory.Error, "Points picked could not generate valid curve! Please check that the points are on the same guiding outline!");
                return false;
            }

            outputDataModel.DerivedObjectGuid = _guidingOutlineDictionary.First(o => o.Value == constraintCurve).Key;

            _helper.Point1Conduit.Enabled = false;
            _helper.Point2Conduit.Enabled = false;
            _helper.TrimmedCurveConduit.Enabled = false;
            _helper.ClearConstraintCurveConduits();

            return true;
        }

        public void OnMouseMove(object sender, GetPointMouseEventArgs e)
        {
            //do nothing
        }
        
        private Curve GetPickedCurve(List<Curve> curves, Point3d pickedPoint)
        {
            Curve endPtClosestCurve;
            double curveonParams;
            var ptClosest = CurveUtilities.GetClosestPointFromCurves(curves, pickedPoint, out endPtClosestCurve, out curveonParams);

            if (pickedPoint.DistanceTo(ptClosest) > 0.001)
            {
                return null;
            }

            return endPtClosestCurve;
        }

        private Point3d GetPointOnCurve(List<Curve> curves, GetPoint getPoint, double maxDistance, out Curve closestCurve)
        {
            var pointOnCurve = Point3d.Unset;
            closestCurve = null;

            var pickedPoint2d = getPoint.Point2d();
            var viewport = getPoint.View().ActiveViewport;
            var picker = new PickContext();
            picker.View = viewport.ParentView;
            picker.PickStyle = PickStyle.PointPick;
            var xform = viewport.GetPickTransform(pickedPoint2d);
            picker.SetPickTransform(xform);

            var distanceFromCamera = double.MinValue;
            foreach (var scurve in curves)
            {
                double t;
                double depth;
                double distance;
                if (picker.PickFrustumTest(scurve.ToNurbsCurve(), out t, out depth, out distance) && depth > distanceFromCamera)
                {
                    distanceFromCamera = depth;
                    pointOnCurve = scurve.PointAt(t);
                    closestCurve = scurve;
                }
            }

            if (pointOnCurve != Point3d.Unset && closestCurve != null && distanceFromCamera < 1.0)
            {
                return pointOnCurve;
            }

            var pickedPoint = getPoint.Point();

            foreach (var scurve in curves)
            {
                double t;
                if (scurve.ClosestPoint(pickedPoint, out t, maxDistance))
                {
                    if (pointOnCurve == Point3d.Unset || scurve.PointAt(t).DistanceTo(pickedPoint) < pointOnCurve.DistanceTo(pickedPoint))
                    {
                        pointOnCurve = scurve.PointAt(t);
                        closestCurve = scurve;
                    }
                }
            }

            return pointOnCurve;
        }
    }
}
