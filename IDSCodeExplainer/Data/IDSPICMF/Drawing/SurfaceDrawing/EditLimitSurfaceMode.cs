using IDS.CMF.DataModel;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.Plugin;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Input.Custom;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IDS.PICMF.Drawing
{
    public class EditLimitSurfaceMode : EditSurfacePatchMode
    {
        private double _extensionLength = 10;
        private readonly double _tubeDiameter = 0.3;
        private Brep _currentCurveTube;
        private Brep _limitSurfacePreview;
        private readonly Color _previewPointColor = Color.Coral;

        public EditLimitSurfaceMode(ref DrawSurfaceDataContext dataContext, PatchSurface patchSurface) : base(ref dataContext, patchSurface)
        {
            _extensionLength = patchSurface.Diameter > 10 ? patchSurface.Diameter : 10;
        }

        public override void OnKeyboard(int key, EditSurface editSurface)
        {
            switch (key)
            {
                case (187): //+
                case (107): //+ numpad
                    _extensionLength += 1.0;
                    CreateBrepFromPoints();
                    IDSPluginHelper.WriteLine(LogCategory.Default, $"Extended Length= {_extensionLength}");
                    break;
                case (189): //-
                case (109): //- numpad
                    if (_extensionLength - 1.0 >= 10)
                    {
                        _extensionLength -= 1.0;
                        CreateBrepFromPoints();
                        IDSPluginHelper.WriteLine(LogCategory.Default, $"Extended Length= {_extensionLength}");
                    }
                    else
                    {
                        IDSPluginHelper.WriteLine(LogCategory.Warning, "Length has to be bigger than 10mm");
                    }
                    break;
            }
        }

        public override void OnDynamicDraw(GetPointDrawEventArgs e, EditSurface editSurface)
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
                    feedbackCurvePoints.Insert(0, CurrentPatchCurvePoints[CurrentPatchCurvePoints.Count - 1]);
                }

                if (SelectedIndex < CurrentPatchCurvePoints.Count - 1)
                {
                    feedbackCurvePoints.Add(CurrentPatchCurvePoints[SelectedIndex + 1]);
                }
                else
                {
                    feedbackCurvePoints.Add(CurrentPatchCurvePoints[0]);
                }

                var feedbackCurve = CurveUtilities.BuildCurve(feedbackCurvePoints.ToList(), 1, false);
                var feedbackTube = Brep.CreatePipe(feedbackCurve, _tubeDiameter / 2, false, PipeCapMode.Round, false, 0.1, 0.1);
                var spherePreview = new Sphere(e.CurrentPoint, _tubeDiameter / 2);
               
                e.Display.DrawBrepWires(BrepUtilities.Append(feedbackTube), FeedbackTubeColor);
                e.Display.DrawSphere(spherePreview, FeedbackTubeColor);
            }

            e.Display.DrawBrepShaded(_currentCurveTube, new DisplayMaterial
            {
                Transparency = 0.5,
                Diffuse = MeshWireColor,
                Specular = MeshWireColor,
                Emission = MeshWireColor
            });

            if (_limitSurfacePreview != null)
            {
                e.Display.DrawBrepShaded(_limitSurfacePreview, new DisplayMaterial
                {
                    Transparency = 0.5,
                    Diffuse = _previewPointColor,
                    Specular = _previewPointColor,
                    Emission = _previewPointColor
                });
            }
        }

        public override void OnMouseMove(GetPointMouseEventArgs e, EditSurface editSurface)
        {
            if (e.LeftButtonDown && SelectedIndex >= 0)
            {
                IsMoving = true;
                var movedPoint = editSurface.Point();
                var meshPoint = editSurface.ConstraintMesh.ClosestMeshPoint(movedPoint, 0.0001);
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
                }
            }
            editSurface.RefreshViewPort();
        }

        protected override void SetCurrentEntities(EditSurface editSurface)
        {
            CreateBrepFromPoints();
            CurrentCurve = CurveUtilities.BuildCurve(CurrentPatchCurvePoints.ToList(), 1, true);
            _currentCurveTube = GuideSurfaceUtilities.CreatePipeBrep(CurrentPatchCurvePoints.ToList(), 0.3 / 2);
            ConduitUtilities.RefeshConduit();
        }

        public override bool OnFinalize(EditSurface editSurface, out bool isContinueLooping)
        {
            isContinueLooping = false;
            SetCurrentEntities(editSurface);
            var brepInner = BrepUtilities.CreatePatchFromPoints(CurrentPatchCurvePoints);
            var surfacePatch = MeshUtilities.ConvertBrepToMesh(brepInner);
            if (surfacePatch != null)
            {
                PatchSurfaces.Add(new PatchData(surfacePatch)
                {
                    GuideSurfaceData = new PatchSurface
                    {
                        ControlPoints = CurrentPatchCurvePoints.ToList(),
                        Diameter = _extensionLength,
                        IsNegative = false
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

            return true;
        }

        private void CreateBrepFromPoints()
        {
            var console = new IDSRhinoConsole();
            var framePoints = TeethSupportedGuideUtilities.ProcessPoints(console, CurrentPatchCurvePoints, _extensionLength);
            var brepOuter = BrepUtilities.CreatePatchFromPoints(framePoints);
            _limitSurfacePreview = brepOuter;
        }
    }
}
