using IDS.CMF;
using IDS.CMF.Constants;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.Core.Drawing;
using IDS.Core.Enumerators;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IDS.PICMF.Operations
{
    public class GuideFlangeInputsGetter
    {
        public enum EResult
        {
            Canceled,
            Failed,
            Success
        }

        private readonly CMFImplantDirector _director;
        private readonly List<Curve> _intersectionCurves;
        public Curve FlangeCurve { get; private set; }
        public Mesh OsteotomyParts { get; private set; }
        public double FlangeHeight { get; private set; }

        public GuideFlangeInputsGetter(CMFImplantDirector director)
        {
            _director = director;

            var objManager = new CMFObjectManager(director);
            var osteotomyParts = ProPlanImportUtilities.GetAllOriginalOsteotomyParts(_director.Document);
            var duplicatedMeshes = osteotomyParts.Select(mesh => mesh.DuplicateMesh());
            OsteotomyParts = MeshUtilities.AppendMeshes(duplicatedMeshes);
            if (OsteotomyParts == null)
            {
                throw new Exception("Osteotomy part invalid.");
            }

            var guideFlangeGuidingBlock = objManager.GetAllBuildingBlocks(IBB.GuideFlangeGuidingOutline);
            _intersectionCurves = new List<Curve>();
            guideFlangeGuidingBlock.ToList().ForEach(x => _intersectionCurves.Add((Curve)x.Geometry));
            if (_intersectionCurves.Count == 0)
            {
                throw new Exception("Guide Flange Guiding Outline invalid.");
            }
        }

        public EResult GetInputs()
        {
            FlangeHeight = GuideFlangeParameters.DefaultHeight;

            var getPoints = new GetPoint();
            getPoints.AcceptNothing(true);
            getPoints.SetCommandPrompt("Select 2 points on the guide flange guiding outline and <ENTER> to finalize");
            getPoints.AcceptUndo(true);

            var constraintCurveConduits = new List<CurveConduit>();
            var constraintCurves = _intersectionCurves.ToList();
            foreach (var curve in constraintCurves)
            {
                var curveConduit = new CurveConduit
                {
                    CurveColor = IDS.CMF.Visualization.Colors.GuideFlangeGuidingOutline,
                    CurveThickness = 2,
                    CurvePreview = curve,
                    Enabled = true
                };
                constraintCurveConduits.Add(curveConduit);
            }

            if (constraintCurves.Count == 1)
            {
                getPoints.Constrain(constraintCurves.First(), false);
            }
            else
            {
                getPoints.Constrain(OsteotomyParts, false);
            }

            var point1 = Point3d.Unset;
            var point2 = Point3d.Unset;
            var pointConduitDiameter = 1.0;
            var pointConduitTransparency = 0.0;
            var pointConduitColor = IDS.CMF.Visualization.Colors.GuideFlange;
            var point1Conduit = new FullSphereConduit(point1, pointConduitDiameter, pointConduitTransparency, pointConduitColor);
            var point2Conduit = new FullSphereConduit(point2, pointConduitDiameter, pointConduitTransparency, pointConduitColor);
            var trimmedCurveConduit = new CurveConduit();
            trimmedCurveConduit.CurveColor = pointConduitColor;
            trimmedCurveConduit.CurveThickness = 2;
            trimmedCurveConduit.DrawOnTop = true;

            while (true)
            {
                getPoints.ClearCommandOptions();
                var optionHeight = new OptionDouble(FlangeHeight, GuideFlangeParameters.MinHeight, GuideFlangeParameters.MaxHeight);
                var heightOptionIndex = getPoints.AddOptionDouble("FlangeHeight", ref optionHeight, $"Minimum: {GuideFlangeParameters.MinHeight}, Maximum: {GuideFlangeParameters.MaxHeight}");

                var getResult = getPoints.Get();
                if (getResult == GetResult.Option)
                {
                    if (getPoints.OptionIndex() == heightOptionIndex)
                    {
                        FlangeHeight = Math.Round(optionHeight.CurrentValue, 1, MidpointRounding.AwayFromZero);
                        optionHeight.CurrentValue = FlangeHeight;
                    }

                    continue;
                }
                else if (getResult == GetResult.Cancel)
                {
                    point1Conduit.Enabled = false;
                    point2Conduit.Enabled = false;
                    trimmedCurveConduit.Enabled = false;
                    foreach (var curveConduit in constraintCurveConduits)
                    {
                        curveConduit.Enabled = false;
                    }
                    IDSPICMFPlugIn.WriteLine(LogCategory.Error, "Guide Flange Input canceled.");
                    return EResult.Canceled;
                }
                else if (getResult == GetResult.Point)
                {
                    if (point1 == Point3d.Unset)
                    {
                        Curve pickedCurve = null;
                        Point3d pickedPoint;
                        if (constraintCurves.Count == 1)
                        {
                            pickedPoint = getPoints.Point();
                            pickedCurve = GetPickedCurve(constraintCurves, pickedPoint);
                        }
                        else
                        {
                            pickedPoint = GetPointOnCurve(constraintCurves, getPoints, double.MaxValue, out pickedCurve);
                        }

                        if (pickedCurve == null)
                        {
                            IDSPICMFPlugIn.WriteLine(LogCategory.Error, "Picked Point is invalid!");
                            continue;
                        }

                        point1 = pickedPoint;
                        point1Conduit.Center = point1;
                        point1Conduit.Enabled = true;
                        //filter constrain to only the selected curve
                        getPoints.ClearConstraints();
                        constraintCurves.Clear();
                        constraintCurves.Add(pickedCurve);
                        getPoints.Constrain(pickedCurve, false);

                        foreach (var conduit in constraintCurveConduits)
                        {
                            conduit.Enabled = false;
                        }
                        constraintCurveConduits.Clear();

                        var curveConduit = new CurveConduit();
                        curveConduit.CurveColor = IDS.CMF.Visualization.Colors.GuideFlangeGuidingOutline;
                        curveConduit.CurveThickness = 2;
                        curveConduit.CurvePreview = pickedCurve;
                        curveConduit.Enabled = true;
                        constraintCurveConduits.Add(curveConduit);
                    }
                    else if (point2 == Point3d.Unset)
                    {
                        var pickedPoint = getPoints.Point();
                        var pickedCurve = GetPickedCurve(constraintCurves, pickedPoint);

                        if (pickedCurve == null)
                        {
                            IDSPICMFPlugIn.WriteLine(LogCategory.Error, "Picked Point is invalid! Please check that the point is on the same guide flange guiding outline where picked point 1 is!");
                            continue;
                        }
                        else if (point1 == pickedPoint)
                        {
                            IDSPICMFPlugIn.WriteLine(LogCategory.Error, "Picked Point is identical!");
                            continue;
                        }

                        var trimmedCurve = TrimCurve(point1, pickedPoint, constraintCurves.First());
                        if (trimmedCurve == null)
                        {
                            IDSPICMFPlugIn.WriteLine(LogCategory.Error, "Unable to trim curve!"); 
                            continue;
                        }

                        point2 = pickedPoint;
                        point2Conduit.Center = point2;
                        point2Conduit.Enabled = true;

                        trimmedCurveConduit.CurvePreview = trimmedCurve;
                        trimmedCurveConduit.Enabled = true;

                        if (trimmedCurve.GetLength() <= GuideFlangeParameters.Rounding)
                        {
                            Dialogs.ShowMessage($"Please take note that your curve is lesser than or equals to {GuideFlangeParameters.Rounding}mm. This may cause failure in guide flange generation!",
                                "Warning", ShowMessageButton.OK, ShowMessageIcon.Exclamation);
                        }
                    }
                }
                else if (getResult == GetResult.Undo)
                {
                    if (point2 != Point3d.Unset)
                    {
                        //undo point2 selection
                        point2 = Point3d.Unset;
                        point2Conduit.Center = point2;
                        point2Conduit.Enabled = false;

                        trimmedCurveConduit.CurvePreview = null;
                        trimmedCurveConduit.Enabled = false;
                    }
                    else if (point1 != Point3d.Unset)
                    {
                        //undo point1 selection
                        point1 = Point3d.Unset;
                        point1Conduit.Center = point1;
                        point1Conduit.Enabled = false;

                        //add back constrains
                        getPoints.ClearConstraints();
                        constraintCurves.Clear();
                        constraintCurves = _intersectionCurves.ToList();
                        if (constraintCurves.Count == 1)
                        {
                            getPoints.Constrain(constraintCurves.First(), false);
                        }
                        else
                        {
                            getPoints.Constrain(OsteotomyParts, false);
                        }

                        foreach (var conduit in constraintCurveConduits)
                        {
                            conduit.Enabled = false;
                        }
                        constraintCurveConduits.Clear();

                        foreach (var curve in constraintCurves)
                        {
                            var curveConduit = new CurveConduit();
                            curveConduit.CurveColor = IDS.CMF.Visualization.Colors.GuideFlangeGuidingOutline;
                            curveConduit.CurveThickness = 2;
                            curveConduit.CurvePreview = curve;
                            curveConduit.Enabled = true;
                            constraintCurveConduits.Add(curveConduit);
                        }
                    }
                }
                else if (getResult == GetResult.Nothing)
                {
                    break;
                }
            }

            if (point1 == Point3d.Unset || point2 == Point3d.Unset)
            {
                point1Conduit.Enabled = false;
                point2Conduit.Enabled = false;
                trimmedCurveConduit.Enabled = false;
                foreach (var conduit in constraintCurveConduits)
                {
                    conduit.Enabled = false;
                }
                IDSPICMFPlugIn.WriteLine(LogCategory.Error, "Guide Flange Input failed.");
                return EResult.Failed;
            }

            FlangeCurve = TrimCurve(point1, point2, constraintCurves.First());

            if (FlangeCurve == null)  
            {
                point1Conduit.Enabled = false;
                point2Conduit.Enabled = false;
                trimmedCurveConduit.Enabled = false;
                foreach (var conduit in constraintCurveConduits)
                {
                    conduit.Enabled = false;
                }
                IDSPICMFPlugIn.WriteLine(LogCategory.Error, "Points picked could not generate valid curve! Please check that the points are on the same guide flange guiding outline!");
                return EResult.Failed;
            }

            point1Conduit.Enabled = false;
            point2Conduit.Enabled = false;
            trimmedCurveConduit.Enabled = false;
            foreach (var conduit in constraintCurveConduits)
            {
                conduit.Enabled = false;
            }
            _director.Document.Views.Redraw();
            return EResult.Success;
        }

        private List<FullSphereConduit> _editPointList;
        private FullSphereConduit _movePointConduit;
        private CurveConduit _trimmedCurveConduit;
        private int _nearestIndex;

        public EResult EditInputs(Curve flangeOutline, double flangeHeight, out Curve editedFlangeCurve, out double editedFlangeHeight)
        {
            editedFlangeCurve = null;
            editedFlangeHeight = flangeHeight;

            var pickedCurveA = GetPickedCurve(_intersectionCurves, flangeOutline.PointAtStart);
            var pickedCurveB = GetPickedCurve(_intersectionCurves, flangeOutline.PointAtEnd);
            if (pickedCurveA != pickedCurveB)
            {
                IDSPICMFPlugIn.WriteLine(LogCategory.Error, "Edit Guide Flange Input failed due to invalid curve.");
                return EResult.Failed;
            }

            FlangeHeight = flangeHeight;

            var editPoints = new GetPoint();
            editPoints.AcceptNothing(true);
            editPoints.SetCommandPrompt("Edit the 2 points on the guide flange guiding outline and <ENTER> to finalize");
            editPoints.AcceptUndo(false);

            var constrainCurve = pickedCurveA;
            editPoints.Constrain(constrainCurve, false);

            var point1 = flangeOutline.PointAtStart;
            var point2 = flangeOutline.PointAtEnd;
            var pointConduitDiameter = 1.0;
            var pointConduitTransparency = 0.0;
            var pointConduitColor = IDS.CMF.Visualization.Colors.GuideFlange;
            var point1Conduit = new FullSphereConduit(point1, pointConduitDiameter, pointConduitTransparency, pointConduitColor);
            var point2Conduit = new FullSphereConduit(point2, pointConduitDiameter, pointConduitTransparency, pointConduitColor);
            _trimmedCurveConduit = new CurveConduit();
            _trimmedCurveConduit.CurveColor = pointConduitColor;
            _trimmedCurveConduit.CurveThickness = 2;
            _trimmedCurveConduit.DrawOnTop = true;
            _trimmedCurveConduit.CurvePreview = flangeOutline;
            //constraint curve conduit

            point1Conduit.Enabled = true;
            point2Conduit.Enabled = true;
            _trimmedCurveConduit.Enabled = true;

            _editPointList = new List<FullSphereConduit>();
            _editPointList.Add(point1Conduit);
            _editPointList.Add(point2Conduit);
            _movePointConduit = new FullSphereConduit(Point3d.Unset, pointConduitDiameter, 0.25, Color.Purple);
            _nearestIndex = -1;

            editPoints.MouseDown += EditPoints_MouseDown;
            editPoints.MouseMove += EditPoints_MouseMove;

            while (true)
            {
                editPoints.ClearCommandOptions();
                var optionHeight = new OptionDouble(FlangeHeight, GuideFlangeParameters.MinHeight, GuideFlangeParameters.MaxHeight);
                var heightOptionIndex = editPoints.AddOptionDouble("FlangeHeight", ref optionHeight, $"Minimum: {GuideFlangeParameters.MinHeight}, Maximum: {GuideFlangeParameters.MaxHeight}");

                var getResult = editPoints.Get(true);
                if (getResult == GetResult.Option)
                {
                    if (editPoints.OptionIndex() == heightOptionIndex)
                    {
                        FlangeHeight = Math.Round(optionHeight.CurrentValue, 1, MidpointRounding.AwayFromZero);
                        optionHeight.CurrentValue = FlangeHeight;
                    }

                    continue;
                }
                else if (getResult == GetResult.Cancel)
                {
                    point1Conduit.Enabled = false;
                    point2Conduit.Enabled = false;
                    _trimmedCurveConduit.Enabled = false;
                    _movePointConduit.Enabled = false;
                    IDSPICMFPlugIn.WriteLine(LogCategory.Error, "Edit Guide Flange Input canceled.");
                    editPoints.MouseDown -= EditPoints_MouseDown;
                    editPoints.MouseMove -= EditPoints_MouseMove;
                    return EResult.Canceled;
                }
                else if (getResult == GetResult.Point)
                {
                    if (_nearestIndex > -1)
                    {
                        _editPointList[_nearestIndex].Center = _movePointConduit.Center;
                        _movePointConduit.Enabled = false;
                        var backupPoint = Point3d.Unset;

                        if (_nearestIndex == 0)
                        {
                            if (point2 == _movePointConduit.Center)
                            {
                                IDSPICMFPlugIn.WriteLine(LogCategory.Error, "Picked Point is identical!");
                                _nearestIndex = -1;
                                continue;
                            }
                            backupPoint = point1;
                            point1 = _movePointConduit.Center;
                        }
                        else if (_nearestIndex == 1)
                        {
                            if (point1 == _movePointConduit.Center)
                            {
                                IDSPICMFPlugIn.WriteLine(LogCategory.Error, "Picked Point is identical!");
                                _nearestIndex = -1;
                                continue;
                            }
                            backupPoint = point2;
                            point2 = _movePointConduit.Center;
                        }
                        _nearestIndex = -1;

                        var trimmedCurve = TrimCurve(point1, point2, constrainCurve);
                        if (trimmedCurve == null)
                        {
                            IDSPICMFPlugIn.WriteLine(LogCategory.Error, "Unable to trim curve!");
                            if (_nearestIndex == 0)
                            {
                                point1 = backupPoint;
                            }
                            else if (_nearestIndex == 1)
                            {
                                point2 = backupPoint;
                            }
                            _nearestIndex = -1;
                            continue;
                        }

                        _trimmedCurveConduit.CurvePreview = trimmedCurve;
                        _trimmedCurveConduit.Enabled = true;

                        if (trimmedCurve.GetLength() <= GuideFlangeParameters.Rounding)
                        {
                            Dialogs.ShowMessage($"Please take note that your curve is lesser than or equals to {GuideFlangeParameters.Rounding}mm. This may cause failure in guide flange generation!",
                                "Warning", ShowMessageButton.OK, ShowMessageIcon.Exclamation);
                        }
                    }
                }
                else if (getResult == GetResult.Nothing)
                {
                    break;
                }
            }

            if (point1 == Point3d.Unset || point2 == Point3d.Unset)
            {
                point1Conduit.Enabled = false;
                point2Conduit.Enabled = false;
                _trimmedCurveConduit.Enabled = false;
                IDSPICMFPlugIn.WriteLine(LogCategory.Error, "Edit Guide Flange Input failed.");
                editPoints.MouseDown -= EditPoints_MouseDown;
                editPoints.MouseMove -= EditPoints_MouseMove;
                return EResult.Failed;
            }

            editedFlangeCurve = TrimCurve(point1, point2, constrainCurve);
            editedFlangeHeight = FlangeHeight;

            if (editedFlangeCurve == null)
            {
                point1Conduit.Enabled = false;
                point2Conduit.Enabled = false;
                _trimmedCurveConduit.Enabled = false;
                IDSPICMFPlugIn.WriteLine(LogCategory.Error, "Points editted could not generate valid curve!");
                editPoints.MouseDown -= EditPoints_MouseDown;
                editPoints.MouseMove -= EditPoints_MouseMove;
                return EResult.Failed;
            }

            point1Conduit.Enabled = false;
            point2Conduit.Enabled = false;
            _trimmedCurveConduit.Enabled = false;
            editPoints.MouseDown -= EditPoints_MouseDown;
            editPoints.MouseMove -= EditPoints_MouseMove;
            _director.Document.Views.Redraw();
            return EResult.Success;
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

        private void EditPoints_MouseDown(object sender, GetPointMouseEventArgs e)
        {
            var point = e.Source.Point();
            var pointList = _editPointList.Select(x => x.Center).ToList();
            _nearestIndex = PickUtilities.GetClosestPickedPointIndex(point, pointList);

            if (_nearestIndex > -1)
            {
                _movePointConduit.Center = pointList[_nearestIndex];
                _movePointConduit.Enabled = true;

                _trimmedCurveConduit.CurvePreview = null;
                _trimmedCurveConduit.Enabled = false;
            }
        }

        private void EditPoints_MouseMove(object sender, GetPointMouseEventArgs e)
        {
            if (e.LeftButtonDown && _nearestIndex > -1)
            {
                //move
                var point = e.Source.Point();
                _movePointConduit.Center = point;
            }

            RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.SetCameraLocations(RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraTarget, RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraLocation);
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

        private Curve TrimCurve(Point3d pointA, Point3d pointB, Curve constraintCurve)
        {
            double pointAOnCurveParam;
            constraintCurve.ClosestPoint(pointA, out pointAOnCurveParam);

            double pointBOnCurveParam;
            constraintCurve.ClosestPoint(pointB, out pointBOnCurveParam);

            if (pointAOnCurveParam == pointBOnCurveParam)
            {
                return null;
            }

            var curve1 = constraintCurve.Trim(pointAOnCurveParam, pointBOnCurveParam);
            var curve2 = constraintCurve.Trim(pointBOnCurveParam, pointAOnCurveParam);
            return curve1.GetLength() < curve2.GetLength() ? curve1 : curve2;
        }
    }
}
