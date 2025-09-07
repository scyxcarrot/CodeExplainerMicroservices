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
    public class EditSurfacePatchMode : IEditSurfaceState
    {
        private const int DegreeOfCurve = 1;
        protected Curve CurrentCurve;
        protected List<Point3d> CurrentPatchCurvePoints;
        private Mesh CurrentCurveTube;
        protected int SelectedIndex;

        protected List<PatchData> PatchSurfaces;
        private readonly PatchSurface _patchSurface;
        private double _tubeDiameter;
        protected bool IsMoving;
        protected bool NeedRegenerate;
        protected bool _isNegative;

        protected Color FeedbackTubeColor = Color.Red;
        protected Color MeshWireColor = Color.Red;

        public EditSurfacePatchMode(ref DrawSurfaceDataContext dataContext, PatchSurface patchSurface)
        {
            SelectedIndex = -1;
            PatchSurfaces = dataContext.PatchSurfaces;
            _patchSurface = patchSurface;
            _tubeDiameter = dataContext.PatchTubeDiameter;
            IsMoving = false;
            NeedRegenerate = false;
            CurrentCurve = new PolyCurve();
            CurrentCurveTube = new Mesh();
        }

        public void OnExecute(EditSurface editSurface)
        {
            _tubeDiameter = _patchSurface.Diameter;

            CurrentPatchCurvePoints = _patchSurface.ControlPoints.ToList();
            if (CurrentPatchCurvePoints[0]
                .Equals(CurrentPatchCurvePoints[CurrentPatchCurvePoints.Count - 1]))
            {
                CurrentPatchCurvePoints.RemoveAt(CurrentPatchCurvePoints.Count - 1);
            }

            SetCurrentEntities(editSurface);
        }

        public virtual void OnKeyboard(int key, EditSurface editSurface)
        {
            switch (key)
            {
                case (187): //+
                case (107): //+ numpad
                    _tubeDiameter += 0.5;
                    OnDiameterChanged(editSurface);
                    break;
                case (189): //-
                case (109): //- numpad
                    if (_tubeDiameter - 0.5 > 0.0)
                    {
                        _tubeDiameter -= 0.5;
                        OnDiameterChanged(editSurface);
                    }
                    else
                    {
                        IDSPluginHelper.WriteLine(LogCategory.Warning, "Diameter has to be bigger than 0.0");
                    }
                    break;
            }
        }

        public virtual bool OnGetPoint(Point3d point, EditSurface editSurface)
        {
            var point2d = editSurface.Point2d();
            SelectedIndex = PickUtilities.GetPickedPoint3dIndexFromPoint2d(point2d, CurrentPatchCurvePoints);

            var meshPoint = editSurface.ConstraintMesh.ClosestMeshPoint(point, 1.0).Point;
            var distTolerance = 10.0;
            if (SelectedIndex == -1 && Control.ModifierKeys == Keys.Shift)
            {
                CurrentCurve.ClosestPoint(meshPoint, out var closestPtParam);
                var closestPt = CurrentCurve.PointAt(closestPtParam);

                var dist = closestPt.DistanceTo(meshPoint);
                if (dist > distTolerance)
                {
                    return true;
                }

                var dictTmpPts = new SortedList<double, Point3d>();
                foreach (var currentPatchCurvePoint in CurrentPatchCurvePoints)
                {
                    double currentPatchCurvePointParam;
                    CurrentCurve.ClosestPoint(currentPatchCurvePoint, out currentPatchCurvePointParam);
                    dictTmpPts.Add(currentPatchCurvePointParam, currentPatchCurvePoint);
                }

                double newPointParamOnCurve;
                CurrentCurve.ClosestPoint(closestPt, out newPointParamOnCurve);

                dictTmpPts.Add(newPointParamOnCurve, closestPt);

                CurrentPatchCurvePoints = new List<Point3d>(dictTmpPts.Select(x => x.Value));
                SetCurrentEntities(editSurface);
            }
            else if (SelectedIndex != -1 && Control.ModifierKeys == Keys.Shift)
            {

                CurrentPatchCurvePoints.RemoveAt(SelectedIndex);
                SetCurrentEntities(editSurface);
            }

            return true;
        }

        public virtual void OnDynamicDraw(GetPointDrawEventArgs e, EditSurface editSurface)
        {
            if (IsMoving && SelectedIndex >= 0)
            {
                var feedbackCurvePoints = new List<Point3d>() { e.CurrentPoint };
                if (SelectedIndex > 0)
                {
                    feedbackCurvePoints.Insert(0, CurrentPatchCurvePoints[SelectedIndex - 1]);
                }
                else
                {
                    feedbackCurvePoints.Insert(0,
                        CurrentPatchCurvePoints[CurrentPatchCurvePoints.Count - 1]);
                }

                if (SelectedIndex < CurrentPatchCurvePoints.Count - 1)
                {
                    feedbackCurvePoints.Add(CurrentPatchCurvePoints[SelectedIndex + 1]);
                }
                else
                {
                    feedbackCurvePoints.Add(CurrentPatchCurvePoints[0]);
                }

                var feedbackCurve = CurveUtilities.BuildCurve(feedbackCurvePoints.ToList(), DegreeOfCurve, false);
                var feedbackTube = Brep.CreatePipe(feedbackCurve, _tubeDiameter / 2, false, PipeCapMode.Round, false,
                    0.1, 0.1);

                e.Display.DrawBrepWires(BrepUtilities.Append(feedbackTube), FeedbackTubeColor);

                var spherePreview = new Sphere(e.CurrentPoint, _tubeDiameter / 2);
                e.Display.DrawSphere(spherePreview, FeedbackTubeColor);
            }

            if (_patchPreview != null)
            {
                e.Display.DrawBrepShaded(_patchPreview, new DisplayMaterial
                {
                    Transparency = 0.5,
                    Diffuse = MeshWireColor,
                    Specular = MeshWireColor,
                    Emission = MeshWireColor
                });
            }
        }

        private Brep _patchPreview = null;

        public virtual void OnPostDrawObjects(DrawEventArgs e, EditSurface editSurface)
        {
            if (CurrentCurve != null)
            {
                e.Display.DepthMode = DepthMode.AlwaysInFront;
                CurrentPatchCurvePoints.ForEach(p =>
                {
                    e.Display.DrawPoint(p, PointStyle.ControlPoint, 5, Color.Red);
                });
                e.Display.DrawCurve(CurrentCurve, Color.Yellow, 3);
                e.Display.DepthMode = DepthMode.Neutral;
            }

            e.Display.DrawMeshShaded(CurrentCurveTube, new DisplayMaterial
            {
                Transparency = 0.5,
                Diffuse = MeshWireColor,
                Specular = MeshWireColor,
                Emission = MeshWireColor
            });

            editSurface.RefreshViewPort();
        }

        public virtual void OnMouseMove(
            GetPointMouseEventArgs e,
            EditSurface editSurface)
        {
            if (e.LeftButtonDown && SelectedIndex >= 0)
            {
                IsMoving = true;
                var moved_point = editSurface.Point();
                var meshPoint = editSurface.ConstraintMesh.ClosestMeshPoint(moved_point, 0.0001);
                var pointOnMesh = meshPoint.Point;
                CurrentPatchCurvePoints[SelectedIndex] = pointOnMesh;
                NeedRegenerate = true;
            }
            else
            {
                IsMoving = false;
                if (NeedRegenerate)
                {
                    SetCurrentEntities(editSurface);
                    NeedRegenerate = false;
                    _patchPreview = BrepUtilities.CreatePatchOnMeshFromClosedCurve(CurrentPatchCurvePoints, editSurface.ConstraintMesh);
                }
            }

            editSurface.RefreshViewPort();
        }

        public virtual void OnMouseLeave(RhinoView view, EditSurface editSurface)
        {

        }

        public virtual void OnMouseEnter(RhinoView view, EditSurface editSurface)
        {

        }

        public virtual bool OnFinalize(EditSurface editSurface, out bool isContinueLooping)
        {
            isContinueLooping = false;

            SetCurrentEntities(editSurface);
            var surfacePatch = GuideSurfaceUtilities.CreatePatch(CurrentCurveTube, editSurface.ConstraintMesh, true);
            if (surfacePatch != null)
            {
                PatchSurfaces.Add(new PatchData(surfacePatch)
                {
                    GuideSurfaceData = new PatchSurface
                    {
                        ControlPoints = CurrentPatchCurvePoints.ToList(),
                        Diameter = _tubeDiameter,
                        IsNegative = _isNegative
                    }
                });
            }
            else
            {
                IDSPluginHelper.WriteLine(
                    LogCategory.Error,
                    "Cannot create patch" +
                    " because the curve draw is too close to the edge");
            }

            CurrentPatchCurvePoints.Clear();
            CurrentCurve = new PolyCurve();
            CurrentCurveTube = new Mesh();

            return true;
        }

        private void OnDiameterChanged(EditSurface editSurface)
        {
            IDSPluginHelper.WriteLine(LogCategory.Default, $"Diameter= {_tubeDiameter}");
            if (!IsMoving)
            {
                SetCurrentEntities(editSurface);
            }
            RhinoDoc.ActiveDoc.Views.Redraw();
        }

        protected virtual void SetCurrentEntities(EditSurface editSurface)
        {
            CurrentCurve = CurveUtilities.BuildCurve(CurrentPatchCurvePoints.ToList(), DegreeOfCurve, true);
            CurrentCurve = CurrentCurve.PullToMesh(editSurface.ConstraintMesh, 0.1);
            CurrentCurveTube = GuideSurfaceUtilities.CreateCurveTube(CurrentCurve, _tubeDiameter / 2);
            _patchPreview = BrepUtilities.CreatePatchOnMeshFromClosedCurve(CurrentPatchCurvePoints, editSurface.ConstraintMesh);
            ConduitUtilities.RefeshConduit();
        }
    }
}
