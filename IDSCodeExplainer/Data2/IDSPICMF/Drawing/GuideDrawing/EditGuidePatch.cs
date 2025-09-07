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
    public abstract class EditGuidePatch : IEditGuideState
    {
        private const int DegreeOfCurve = 1;
        private Curve _currentCurve;
        private List<Point3d> _currentPatchCurvePoints;
        private Mesh _currentCurveTube;
        private int _selectedIndex;

        private readonly List<PatchData> _patchSurfaces;
        private readonly PatchSurface _patchSurface;
        private double _tubeDiameter;
        private bool _isMoving;
        private bool _needRegenerate;
        protected bool _isNegative;

        protected Color feedbackTubeColor = Color.LightYellow;
        protected Color meshWireColor = Color.Green;

        protected EditGuidePatch(List<PatchData> patchSurfaces, ref double tubeDiameter, PatchSurface patchSurface)
        {
            _selectedIndex = -1;
            _patchSurfaces = patchSurfaces;
            _patchSurface = patchSurface;
            _tubeDiameter = tubeDiameter;
            _isMoving = false;
            _needRegenerate = false;
            _currentCurve = new PolyCurve();
            _currentCurveTube = new Mesh();
        }

        public void OnExecute(EditGuide editGuide)
        {
            _tubeDiameter = _patchSurface.Diameter;

            _currentPatchCurvePoints = _patchSurface.ControlPoints.ToList();
            if (_currentPatchCurvePoints.First().Equals(_currentPatchCurvePoints.Last()))
            {
                _currentPatchCurvePoints.RemoveAt(_currentPatchCurvePoints.Count - 1);
            }

            SetCurrentEntities(editGuide);
        }

        public virtual void OnKeyboard(int key, EditGuide editGuide)
        {
            switch (key)
            {
                case (187): //+
                case (107): //+ numpad
                    _tubeDiameter += 0.5;
                    OnDiameterChanged(editGuide);
                    break;
                case (189): //-
                case (109): //- numpad
                    if (_tubeDiameter - 0.5 > 0.0)
                    {
                        _tubeDiameter -= 0.5;
                        OnDiameterChanged(editGuide);
                    }
                    else
                    {
                        IDSPluginHelper.WriteLine(LogCategory.Warning, "Diameter has to be bigger than 0.0");
                    }
                    break;
            }
        }

        public virtual bool OnGetPoint(Point3d point, EditGuide editGuide)
        {
            var point2d = editGuide.Point2d();
            _selectedIndex = PickUtilities.GetPickedPoint3dIndexFromPoint2d(point2d, _currentPatchCurvePoints);

            var meshPoint = editGuide.LowLoDConstraintMesh.ClosestMeshPoint(point, 1.0).Point;
            var distTolerance = 10.0;
            if (_selectedIndex == -1 && Control.ModifierKeys == Keys.Shift)
            {
                double closestPtParam;
                _currentCurve.ClosestPoint(meshPoint, out closestPtParam);
                var closestPt = _currentCurve.PointAt(closestPtParam);

                var dist = closestPt.DistanceTo(meshPoint);
                if (dist > distTolerance)
                {
                    return true;
                }

                var dictTmpPts = new SortedList<double, Point3d>();
                foreach (var currentPatchCurvePoint in _currentPatchCurvePoints)
                {
                    double currentPatchCurvePointParam;
                    _currentCurve.ClosestPoint(currentPatchCurvePoint, out currentPatchCurvePointParam);
                    dictTmpPts.Add(currentPatchCurvePointParam, currentPatchCurvePoint);
                }

                double newPointParamOnCurve;
                _currentCurve.ClosestPoint(closestPt, out newPointParamOnCurve);

                dictTmpPts.Add(newPointParamOnCurve, closestPt);

                _currentPatchCurvePoints = new List<Point3d>(dictTmpPts.Select(x => x.Value));
                SetCurrentEntities(editGuide);
            }
            else if (_selectedIndex != -1 && Control.ModifierKeys == Keys.Shift)
            {

                _currentPatchCurvePoints.RemoveAt(_selectedIndex);
                SetCurrentEntities(editGuide);
            }

            return true;
        }

        public virtual void OnDynamicDraw(GetPointDrawEventArgs e, EditGuide editGuide)
        {
            if (_isMoving && _selectedIndex >= 0)
            {
                var feedbackCurvePoints = new List<Point3d>() { e.CurrentPoint };
                if (_selectedIndex > 0)
                {
                    feedbackCurvePoints.Insert(0, _currentPatchCurvePoints[_selectedIndex - 1]);
                }
                else
                {
                    feedbackCurvePoints.Insert(0, _currentPatchCurvePoints.Last());
                }

                if (_selectedIndex < _currentPatchCurvePoints.Count - 1)
                {
                    feedbackCurvePoints.Add(_currentPatchCurvePoints[_selectedIndex + 1]);
                }
                else
                {
                    feedbackCurvePoints.Add(_currentPatchCurvePoints.First());
                }

                var feedbackCurve = CurveUtilities.BuildCurve(feedbackCurvePoints.ToList(), DegreeOfCurve, false);
                var feedbackTube = Brep.CreatePipe(feedbackCurve, _tubeDiameter / 2, false, PipeCapMode.Round, false,
                    0.1, 0.1);

                e.Display.DrawBrepWires(BrepUtilities.Append(feedbackTube), feedbackTubeColor);

                var spherePreview = new Sphere(e.CurrentPoint, _tubeDiameter / 2);
                e.Display.DrawSphere(spherePreview, feedbackTubeColor);
            }

            if (_patchPreview != null)
            {
                e.Display.DrawBrepShaded(_patchPreview, new DisplayMaterial
                {
                    Transparency = 0.5,
                    Diffuse = meshWireColor,
                    Specular = meshWireColor,
                    Emission = meshWireColor
                });
            }
        }

        private Brep _patchPreview = null;

        public virtual void OnPostDrawObjects(DrawEventArgs e, EditGuide editGuide)
        {
            if (_currentCurve != null)
            {
                e.Display.DepthMode = DepthMode.AlwaysInFront;
                _currentPatchCurvePoints.ForEach(p =>
                {
                    e.Display.DrawPoint(p, PointStyle.ControlPoint, 5, Color.Red);
                });
                e.Display.DrawCurve(_currentCurve, Color.Yellow, 3);
                e.Display.DepthMode = DepthMode.Neutral;
            }

            e.Display.DrawMeshShaded(_currentCurveTube, new DisplayMaterial
            {
                Transparency = 0.5,
                Diffuse = meshWireColor,
                Specular = meshWireColor,
                Emission = meshWireColor
            });

            editGuide.RefreshViewPort();
        }

        public virtual void OnMouseMove(GetPointMouseEventArgs e, EditGuide editGuide)
        {
            if (e.LeftButtonDown && _selectedIndex >= 0)
            {
                _isMoving = true;
                var moved_point = editGuide.Point();
                var meshPoint = editGuide.LowLoDConstraintMesh.ClosestMeshPoint(moved_point, 0.0001);
                var pointOnMesh = meshPoint.Point;
                _currentPatchCurvePoints[_selectedIndex] = pointOnMesh;
                _needRegenerate = true;
            }
            else
            {
                _isMoving = false;
                if (_needRegenerate)
                {
                    SetCurrentEntities(editGuide);
                    _needRegenerate = false;
                    _patchPreview = BrepUtilities.CreatePatchOnMeshFromClosedCurve(_currentPatchCurvePoints, editGuide.LowLoDConstraintMesh);
                }
            }

            editGuide.RefreshViewPort();
        }

        public virtual void OnMouseLeave(RhinoView view, EditGuide editGuide)
        {

        }

        public virtual void OnMouseEnter(RhinoView view, EditGuide editGuide)
        {

        }

        public virtual bool OnFinalize(EditGuide editGuide, out bool isContinueLooping)
        {
            isContinueLooping = false;

            SetCurrentEntities(editGuide);
            var surfacePatch = GuideSurfaceUtilities.CreatePatch(_currentCurveTube, editGuide.LowLoDConstraintMesh, true);

            _patchSurfaces.Add(new PatchData(surfacePatch)
            {
                GuideSurfaceData = new PatchSurface
                {
                    ControlPoints = _currentPatchCurvePoints.ToList(),
                    Diameter = _tubeDiameter,
                    IsNegative = _isNegative
                }
            });

            _currentPatchCurvePoints.Clear();
            _currentCurve = new PolyCurve();
            _currentCurveTube = new Mesh();

            return true;
        }

        private void OnDiameterChanged(EditGuide editGuide)
        {
            IDSPluginHelper.WriteLine(LogCategory.Default, $"Diameter= {_tubeDiameter}");
            if (!_isMoving)
            {
                SetCurrentEntities(editGuide);
            }
            RhinoDoc.ActiveDoc.Views.Redraw();
        }

        private void SetCurrentEntities(EditGuide editGuide)
        {
            _currentCurve = CurveUtilities.BuildCurve(_currentPatchCurvePoints.ToList(), DegreeOfCurve, true);
            _currentCurve = _currentCurve.PullToMesh(editGuide.LowLoDConstraintMesh, 0.1);
            _currentCurveTube = GuideSurfaceUtilities.CreateCurveTube(_currentCurve, _tubeDiameter / 2);
            _patchPreview = BrepUtilities.CreatePatchOnMeshFromClosedCurve(_currentPatchCurvePoints, editGuide.LowLoDConstraintMesh);
            ConduitUtilities.RefeshConduit();
        }
    }
}
