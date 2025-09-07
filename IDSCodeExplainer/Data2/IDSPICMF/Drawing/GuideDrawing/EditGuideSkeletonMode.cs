using IDS.CMF.DataModel;
using IDS.CMF.Utilities;
using IDS.CMF.Visualization;
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
    public class EditGuideSkeletonMode : IEditGuideState
    {
        class SkeletonObject
        {
            public List<Point3d> ControlPoints;
            public Polyline Polyline;
            public Mesh Surface;
        }

        private readonly DrawGuideDataContext _dataContext;
        private int _curveDegree = 1;
        private readonly Color _pointColor = Color.Crimson;
        private readonly Color _curveColor = Color.Blue;
        private readonly Color _tubeColor = Colors.GuidePositiveSkeletonWireframe;
        private Point3d _selectedPoint;
        private int _selectedIndex;
        private bool _isMoving;
        private bool _needRegenerate;

        private List<Point3d> _allSkeletonCurvePoints = new List<Point3d>();
        private List<SkeletonObject> _allSkeletons = new List<SkeletonObject>();
        private readonly List<Point3d> _neighbouringPointsOfSelectedIndex = new List<Point3d>();
        private readonly SkeletonSurface _skeletonSurface;

        public EditGuideSkeletonMode(ref DrawGuideDataContext dataContext, SkeletonSurface skeletonSurface)
        {
            _selectedIndex = -1;
            _dataContext = dataContext;
            _skeletonSurface = skeletonSurface;
            _isMoving = false;
            _needRegenerate = false;
        }

        public void OnExecute(EditGuide editGuide)
        {
            _dataContext.SkeletonTubeDiameter = _skeletonSurface.Diameter;

            InvalidateSkeleton(editGuide);
        }

        private void InvalidateSkeleton(EditGuide editGuide)
        {
            _allSkeletonCurvePoints = new List<Point3d>();
            _allSkeletons = new List<SkeletonObject>();

            foreach (var points in _skeletonSurface.ControlPoints)
            {
                _allSkeletonCurvePoints.AddRange(points);
                var polyline = new Polyline(points);
                _allSkeletons.Add(new SkeletonObject
                {
                    ControlPoints = points,
                    Polyline = polyline,
                    Surface = CreateTubeMesh(editGuide.LowLoDConstraintMesh, polyline)
                });
            }

            _allSkeletonCurvePoints = _allSkeletonCurvePoints.Distinct().ToList();
        }

        public void OnKeyboard(int key, EditGuide editGuide)
        {
            switch (key)
            {
                case (187): //+
                case (107): //+ numpad
                    _dataContext.SkeletonTubeDiameter += 0.5;
                    OnDiameterChanged(editGuide);
                    break;
                case (189): //-
                case (109): //- numpad
                    if (_dataContext.SkeletonTubeDiameter - 0.5 > 0.0)
                    {
                        _dataContext.SkeletonTubeDiameter -= 0.5;
                        OnDiameterChanged(editGuide);
                    }
                    else
                    {
                        IDSPluginHelper.WriteLine(LogCategory.Warning, "Diameter has to be bigger than 0.0");
                    }
                    break;
                default:
                    return; // nothing to do
            }
        }

        private SkeletonObject GetClosestSkeletonObject(Point3d testPoint, List<SkeletonObject> skelObjects)
        {
            SkeletonObject res = null;

            var closestDist = double.MaxValue;
            skelObjects.ForEach(x =>
            {
                var closestPt = x.Polyline.ClosestPoint(testPoint);
                var dist = closestPt.DistanceTo(testPoint);

                if (dist < closestDist)
                {
                    closestDist = dist;
                    res = x;
                }
            });

            return res;
        }

        public bool OnGetPoint(Point3d point, EditGuide editGuide)
        {
            var point2d = editGuide.Point2d();
            _selectedIndex = PickUtilities.GetPickedPoint3dIndexFromPoint2d(point2d, _allSkeletonCurvePoints);
            SetNeighbouringPoints();

            //TODO control point
            var meshPoint = editGuide.LowLoDConstraintMesh.ClosestMeshPoint(point, 1.0).Point;
            var distTolerance = 10.0;
            if (_selectedIndex == -1 && Control.ModifierKeys == Keys.Shift)
            {
                var closestSkelObject = GetClosestSkeletonObject(meshPoint, _allSkeletons);
                var curve = closestSkelObject.Polyline.ToNurbsCurve().PullToMesh(editGuide.LowLoDConstraintMesh, 0.1);

                double closestPtParam;
                curve.ClosestPoint(meshPoint, out closestPtParam);
                var closestPt = curve.PointAt(closestPtParam);

                var dist = closestPt.DistanceTo(meshPoint);
                if (dist > distTolerance)
                {
                    return true;
                }

                bool pointAdded = false;
                var tmpCurrentCurvePoints = new List<Point3d>();
                foreach (var currentPatchCurvePoint in closestSkelObject.ControlPoints)
                {
                    double currentPatchCurvePointParam;
                    curve.ClosestPoint(currentPatchCurvePoint, out currentPatchCurvePointParam);

                    if (currentPatchCurvePointParam < closestPtParam)
                    {
                        tmpCurrentCurvePoints.Add(currentPatchCurvePoint);
                    }
                    else
                    {
                        if (!pointAdded)
                        {
                            tmpCurrentCurvePoints.Add(closestPt);
                            pointAdded = true;
                        }

                        tmpCurrentCurvePoints.Add(currentPatchCurvePoint);
                    }
                }

                var skelSurfacePointSetIndex = _allSkeletons.IndexOf(closestSkelObject);
                _skeletonSurface.ControlPoints[skelSurfacePointSetIndex] = new List<Point3d>(tmpCurrentCurvePoints);

                InvalidateSkeleton(editGuide);
            }
            else if (_selectedIndex != -1 && Control.ModifierKeys == Keys.Shift)
            {
                var closestSkelObject = GetClosestSkeletonObject(meshPoint, _allSkeletons);

                if (closestSkelObject.ControlPoints.Count < 3)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, "There must be at least 2 points!");
                    return true;
                }

                var skelSurfacePointSetIndex = _allSkeletons.IndexOf(closestSkelObject);

                var pt = PointUtilities.FindClosestPoint(meshPoint, closestSkelObject.ControlPoints);
                _skeletonSurface.ControlPoints[skelSurfacePointSetIndex].RemoveAt(closestSkelObject.ControlPoints.IndexOf(pt));

                InvalidateSkeleton(editGuide);
            }

            return true;
        }

        public void OnDynamicDraw(GetPointDrawEventArgs e, EditGuide editGuide)
        {
            if (_isMoving && _selectedIndex >= 0)
            {
                var spherePreview = new Sphere(e.CurrentPoint, _dataContext.SkeletonTubeDiameter/2);
                e.Display.DrawSphere(spherePreview, _tubeColor);

                foreach (var point in _neighbouringPointsOfSelectedIndex)
                {
                    e.Display.DrawLine(point, e.CurrentPoint, _curveColor);
                    var curvePoints = new List<Point3d>() { point, e.CurrentPoint };
                    e.Display.DrawBrepWires(CreatePipeBrep(curvePoints, _dataContext.SkeletonTubeDiameter/2), _tubeColor);
                }
            }
        }

        public void OnPostDrawObjects(DrawEventArgs e, EditGuide editGuide)
        {
            _allSkeletonCurvePoints.ForEach(p =>
            {
                e.Display.DepthMode = DepthMode.AlwaysInFront;
                e.Display.DrawPoint(p, PointStyle.ControlPoint, 5, _pointColor);
            });
            e.Display.DepthMode = DepthMode.Neutral;

            foreach (var skeleton in _allSkeletons)
            {
                e.Display.DepthMode = DepthMode.AlwaysInFront;
                e.Display.DrawPolyline(skeleton.Polyline, _curveColor,3);
                e.Display.DepthMode = DepthMode.Neutral;
                e.Display.DrawMeshShaded(skeleton.Surface, new DisplayMaterial
                {
                    Transparency = 0.5,
                    Diffuse = _tubeColor,
                    Specular = _tubeColor,
                    Emission = _tubeColor
                });
            }

            editGuide.RefreshViewPort();
        }

        public void OnMouseMove(GetPointMouseEventArgs e, EditGuide editGuide)
        {
            if (e.LeftButtonDown && _selectedIndex >= 0)
            {
                _isMoving = true;
                var moved_point = editGuide.Point();
                var meshPoint = editGuide.LowLoDConstraintMesh.ClosestMeshPoint(moved_point, 0.0001);
                var pointOnMesh = meshPoint.Point;
                _allSkeletonCurvePoints[_selectedIndex] = pointOnMesh;
                _needRegenerate = true;
            }
            else
            {
                _isMoving = false;
                if (_needRegenerate)
                {
                    UpdateAffectedEntities(editGuide);
                    _needRegenerate = false;
                }
            }

            editGuide.RefreshViewPort();
        }

        public void OnMouseLeave(RhinoView view, EditGuide editGuide)
        {

        }

        public void OnMouseEnter(RhinoView view, EditGuide editGuide)
        {

        }

        public bool OnFinalize(EditGuide editGuide, out bool isContinueLooping)
        {
            isContinueLooping = false;
            _selectedIndex = -1;

            if (_allSkeletons.Any())
            {
                var curves = new List<Curve>();
                var controlPoints = new List<List<Point3d>>();
                _allSkeletons.ForEach(skeleton => curves.Add(CurveUtilities.BuildCurve(skeleton.ControlPoints, _curveDegree, false)));
                _allSkeletons.ForEach(skeleton => controlPoints.Add(skeleton.ControlPoints));

                _allSkeletonCurvePoints.Clear();
                _allSkeletons.Clear();
                _neighbouringPointsOfSelectedIndex.Clear();

                var surface = GuideSurfaceUtilities.CreateSkeletonSurface(editGuide.LowLoDConstraintMesh, curves, _dataContext.SkeletonTubeDiameter/2);
                _dataContext.SkeletonSurfaces.Add(new PatchData(surface)
                {
                    GuideSurfaceData = new SkeletonSurface
                    {
                        ControlPoints = controlPoints,
                        Diameter = _dataContext.SkeletonTubeDiameter,
                        IsNegative = false
                    }
                });
            }

            return true;
        }

        private Brep CreatePipeBrep(List<Point3d> curvePoints, double pipeRadius)
        {
            var curve = CurveUtilities.BuildCurve(curvePoints, _curveDegree, false);
            return CreatePipeBrep(curve, pipeRadius);
        }

        private Brep CreatePipeBrep(Curve curve, double pipeRadius)
        {
            var pipe = Brep.CreatePipe(curve, pipeRadius, false, PipeCapMode.Round, false, 0.1, 0.1);
            return BrepUtilities.Append(pipe);
        }

        private Mesh CreateTubeMesh(Mesh constraintMesh, Polyline polyline)
        {
            var currentCurve = CurveUtilities.BuildCurve(polyline.ToList(), _curveDegree, false);
            var curve = currentCurve.PullToMesh(constraintMesh, 0.1);
            return GuideSurfaceUtilities.CreateCurveTube(curve, _dataContext.SkeletonTubeDiameter/2);
        }

        private void OnDiameterChanged(EditGuide editGuide)
        {
            IDSPluginHelper.WriteLine(LogCategory.Default, $"Diameter= {_dataContext.SkeletonTubeDiameter}");
            if (!_isMoving)
            {
                _allSkeletons.ForEach(skeleton => skeleton.Surface = CreateTubeMesh(editGuide.LowLoDConstraintMesh, skeleton.Polyline));
            }
            RhinoDoc.ActiveDoc.Views.Redraw();
        }

        private void SetNeighbouringPoints()
        {
            _neighbouringPointsOfSelectedIndex.Clear();
            if (_selectedIndex == -1)
            {
                return;
            }

            var selectedPoint = _allSkeletonCurvePoints[_selectedIndex];
            foreach (var skeleton in _allSkeletons)
            {
                if (skeleton.ControlPoints.Contains(selectedPoint))
                {
                    var index = skeleton.ControlPoints.IndexOf(selectedPoint);
                    if (index == 0)
                    {
                        _neighbouringPointsOfSelectedIndex.Add(skeleton.ControlPoints[index + 1]);
                    }
                    else if (index == skeleton.ControlPoints.Count - 1)
                    {
                        _neighbouringPointsOfSelectedIndex.Add(skeleton.ControlPoints[index - 1]);
                    }
                    else
                    {
                        _neighbouringPointsOfSelectedIndex.Add(skeleton.ControlPoints[index + 1]);
                        _neighbouringPointsOfSelectedIndex.Add(skeleton.ControlPoints[index - 1]);
                    }
                }
            }
            _selectedPoint = selectedPoint;
        }

        private void UpdateAffectedEntities(EditGuide editGuide)
        {
            var newPoint = _allSkeletonCurvePoints[_selectedIndex];
            foreach (var skeleton in _allSkeletons)
            {
                if (skeleton.ControlPoints.Contains(_selectedPoint))
                {
                    var index = skeleton.ControlPoints.IndexOf(_selectedPoint);
                    skeleton.ControlPoints[index] = newPoint;
                    skeleton.Polyline = new Polyline(skeleton.ControlPoints);
                    skeleton.Surface = CreateTubeMesh(editGuide.LowLoDConstraintMesh, skeleton.Polyline);
                }
            }
        }
    }
}
