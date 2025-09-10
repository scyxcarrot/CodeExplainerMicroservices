using IDS.CMF.DataModel;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Input.Custom;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace IDS.PICMF.Drawing
{
    public class DrawRegionSkeletonMode : IDrawSurfaceState
    {
        private readonly DrawSurfaceDataContext _dataContext;
        private const int CurveDegree = 1;
        private readonly Color _pointColor = Color.Crimson;
        private readonly Color _curveColor = Color.Blue;
        private readonly Color _tubeColor = Color.Yellow;

        private readonly List<Point3d> _allSkeletonCurvePoints = new List<Point3d>();
        private readonly List<Polyline> _allSkeletonCurve = new List<Polyline>();
        private readonly List<Point3d> _currentSkeletonCurvePoints = new List<Point3d>();
        private readonly List<Brep> _allSkeletonTube = new List<Brep>();
        private Brep _currentCurveTube;

        private readonly DrawSurfaceUndoData _undoData;
        private readonly List<IUndoableSurfaceAction> _undoList;

        public DrawRegionSkeletonMode(ref DrawSurfaceDataContext dataContext)
        {
            _dataContext = dataContext;
            _currentCurveTube = new Brep();
            _undoData = new DrawSurfaceUndoData
            {
                CurrentPointList = _currentSkeletonCurvePoints,
                AllPoints = _allSkeletonCurvePoints,
                SelectedIndex = -1
            };
            _undoList = new List<IUndoableSurfaceAction>();
        }

        public void OnKeyboard(int key, DrawSurface drawSurface)
        {
            bool dummy;
            switch (key)
            {
                case (76): // L
                    OnFinalize(drawSurface.ConstraintMesh, out dummy);
                    break;
                case (80): // P

                    if (_currentSkeletonCurvePoints.Any())
                    {
                        IDSPluginHelper.WriteLine(LogCategory.Warning, "Please complete the skeleton drawing first.");
                        break;
                    }

                    if (OnFinalize(drawSurface.ConstraintMesh, out dummy))
                    {
                        ((DrawRegion) drawSurface).SetToPatchDrawing();
                    }
                    else
                    {
                        IDSPluginHelper.WriteLine(LogCategory.Warning, "Please provide a second point first.");
                    }
                    break;
                case (187): //+
                case (107): //+ numpad
                    _dataContext.SkeletonTubeDiameter += _dataContext.DrawStepSize;
                    RebuildChanges(true);

                    IDSPluginHelper.WriteLine(LogCategory.Default, $"Diameter= {_dataContext.SkeletonTubeDiameter}");
                    RhinoDoc.ActiveDoc.Views.Redraw();
                    break;
                case (189): //-
                case (109): //- numpad

                    if (_dataContext.SkeletonTubeDiameter - _dataContext.DrawStepSize > 0.0)
                    {
                        _dataContext.SkeletonTubeDiameter -= _dataContext.DrawStepSize;
                        RebuildChanges(true);
                    }
                    else
                    {
                        IDSPluginHelper.WriteLine(LogCategory.Warning, "Diameter has to be bigger than 0.0");
                    }

                    IDSPluginHelper.WriteLine(LogCategory.Default, $"Diameter= {_dataContext.SkeletonTubeDiameter}");
                    RhinoDoc.ActiveDoc.Views.Redraw();
                    break;
                default:
                    return; // nothing to do
            }
        }

        public bool OnGetPoint(Point3d point, Mesh constraintMesh, GetCurvePoints drawCurvePointsDerivation)
        {
            if (Control.ModifierKeys == Keys.Alt)
            {
                var point2d = drawCurvePointsDerivation.Point2d();
                var nearestIndex = PickUtilities.GetPickedPoint3dIndexFromPoint2d(point2d, _allSkeletonCurvePoints);

                if (nearestIndex != -1)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Default, "Draw point base has changed!");
                    if (_currentSkeletonCurvePoints.Any())
                    {
                        if (_currentSkeletonCurvePoints.Count >= 2)
                        {
                            _allSkeletonTube.Add(CreateCurrentTubeBrep());
                            _allSkeletonCurve.Add(new Polyline(_currentSkeletonCurvePoints));
                        }
                        _currentSkeletonCurvePoints.Clear();
                        _undoList.Clear();
                    }
                    _undoData.SelectedIndex = nearestIndex;
                    return true;
                }
                else
                {
                    IDSPluginHelper.WriteLine(LogCategory.Default, "No drawing point on selected location, please click near to any existing points!");
                    return false;
                }
            }

            if (_undoData.SelectedIndex != -1 && !_currentSkeletonCurvePoints.Any())
            {
                _currentSkeletonCurvePoints.Add(_allSkeletonCurvePoints[_undoData.SelectedIndex]);
            }

            HandleUndoableAddControlPoint(point);

            RebuildChanges();

            return true;
        }

        public void OnDynamicDraw(GetPointDrawEventArgs e, GetCurvePoints drawCurvePointsDerivation)
        {
            var spherePreview = new Sphere(e.CurrentPoint, _dataContext.SkeletonTubeDiameter / 2);

            if (_undoData.SelectedIndex != -1 && Control.ModifierKeys != Keys.Alt)
            {
                e.Display.DrawLine(_allSkeletonCurvePoints[_undoData.SelectedIndex], e.CurrentPoint, _curveColor);

                var curvePoints = new List<Point3d>() { _allSkeletonCurvePoints[_undoData.SelectedIndex], e.CurrentPoint };
                e.Display.DrawBrepWires(GuideSurfaceUtilities.CreatePipeBrep(curvePoints, _dataContext.SkeletonTubeDiameter / 2), _tubeColor);
            }

            if (Control.ModifierKeys == Keys.Alt)
            {
                var closestPt = PointUtilities.FindClosestPoint(e.CurrentPoint, _allSkeletonCurvePoints);

                if (closestPt == Point3d.Unset || closestPt.DistanceTo(e.CurrentPoint) > 1.0)
                {
                    return;
                }

                e.Display.DepthMode = DepthMode.AlwaysInFront;
                e.Display.DrawPoint(closestPt, PointStyle.RoundActivePoint, 10, Color.Purple);
                e.Display.DepthMode = DepthMode.Neutral;
            }
            else
            {
                e.Display.DrawSphere(spherePreview, Color.Magenta);
            }
        }

        public void OnPostDrawObjects(DrawEventArgs e, GetCurvePoints drawCurvePointsDerivation)
        {
            _allSkeletonCurvePoints.ForEach(p =>
            {
                e.Display.DepthMode = DepthMode.AlwaysInFront;
                e.Display.DrawPoint(p, PointStyle.ControlPoint, 5, _pointColor);
            });

            e.Display.DepthMode = DepthMode.Neutral;

            foreach (var line in _allSkeletonCurve)
            {
                e.Display.DrawPolyline(line, _curveColor);
            }

            var material = new DisplayMaterial
            {
                Transparency = 0.5,
                Diffuse = _tubeColor,
                Specular = _tubeColor,
                Emission = _tubeColor
            };

            foreach (var tube in _allSkeletonTube)
            {
                e.Display.DrawBrepShaded(tube, material);
            }

            if (_currentSkeletonCurvePoints.Count >= 2)
            {
                e.Display.DrawPolyline(_currentSkeletonCurvePoints, _curveColor);

                e.Display.DrawBrepShaded(_currentCurveTube, material);
            }

        }

        public void OnMouseMove(GetPointMouseEventArgs e, GetCurvePoints drawCurvePointsDerivation)
        {

        }

        public void OnMouseLeave(RhinoView view)
        {

        }

        public void OnMouseEnter(RhinoView view)
        {

        }

        public bool OnFinalize(Mesh constraintMesh, out bool isContinueLooping)
        {
            isContinueLooping = false;

            if (_currentSkeletonCurvePoints.Any() && _currentSkeletonCurvePoints.Count < 2)
            {
                return false;
            }

            _undoData.SelectedIndex = -1;

            if (_currentSkeletonCurvePoints.Any())
            {
                _allSkeletonCurve.Add(new Polyline(_currentSkeletonCurvePoints));
                _currentSkeletonCurvePoints.Clear();
            }

            if (_allSkeletonCurve.Any())
            {
                var curves = new List<Curve>();
                var controlPoints = new List<List<Point3d>>();
                _allSkeletonCurve.ForEach(line => curves.Add(CurveUtilities.BuildCurve(line.ToList(), CurveDegree, false)));
                _allSkeletonCurve.ForEach(line => controlPoints.Add(line.ToList()));

                var GuideSurfaceData = new SkeletonSurface
                {
                    ControlPoints = controlPoints,
                    Diameter = _dataContext.SkeletonTubeDiameter,
                    IsNegative = false
                };

                var allTube = new Brep();
                foreach (var tube in _allSkeletonTube)
                {
                    allTube.Append(tube);
                }
                allTube.Append(_currentCurveTube);
                _dataContext.SkeletonTubes.Add(new KeyValuePair<Brep, SkeletonSurface>(allTube, GuideSurfaceData));
                _dataContext.SkeletonCurves.Add(new KeyValuePair<List<Curve>, SkeletonSurface>(curves, GuideSurfaceData));

                _allSkeletonCurvePoints.Clear();
                _allSkeletonCurve.Clear();
                _allSkeletonTube.Clear();
                _currentCurveTube = new Brep();


                isContinueLooping = true;
            }

            _undoList.Clear();

            return true;
        }

        private Brep CreateCurrentTubeBrep()
        {
            return GuideSurfaceUtilities.CreatePipeBrep(_currentSkeletonCurvePoints.ToList(), _dataContext.SkeletonTubeDiameter / 2);
        }

        private void ReviseAllHistoricalTubeBrep()
        {
            var controlPoints = new List<List<Point3d>>();
            _allSkeletonCurve.ForEach(line => controlPoints.Add(line.ToList()));
            _allSkeletonTube.Clear();
            foreach (var controlPoint in controlPoints)
            {
                _allSkeletonTube.Add(GuideSurfaceUtilities.CreatePipeBrep(controlPoint, _dataContext.SkeletonTubeDiameter / 2));
            }
        }

        public void RebuildChanges(bool reviseAllSkeleton = false)
        {
            if (reviseAllSkeleton)
            {
                ReviseAllHistoricalTubeBrep();
            }

            if (_currentSkeletonCurvePoints.Count < 2)
            {
                _currentCurveTube = new Brep();
                return;
            }

            _currentCurveTube = CreateCurrentTubeBrep();
        }

        public void OnUndo(Mesh constraintMesh)
        {
            if (_undoList.Count > 0)
            {
                var action = _undoList[_undoList.Count - 1];
                action.Undo(_undoData);
                _undoList.Remove(action);

                RebuildChanges();
            }
        }

        private void AddToUndoList(IUndoableSurfaceAction action)
        {
            if (_undoList.Count > 2)
            {
                _undoList.RemoveAt(0);
            }
            _undoList.Add(action);
        }

        private void HandleUndoableAddControlPoint(Point3d point)
        {
            var action = new AddControlPointSkeletonSurfaceAction();
            action.PointToAdd = point;
            action.Do(_undoData);
            AddToUndoList(action);
        }
    }
}
