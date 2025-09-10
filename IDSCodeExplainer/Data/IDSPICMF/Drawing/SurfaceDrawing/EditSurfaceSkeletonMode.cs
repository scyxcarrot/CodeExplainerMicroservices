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
    public class EditSurfaceSkeletonMode : IEditSurfaceState
    {
        private sealed class SkeletonObject
        {
            public List<Point3d> ControlPoints;
            public Polyline Polyline;
            public Mesh Surface;
        }

        private readonly DrawSurfaceDataContext _dataContext;
        private readonly int _curveDegree = 1;
        private readonly Color _pointColor = Color.Crimson;
        private readonly Color _curveColor = Color.Blue;
        private readonly Color _tubeColor = Color.Yellow;
        private Point3d _selectedPoint;
        private int _selectedIndex;
        private bool _isMoving;
        private bool _needRegenerate;

        private List<Point3d> _allSkeletonCurvePoints = new List<Point3d>();
        private List<SkeletonObject> _allSkeletons = new List<SkeletonObject>();
        private readonly List<Point3d> _neighbouringPointsOfSelectedIndex = new List<Point3d>();
        private readonly SkeletonSurface _skeletonSurface;

        public EditSurfaceSkeletonMode(
            ref DrawSurfaceDataContext dataContext, 
            SkeletonSurface skeletonSurface)
        {
            _selectedIndex = -1;
            _dataContext = dataContext;
            _skeletonSurface = skeletonSurface;
            _isMoving = false;
            _needRegenerate = false;
        }

        public void OnExecute(EditSurface editSurface)
        {
            _dataContext.SkeletonTubeDiameter = _skeletonSurface.Diameter;
            InvalidateSkeleton(editSurface);
        }

        private void InvalidateSkeleton(EditSurface editSurface)
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
                    Surface = CreateTubeMesh(editSurface.ConstraintMesh, polyline)
                });
            }

            _allSkeletonCurvePoints = _allSkeletonCurvePoints.Distinct().ToList();
        }

        public void OnKeyboard(int key, EditSurface editSurface)
        {
            switch (key)
            {
                case (187): //+
                case (107): //+ numpad
                    _dataContext.SkeletonTubeDiameter += 0.5;
                    OnDiameterChanged(editSurface);
                    break;
                case (189): //-
                case (109): //- numpad
                    if (_dataContext.SkeletonTubeDiameter - 0.5 > 0.0)
                    {
                        _dataContext.SkeletonTubeDiameter -= 0.5;
                        OnDiameterChanged(editSurface);
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

        private static SkeletonObject GetClosestSkeletonObject(Point3d testPoint, List<SkeletonObject> skelObjects)
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

        public bool OnGetPoint(Point3d point, EditSurface editSurface)
        {
            var point2d = editSurface.Point2d();
            _selectedIndex = PickUtilities.GetPickedPoint3dIndexFromPoint2d(point2d, _allSkeletonCurvePoints);
            SetNeighbouringPoints();

            var meshPoint = editSurface.ConstraintMesh.ClosestMeshPoint(point, 1.0).Point;
            var distanceTolerance = 10.0;
            if (_selectedIndex == -1 && Control.ModifierKeys == Keys.Shift)
            {
                var closestSkeletonObject = GetClosestSkeletonObject(meshPoint, _allSkeletons);
                var curve = closestSkeletonObject.Polyline.ToNurbsCurve().PullToMesh(editSurface.ConstraintMesh, 0.1);

                curve.ClosestPoint(meshPoint, out var closestPtParam);
                var closestPt = curve.PointAt(closestPtParam);

                var dist = closestPt.DistanceTo(meshPoint);
                if (dist > distanceTolerance)
                {
                    return true;
                }

                var pointAdded = false;
                var tempCurrentCurvePoints = new List<Point3d>();
                foreach (var currentPatchCurvePoint in closestSkeletonObject.ControlPoints)
                {
                    curve.ClosestPoint(currentPatchCurvePoint, 
                        out var currentPatchCurvePointParam);

                    if (currentPatchCurvePointParam < closestPtParam)
                    {
                        tempCurrentCurvePoints.Add(currentPatchCurvePoint);
                    }
                    else
                    {
                        if (!pointAdded)
                        {
                            tempCurrentCurvePoints.Add(closestPt);
                            pointAdded = true;
                        }

                        tempCurrentCurvePoints.Add(currentPatchCurvePoint);
                    }
                }

                var skeletonSurfacePointSetIndex = _allSkeletons.IndexOf(closestSkeletonObject);
                _skeletonSurface.ControlPoints[skeletonSurfacePointSetIndex] = 
                    new List<Point3d>(tempCurrentCurvePoints);
                InvalidateSkeleton(editSurface);
            }
            else if (_selectedIndex != -1 && Control.ModifierKeys == Keys.Shift)
            {
                var closestSkeletonObject = GetClosestSkeletonObject(meshPoint, _allSkeletons);

                if (closestSkeletonObject.ControlPoints.Count < 3)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, "There must be at least 2 points!");
                    return true;
                }

                var skeletonSurfacePointSetIndex = _allSkeletons.IndexOf(closestSkeletonObject);

                var closestPoint = PointUtilities.FindClosestPoint(meshPoint, closestSkeletonObject.ControlPoints);
                _skeletonSurface.ControlPoints[skeletonSurfacePointSetIndex].RemoveAt(closestSkeletonObject.ControlPoints.IndexOf(closestPoint));

                InvalidateSkeleton(editSurface);
            }

            return true;
        }

        public void OnDynamicDraw(GetPointDrawEventArgs e, EditSurface editSurface)
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

        public void OnPostDrawObjects(DrawEventArgs e, EditSurface editSurface)
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

            editSurface.RefreshViewPort();
        }

        public void OnMouseMove(GetPointMouseEventArgs e, EditSurface editSurface)
        {
            if (e.LeftButtonDown && _selectedIndex >= 0)
            {
                _isMoving = true;
                var moved_point = editSurface.Point();
                var meshPoint = editSurface.ConstraintMesh.ClosestMeshPoint(moved_point, 0.0001);
                var pointOnMesh = meshPoint.Point;
                _allSkeletonCurvePoints[_selectedIndex] = pointOnMesh;
                _needRegenerate = true;
            }
            else
            {
                _isMoving = false;
                if (_needRegenerate)
                {
                    UpdateAffectedEntities(editSurface);
                    _needRegenerate = false;
                }
            }

            editSurface.RefreshViewPort();
        }

        public void OnMouseLeave(RhinoView view, EditSurface editSurface)
        {

        }

        public void OnMouseEnter(RhinoView view, EditSurface editSurface)
        {

        }

        public bool OnFinalize(EditSurface editSurface, out bool isContinueLooping)
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

                var surface = GuideSurfaceUtilities.CreateSkeletonSurface(editSurface.ConstraintMesh, curves, _dataContext.SkeletonTubeDiameter/2);
                if (surface != null)
                {
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
            }

            return true;
        }

        private Brep CreatePipeBrep(List<Point3d> curvePoints, double pipeRadius)
        {
            var curve = CurveUtilities.BuildCurve(curvePoints, _curveDegree, false);
            return CreatePipeBrep(curve, pipeRadius);
        }

        private static Brep CreatePipeBrep(Curve curve, double pipeRadius)
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

        private void OnDiameterChanged(EditSurface editSurface)
        {
            IDSPluginHelper.WriteLine(LogCategory.Default, $"Diameter= {_dataContext.SkeletonTubeDiameter}");
            if (!_isMoving)
            {
                _allSkeletons.ForEach(skeleton => skeleton.Surface = CreateTubeMesh(editSurface.ConstraintMesh, skeleton.Polyline));
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
            var skeletonsWithPoint = _allSkeletons
                .Where(skeleton => skeleton.ControlPoints.Contains(selectedPoint))
                .ToList();


            foreach (var controlPoints in skeletonsWithPoint.Select(x=>x.ControlPoints))
            {
                var index = controlPoints.IndexOf(selectedPoint);
                if (index == 0)
                {
                    _neighbouringPointsOfSelectedIndex.Add(controlPoints[index + 1]);
                }
                else if (index == controlPoints.Count - 1)
                {
                    _neighbouringPointsOfSelectedIndex.Add(controlPoints[index - 1]);
                }
                else
                {
                    _neighbouringPointsOfSelectedIndex.Add(controlPoints[index + 1]);
                    _neighbouringPointsOfSelectedIndex.Add(controlPoints[index - 1]);
                }
            }
            
            _selectedPoint = selectedPoint;
        }

        private void UpdateAffectedEntities(EditSurface editSurface)
        {
            var newPoint = _allSkeletonCurvePoints[_selectedIndex];
            var skeletonsWithPoint = _allSkeletons.Where(skeleton => skeleton.ControlPoints.Contains(_selectedPoint));

            foreach (var skeletonWithPoint in skeletonsWithPoint)
            {
                var index = skeletonWithPoint.ControlPoints.IndexOf(_selectedPoint);
                skeletonWithPoint.ControlPoints[index] = newPoint;
                skeletonWithPoint.Polyline = new Polyline(skeletonWithPoint.ControlPoints);
                skeletonWithPoint.Surface = CreateTubeMesh(
                    editSurface.ConstraintMesh, skeletonWithPoint.Polyline);
            }
        }
    }
}
