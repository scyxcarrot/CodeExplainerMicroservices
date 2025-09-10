using IDS.CMF.DataModel;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.Plugin;
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
    public class DrawLimitSurfaceMode : IDrawSurfaceState
    {
        private const int DegreeOfCurve = 1;
        private Curve _currentCurve;
        private Brep _currentCurveTube;
        private readonly double _tubeDiameter;
        private double _extensionLength = 10;
        private bool _isCurveClosed = false; // restrict user only draw 1 patch.
        private Brep _limitingSurface;

        protected Color FeedbackTubeColor = Color.LightYellow;
        protected Color ClosingPointColor = Color.DarkOrchid;
        protected Color PointsColor = Color.Yellow;
        protected Color MeshWireColor = Color.Green;
        private readonly Color _previewPointColor = Color.Coral;

        private readonly DrawSurfaceDataContext _dataContext;
        private readonly DrawSurfaceUndoData _undoData;
        private readonly List<IUndoableSurfaceAction> _undoList;
        private readonly List<Point3d> _currentPatchCurvePoints = new List<Point3d>();

        public DrawLimitSurfaceMode(ref DrawSurfaceDataContext dataContext)
        {
            _dataContext = dataContext;
            _tubeDiameter = _dataContext.PatchTubeDiameter;
            _currentCurve = new PolyCurve();
            _currentCurveTube = new Brep();
            _undoData = new DrawSurfaceUndoData
            {
                CurrentPointList = _currentPatchCurvePoints
            };
            _undoList = new List<IUndoableSurfaceAction>();
        }

        private void CreateBrepFromPoints()
        {
            // Create a closed curve from the points
            var console = new IDSRhinoConsole();
            var controlPoints = _dataContext.PositivePatchTubes.SelectMany(p => p.Value.ControlPoints).ToList();
            if (controlPoints.Count > 0)
            {
                var framePoints = TeethSupportedGuideUtilities.ProcessPoints(console, controlPoints, _extensionLength);
                var brepOuter = BrepUtilities.CreatePatchFromPoints(framePoints);
                _limitingSurface = brepOuter;
            }
            else
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, "Please finish the current draw patch first!");
            }

        }

        public void OnKeyboard(int key, DrawSurface drawSurface)
        {
            switch (key)
            {
                case (187): //+
                case (107): //+ numpad
                    _extensionLength += 1.0;
                    _dataContext.ExtensionLength = _extensionLength;
                    CreateBrepFromPoints();
                    IDSPluginHelper.WriteLine(LogCategory.Default, $"Extended Length= {_extensionLength}");
                    RhinoDoc.ActiveDoc.Views.Redraw();
                    break;
                case (189): //-
                case (109): //- numpad
                    if (_extensionLength - 1.0 >= 10)
                    {
                        _extensionLength -= 1.0;
                        _dataContext.ExtensionLength = _extensionLength;
                        CreateBrepFromPoints();
                        IDSPluginHelper.WriteLine(LogCategory.Default, $"Extended Length= {_extensionLength}");
                    }
                    else
                    {
                        IDSPluginHelper.WriteLine(LogCategory.Default, "Length has to be bigger than 10mm");
                    }
                    IDSPluginHelper.WriteLine(LogCategory.Default, $"Current Length: {_dataContext.ExtensionLength}mm");
                    RhinoDoc.ActiveDoc.Views.Redraw();
                    break;
            }
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

        private void HandleCloseCurve()
        {
            var brepInner = BrepUtilities.CreatePatchFromPoints(_currentPatchCurvePoints);
            var innerSurface = MeshUtilities.ConvertBrepToMesh(brepInner);
            var innerPatchSurface = new PatchSurface
            {
                ControlPoints = _currentPatchCurvePoints.ToList(),
                Diameter = _tubeDiameter,
                IsNegative = false,
            };
            _dataContext.PositivePatchTubes.Add(new KeyValuePair<Mesh, PatchSurface>(innerSurface, innerPatchSurface));
            _currentPatchCurvePoints.Clear();
            _currentCurve = new PolyCurve();
            _currentCurveTube = new Brep();
            _undoList.Clear();
            _isCurveClosed = true;
            CreateBrepFromPoints();
        }

        private bool HandleUndoableAddControlPoint(Point3d point)
        {
            var action = new AddControlPointSurfaceAction();
            action.PointToAdd = point;
            var handled = action.Do(_undoData);
            AddToUndoList(action);
            return handled;
        }

        private void AddToUndoList(IUndoableSurfaceAction action)
        {
            if (_undoList.Count > 2)
            {
                _undoList.RemoveAt(0);
            }
            _undoList.Add(action);
        }

        private void RebuildChanges(Mesh constraintMesh)
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

        public bool OnGetPoint(Point3d point, Mesh constraintMesh, GetCurvePoints drawCurvePointsDerivation)
        {
            if (_isCurveClosed)
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "Only can draw 1 patch!");
                return false;
            }

            if (IsCurveClosing(point))
            {
                HandleCloseCurve();
                return true;
            }

            HandleUndoableAddControlPoint(point);

            RebuildChanges(constraintMesh);

            return true;
        }

        public void OnDynamicDraw(GetPointDrawEventArgs e, GetCurvePoints drawCurvePointsDerivation)
        {
            var sphereColor = FeedbackTubeColor;
            if (IsCurveClosing(e.CurrentPoint))
            {
                var feedbackCurvePoints = new List<Point3d>() { _currentPatchCurvePoints.LastOrDefault(), e.CurrentPoint };
                e.Display.DrawPoint(_currentPatchCurvePoints.FirstOrDefault(), ClosingPointColor);
                e.Display.DrawBrepWires(GuideSurfaceUtilities.CreatePipeBrep(feedbackCurvePoints, _tubeDiameter / 2), ClosingPointColor);
                sphereColor = ClosingPointColor;
            }
            else
            {
                if (_currentPatchCurvePoints.Any())
                {
                    var feedbackCurvePoints = new List<Point3d>() { _currentPatchCurvePoints.LastOrDefault(), e.CurrentPoint };
                    e.Display.DrawBrepWires(GuideSurfaceUtilities.CreatePipeBrep(feedbackCurvePoints, _tubeDiameter / 2), FeedbackTubeColor);
                    e.Display.DrawPoints(_currentPatchCurvePoints, PointStyle.Circle, 5, PointsColor);
                }
            }

            var spherePreview = new Sphere(e.CurrentPoint, _tubeDiameter / 2);
            e.Display.DrawSphere(spherePreview, sphereColor);
            e.Display.DrawPoint(e.CurrentPoint, PointStyle.Circle, 5, MeshWireColor);
            if (_limitingSurface != null)
            {
                e.Display.DrawBrepShaded(_limitingSurface, new DisplayMaterial
                {
                    Transparency = 0.5,
                    Diffuse = _previewPointColor,
                    Specular = _previewPointColor,
                    Emission = _previewPointColor
                });
            }
        }

        public void OnPostDrawObjects(DrawEventArgs e, GetCurvePoints drawCurvePointsDerivation)
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
            isContinueLooping = true;
            if (_currentPatchCurvePoints.Count > 2)
            {
                HandleCloseCurve();
            }
            else
            {
                isContinueLooping = false;
            }

            return true;
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
    }
}
