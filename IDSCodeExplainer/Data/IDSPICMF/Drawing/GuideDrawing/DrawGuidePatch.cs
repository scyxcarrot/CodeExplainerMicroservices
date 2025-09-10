using IDS.CMF.DataModel;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.PICMF.DrawingAction;
using Rhino;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Input.Custom;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CurvePoints = System.Collections.Generic.List<Rhino.Geometry.Point3d>;

namespace IDS.PICMF.Drawing
{
    public abstract class DrawGuidePatch : IDrawGuideState
    {
        protected DrawGuideDataContext _dataContext;

        private const int DegreeOfCurve = 1;
        private Curve _currentCurve;
        private readonly CurvePoints _currentPatchCurvePoints = new CurvePoints();
        private Brep _currentCurveTube;

        private double _tubeDiameter;
        protected bool _isNegative;

        protected Color feedbackTubeColor = Color.LightYellow;
        protected Color closingPointColor = Color.White;
        protected Color pointsColor = Color.Yellow;
        protected Color curveColor = Color.Magenta;
        protected Color meshWireColor = Color.Green;

        private readonly DrawGuideUndoData _undoData;
        private readonly List<IUndoableGuideAction> _undoList;

        protected DrawGuidePatch(List<PatchData> patchSurfaces, ref double tubeDiameter)
        {
            _tubeDiameter = tubeDiameter;
            _currentCurve = new PolyCurve();
            _currentCurveTube = new Brep();
            _undoData = new DrawGuideUndoData
            {
                CurrentPointList = _currentPatchCurvePoints
            };
            _undoList = new List<IUndoableGuideAction>();
        }

        public virtual void OnKeyboard(int key, DrawGuide drawGuide)
        {
            switch (key)
            {
                case (187): //+
                case (107): //+ numpad
                    _tubeDiameter += _dataContext.DrawStepSize;
                    IDSPluginHelper.WriteLine(LogCategory.Default, $"Diameter= {_tubeDiameter}");
                    RhinoDoc.ActiveDoc.Views.Redraw();
                    break;
                case (189): //-
                case (109): //- numpad
                    if (_tubeDiameter - _dataContext.DrawStepSize > 0.0)
                    {
                        _tubeDiameter -= _dataContext.DrawStepSize;
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
                IsNegative = _isNegative
            };

            var brepSurface = BrepUtilities.CreatePatchOnMeshFromClosedCurve(_currentPatchCurvePoints, constraintMesh);

            if (_isNegative)
            {
                _dataContext.NegativePatchSurface.Add(new KeyValuePair<Brep, PatchSurface>(brepSurface, GuideSurfaceData));
                _dataContext.NegativePatchTubes.Add(new KeyValuePair<Mesh, PatchSurface>(tube, GuideSurfaceData));
            }
            else
            {
                _dataContext.PositivePatchSurface.Add(new KeyValuePair<Brep, PatchSurface>(brepSurface, GuideSurfaceData));
                _dataContext.PositivePatchTubes.Add(new KeyValuePair<Mesh, PatchSurface>(tube, GuideSurfaceData));
            }

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
            return _currentPatchCurvePoints.FirstOrDefault().DistanceTo(point) <= closingTolerance && _currentPatchCurvePoints.Count >= 3;
        }

        public virtual void OnDynamicDraw(GetPointDrawEventArgs e, GetCurvePoints drawCurvePointsDerivation)
        {
            var sphereColor = feedbackTubeColor;
            //User Feedback
            if (IsCurveClosing(e.CurrentPoint))
            {
                e.Display.DrawPoint(_currentPatchCurvePoints.FirstOrDefault(), closingPointColor);
                var feedbackCurvePoints = new List<Point3d>() { _currentPatchCurvePoints.LastOrDefault(), e.CurrentPoint };
                e.Display.DrawBrepWires(GuideSurfaceUtilities.CreatePipeBrep(feedbackCurvePoints, _tubeDiameter / 2), closingPointColor);
                sphereColor = closingPointColor;
            }
            else
            {
                if (_currentPatchCurvePoints.Any())
                {
                    var feedbackCurvePoints = new List<Point3d>() { _currentPatchCurvePoints.LastOrDefault(), e.CurrentPoint };                
                    e.Display.DrawBrepWires(GuideSurfaceUtilities.CreatePipeBrep(feedbackCurvePoints, _tubeDiameter / 2), feedbackTubeColor);
                }
            }

            var spherePreview = new Sphere(e.CurrentPoint, _tubeDiameter / 2);
            e.Display.DrawSphere(spherePreview, sphereColor);
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
                Diffuse = meshWireColor,
                Specular = meshWireColor,
                Emission = meshWireColor
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
                var action = _undoList.Last();
                action.Undo(_undoData);
                _undoList.Remove(action);

                RebuildChanges(constraintMesh);
            }
        }

        private void AddToUndoList(IUndoableGuideAction action)
        {
            if (_undoList.Count > 2)
            {
                _undoList.RemoveAt(0);
            }
            _undoList.Add(action);
        }

        private bool HandleUndoableAddControlPoint(Point3d point)
        {
            var action = new AddControlPointGuideAction();
            action.PointToAdd = point;
            var handled = action.Do(_undoData);
            AddToUndoList(action);
            return handled;
        }
    }
}
