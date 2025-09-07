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

namespace IDS.PICMF.Drawing
{
    public class DrawRegionPatchMode : IDrawSurfaceState
    {
        protected DrawSurfaceDataContext DataContext;

        private const int DegreeOfCurve = 1;
        private Curve _currentCurve;
        private readonly List<Point3d> _currentPatchCurvePoints = new List<Point3d>();
        private Brep _currentCurveTube;

        private double _tubeDiameter;

        protected Color FeedbackTubeColor = Color.Red;
        protected Color ClosingPointColor = Color.White;
        protected Color PointsColor = Color.Yellow;
        protected Color MeshWireColor = Color.Red;

        private readonly DrawSurfaceUndoData _undoData;
        private readonly List<IUndoableSurfaceAction> _undoList;

        public DrawRegionPatchMode(ref DrawSurfaceDataContext dataContext)
        {
            DataContext = dataContext;
            _tubeDiameter = dataContext.PatchTubeDiameter;
            _currentCurve = new PolyCurve();
            _currentCurveTube = new Brep();
            _undoData = new DrawSurfaceUndoData
            {
                CurrentPointList = _currentPatchCurvePoints
            };
            _undoList = new List<IUndoableSurfaceAction>();
        }

        public virtual void OnKeyboard(int key, DrawSurface drawSurface)
        {
            switch (key)
            {
                case (80): // P
                    if (OnFinalize(drawSurface.ConstraintMesh, out _))
                    {
                        ((DrawRegion)drawSurface).SetToSkeletonDrawing();
                    }
                    else
                    {
                        IDSPluginHelper.WriteLine(LogCategory.Warning, "Please close the patch first.");
                    }
                    break;
                case (187): //+
                case (107): //+ numpad
                    _tubeDiameter += DataContext.DrawStepSize;
                    IDSPluginHelper.WriteLine(LogCategory.Default, $"Diameter= {_tubeDiameter}");
                    RhinoDoc.ActiveDoc.Views.Redraw();
                    break;
                case (189): //-
                case (109): //- numpad
                    if (_tubeDiameter - DataContext.DrawStepSize > 0.0)
                    {
                        _tubeDiameter -= DataContext.DrawStepSize;
                    }
                    else
                    {
                        IDSPluginHelper.WriteLine(LogCategory.Warning, "Diameter has to be bigger than 0.0");
                    }

                    IDSPluginHelper.WriteLine(LogCategory.Default, $"Diameter= {_tubeDiameter}");
                    RhinoDoc.ActiveDoc.Views.Redraw();
                    break;
            }
        }

        public virtual bool OnGetPoint(Point3d point, Mesh constraintMesh, GetCurvePoints drawCurvePointsDerivation)
        {
            if (IsCurveClosing(point))
            {
                HandleCloseCurve(constraintMesh);
                return true;
            }

            HandleUndoableAddControlPoint(point);

            RebuildChanges(constraintMesh);

            return true;
        }

        private void HandleCloseCurve(Mesh constraintMesh)
        {
            var closedCurve = CurveUtilities.BuildCurve(_currentPatchCurvePoints, DegreeOfCurve, true);
            var pulledCurve = closedCurve.PullToMesh(constraintMesh, 0.1);
            var tube = GuideSurfaceUtilities.CreateCurveTube(pulledCurve, _tubeDiameter / 2);

            var GuideSurfaceData = new PatchSurface
            {
                ControlPoints = _currentPatchCurvePoints.ToList(),
                Diameter = _tubeDiameter,
                IsNegative = false,
            };

            var brepSurface = BrepUtilities.CreatePatchOnMeshFromClosedCurve(_currentPatchCurvePoints, constraintMesh);

            DataContext.PositivePatchSurface.Add(new KeyValuePair<Brep, PatchSurface>(brepSurface, GuideSurfaceData));
            DataContext.PositivePatchTubes.Add(new KeyValuePair<Mesh, PatchSurface>(tube, GuideSurfaceData));

            _currentPatchCurvePoints.Clear();
            _currentCurve = new PolyCurve();
            _currentCurveTube = new Brep();
            _undoList.Clear();
        }

        private bool IsCurveClosing(Point3d point)
        {
            if (_currentPatchCurvePoints.Count < 3)
            {
                return false;
            }

            var closingTolerance = 1.0;
            return _currentPatchCurvePoints[0].DistanceTo(point) <= closingTolerance && _currentPatchCurvePoints.Count >= 3;
        }

        public virtual void OnDynamicDraw(GetPointDrawEventArgs e, GetCurvePoints drawCurvePointsDerivation)
        {
            var sphereColor = FeedbackTubeColor;
            //User Feedback
            if (IsCurveClosing(e.CurrentPoint))
            {
                e.Display.DrawPoint(_currentPatchCurvePoints.FirstOrDefault(), ClosingPointColor);
                var feedbackCurvePoints = new List<Point3d>() { _currentPatchCurvePoints.LastOrDefault(), e.CurrentPoint };
                e.Display.DrawBrepWires(GuideSurfaceUtilities.CreatePipeBrep(feedbackCurvePoints, _tubeDiameter / 2), ClosingPointColor);
                sphereColor = ClosingPointColor;
            }
            else
            {
                if (_currentPatchCurvePoints.Any())
                {
                    var feedbackCurvePoints = new List<Point3d>() { _currentPatchCurvePoints.LastOrDefault(), e.CurrentPoint };
                    e.Display.DrawBrepWires(GuideSurfaceUtilities.CreatePipeBrep(feedbackCurvePoints, _tubeDiameter / 2), FeedbackTubeColor);
                }
            }

            var spherePreview = new Sphere(e.CurrentPoint, _tubeDiameter / 2);
            e.Display.DrawSphere(spherePreview, sphereColor);
            e.Display.DrawPoint(e.CurrentPoint, PointStyle.Circle, 5, MeshWireColor);
        }

        public virtual void OnPostDrawObjects(DrawEventArgs e, GetCurvePoints drawCurvePointsDerivation)
        {
            //Current Drawing.
            if (_currentCurve != null)
            {
                _currentPatchCurvePoints.ForEach(p =>
                {
                    e.Display.DrawPoint(p, Color.Yellow);
                });
                e.Display.DrawCurve(_currentCurve, Color.Magenta);
            }
            e.Display.DrawBrepShaded(_currentCurveTube, new DisplayMaterial
            {
                Transparency = 0.5,
                Diffuse = MeshWireColor,
                Specular = MeshWireColor,
                Emission = MeshWireColor
            });
        }

        public virtual void OnMouseMove(GetPointMouseEventArgs e, GetCurvePoints drawCurvePointsDerivation)
        {

        }

        public virtual void OnMouseLeave(RhinoView view)
        {

        }

        public virtual void OnMouseEnter(RhinoView view)
        {

        }

        public virtual bool OnFinalize(Mesh constraintMesh, out bool isContinueLooping)
        {
            isContinueLooping = true;
            if (_currentPatchCurvePoints.Count > 2)
            {
                HandleCloseCurve(constraintMesh);
            }
            else
            {
                isContinueLooping = false;
            }

            return true;
        }

        public void RebuildChanges(Mesh constraintMesh)
        {
            if (_currentPatchCurvePoints.Count < 2)
            {
                _currentCurve = new PolyCurve();
                _currentCurveTube = new Brep();
                return;
            }

            var currentCurve = CurveUtilities.BuildCurve(_currentPatchCurvePoints, DegreeOfCurve, false);
            _currentCurve = currentCurve.PullToMesh(constraintMesh, 0.1);
            _currentCurveTube = GuideSurfaceUtilities.CreatePipeBrep(_currentPatchCurvePoints, _tubeDiameter / 2);
        }

        public void OnUndo(Mesh constraintMesh)
        {
            if (_undoList.Count > 0)
            {
                var action = _undoList[_undoList.Count - 1];
                action.Undo(_undoData);
                _undoList.Remove(action);

                RebuildChanges(constraintMesh);
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
            var action = new AddControlPointSurfaceAction();
            action.PointToAdd = point;
            action.Do(_undoData);
            AddToUndoList(action);
        }
    }
}
