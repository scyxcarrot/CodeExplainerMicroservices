using IDS.CMF.DataModel;
using IDS.CMF.Utilities;
using IDS.CMF.Visualization;
using IDS.Core.Drawing;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.PICMF.Helper;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.UI;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace IDS.PICMF.Drawing
{
    public class DrawGuide : GetCurvePoints
    {
        public DrawGuideResult ResultOfGuideDrawing { get; set; }

        private IDrawGuideState CurrentDrawGuideMode;
        private readonly DrawGuideSkeletonMode _skeletonModeBuffer;
        private readonly DrawGuidePatchMode _patchModeBuffer;
        private readonly DrawGuideDataContext _dataContext;

        private readonly ToggleGuideDrawingTransparencyVisualization _guideSupportVisualizationToggler;

        private readonly Color _positivePatchColor;
        private readonly Color _negativePatchColor;
        private readonly Color _positiveSkeletonColor;

        private Mesh _lowLoDConstraintMesh;
        public Mesh LowLoDConstraintMesh
        {
            get { return _lowLoDConstraintMesh; }
            set
            {
                _lowLoDConstraintMesh = value;
                if (value != null)
                {
                    Constrain(value, false);
                }
            }
        }

        public Mesh GuideSurfaceCreationLowLoDBase { get; }

        public DrawGuide(Mesh lowLoDConstraintMesh, Mesh guideSurfaceCreationLowLoDBase, DrawGuideDataContext dataContext, bool allowNegativeDrawing, bool onlyPatchMode = false, bool drawSolidSurface = false)
        {
            LowLoDConstraintMesh = lowLoDConstraintMesh;
            GuideSurfaceCreationLowLoDBase = guideSurfaceCreationLowLoDBase;
            
            AcceptString(true); // Press ENTER is allowed when contain text like -,+,o in console
            AcceptNothing(true); // Pressing ENTER is allowed
            AcceptUndo(true);
            PermitObjectSnap(true); // Only allow our own constraining geometry

            _dataContext = dataContext;

            _patchModeBuffer = new DrawGuidePatchMode(ref _dataContext, allowNegativeDrawing, onlyPatchMode, drawSolidSurface);

            if (onlyPatchMode)
            {
                CurrentDrawGuideMode = _patchModeBuffer;
            }
            else
            {
                _skeletonModeBuffer = new DrawGuideSkeletonMode(ref _dataContext);
                CurrentDrawGuideMode = _skeletonModeBuffer;
            }

            _guideSupportVisualizationToggler = new ToggleGuideDrawingTransparencyVisualization(GuideDrawingTransparencyProxy.IsTransparent);
            _positivePatchColor = drawSolidSurface ? Colors.GuideSolidPatch : Colors.GuidePositivePatchWireframe;
            _negativePatchColor = Colors.GuideNegativePatchWireframe;
            _positiveSkeletonColor = Colors.GuidePositiveSkeletonWireframe;
        }

        #region KeyState

        // Get the key state
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern short GetKeyState(int keyCode);
        
        // Detect if a keyboard key is down
        private static bool IsKeyDown(int key)
        {
            short retVal = GetKeyState(key);

            //If the high-order bit is 1, the key is down
            //otherwise, it is up.
            if ((retVal & 0x8000) == 0x8000)
            {
                return true;
            }

            //If the low-order bit is 1, the key is toggled.
            return false;
        }

        protected void OnKeyboard(int key)
        {
            // Only execute if key is down (avoid triggering on key release)
            if (!IsKeyDown(key))
            {
                return;
            }

            CurrentDrawGuideMode.OnKeyboard(key, this);

            switch (key)
            {
                case (79): //O
                    GuideDrawingTransparencyProxy.IsTransparent = !GuideDrawingTransparencyProxy.IsTransparent;
                    _guideSupportVisualizationToggler.DoToggle(GuideDrawingTransparencyProxy.IsTransparent);
                    break;
                case (187): //+
                case (107): //+ numpad

                    break;
                case (189): //-
                case (109): //- numpad

                    break;
            }

            RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.SetCameraLocations(
                RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraTarget,
                RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraLocation);
            RefreshViewPort();
            RhinoDoc.ActiveDoc.Views.Redraw();
        }

        #endregion

        private int CalculateCreatedSurfaces()
        {
            return _dataContext.NegativePatchSurface.Count + _dataContext.PositivePatchSurface.Count + _dataContext.SkeletonCurves.Count;
        }

        public bool Execute()
        {
            if (LowLoDConstraintMesh == null)
            {
                return false;
            }

            RhinoApp.KeyboardEvent += OnKeyboard;

            var mouseCallback = new IDSMouseCallback {Enabled = true};
            mouseCallback.MouseEnter += OnMouseEnter;
            mouseCallback.MouseLeave += OnMouseLeave;

            ResultOfGuideDrawing = null;

            var prevTotalSurface = 0;
            var drawCounter = 0;
            while (true)
            {
                this.EnableTransparentCommands(false);
                var rc = this.Get();

                if (rc == GetResult.String)
                {
                    var stringRes = StringResult().ToLower();
                    if (stringRes.Contains("redo") ||
                        stringRes.Contains("/_copy"))
                    {
                        continue;
                    }
                }

                if (rc == GetResult.Point)
                {
                    prevTotalSurface = CalculateCreatedSurfaces();

                    CurrentDrawGuideMode.OnGetPoint(base.Point(), LowLoDConstraintMesh, this);

                    var currentTotalSurface = CalculateCreatedSurfaces();
                    if (currentTotalSurface != prevTotalSurface)
                    {
                        drawCounter++;
                    }
                }
                else if (rc == GetResult.Nothing || rc == GetResult.String)
                {
                    drawCounter++;
                    bool continueLooping;
                    CurrentDrawGuideMode.OnFinalize(LowLoDConstraintMesh, out continueLooping);

                    if (continueLooping)
                    {
                        continue;
                    }
                    break; // User pressed ENTER
                }
                else if (rc == GetResult.Cancel)
                {
                    if (drawCounter > 0)
                    {
                        var dlgRes = MessageBox.Show(
                            "Pressing Esc will delete the surfaces that you have drawn and are currently drawing. Do you want to proceed?",
                            "Drawing Surface", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);

                        if (dlgRes == DialogResult.OK)
                        {
                            UnregisterEvents(mouseCallback);

                            return false;
                        }

                        continue;
                    }

                    UnregisterEvents(mouseCallback);
                    return false;
                }
                else if (rc == GetResult.Undo)
                {
                    CurrentDrawGuideMode.OnUndo(LowLoDConstraintMesh);
                    RefreshViewPort();
                }
                else
                {
                    var helper = new TransparentCommandHelper();
                    helper.HandleGuideDrawingTransparentCommands(this);
                }
            }

            UnregisterEvents(mouseCallback);

            if (_dataContext.ContainsDrawing())
            {
                PrepareResult();
            }
            else
            {
                return false;
            }

            return true;
        }

        private void UnregisterEvents(IDSMouseCallback mouseCallback)
        {
            RhinoApp.KeyboardEvent -= OnKeyboard;
            mouseCallback.Enabled = false;
            mouseCallback.MouseEnter -= OnMouseEnter;
            mouseCallback.MouseLeave -= OnMouseLeave;
        }

        private Mesh CreateRoIMesh()
        {
            //Prepare ROI
            var roIPatches = new Mesh();

            var constraintRoIDefiner = new Mesh();

            if (_dataContext.RoIMeshDefiner.IsValid)
            {
                constraintRoIDefiner.Append(_dataContext.RoIMeshDefiner);
            }

            _dataContext.SkeletonCurves.ForEach(x =>
            {
                var t = GuideSurfaceUtilities.CreateSkeletonTube(LowLoDConstraintMesh, x.Key, _dataContext.SkeletonTubeDiameter / 2);
                constraintRoIDefiner.Append(t);
            });

            _dataContext.PositivePatchTubes.ForEach(x =>
            {
                constraintRoIDefiner.Append(x.Key);
                var patch = GuideDrawingUtilities.CreatePatchOnMeshFromClosedCurveMesh(x.Value.ControlPoints, LowLoDConstraintMesh);
                if (patch == null)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, "One of the guide patch failed to be generated, please adjust the failed patches!");
                }
                else
                {
                    roIPatches.Append(patch);
                }

            });

            _dataContext.NegativePatchTubes.ForEach(x =>
            {
                constraintRoIDefiner.Append(x.Key);
                var patch = GuideDrawingUtilities.CreatePatchOnMeshFromClosedCurveMesh(x.Value.ControlPoints, LowLoDConstraintMesh);
                if (patch == null)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, "One of the guide patch failed to be generated, please adjust the failed patches!");
                }
                else
                {
                    roIPatches.Append(patch);
                }
            });

            if (roIPatches.IsValid) //There can be drawing without patches.
            {
                constraintRoIDefiner.Append(roIPatches);
            }

            return GuideDrawingUtilities.CreateRoiMesh(GuideSurfaceCreationLowLoDBase, constraintRoIDefiner);
        }

        private void PrepareResult()
        {
            ResultOfGuideDrawing = new DrawGuideResult();

            var constraintMeshRoIed = CreateRoIMesh();

            //Create Result
            var positiveSurfaces = new List<PatchData>();
            _dataContext.SkeletonCurves.ForEach(x =>
            {
                var surface = GuideSurfaceUtilities.CreateSkeletonSurface(constraintMeshRoIed,
                    x.Key, x.Value.Diameter / 2);

                if (surface != null && surface.Faces.Any())
                {
                    positiveSurfaces.Add(new PatchData(surface)
                    {
                        GuideSurfaceData = x.Value
                    });
                }
                else
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, "Skeleton Surface failed to be created, please adjust the failed Skeleton design.");
                }
            });

            _dataContext.PositivePatchTubes.ForEach(x =>
            {
                var surfacePatch = GuideSurfaceUtilities.CreatePatch(x.Key, constraintMeshRoIed, true);

                if (surfacePatch != null && surfacePatch.Faces.Any())
                {
                    positiveSurfaces.Add(new PatchData(surfacePatch)
                    {
                        GuideSurfaceData = x.Value
                    });
                }
                else
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, "Positive patch failed to be created, please adjust the failed patch design.");
                }
            });

            var negativeSurfaces = new List<PatchData>();
            _dataContext.NegativePatchTubes.ForEach(x =>
            {
                var surfacePatch = GuideSurfaceUtilities.CreatePatch(x.Key, constraintMeshRoIed, true);

                if (surfacePatch != null && surfacePatch.Faces.Any())
                {
                    negativeSurfaces.Add(new PatchData(surfacePatch)
                    {
                        GuideSurfaceData = x.Value
                    });
                }
                else
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, "Negative patch failed to be created, please adjust the failed patch design.");
                }
            });

            //Add Results
            ResultOfGuideDrawing.GuideBaseSurfaces.AddRange(positiveSurfaces);
            ResultOfGuideDrawing.GuideBaseNegativeSurfaces.AddRange(negativeSurfaces);

            ResultOfGuideDrawing.RoIMesh = constraintMeshRoIed;
        }

        private void OnMouseEnter(MouseCallbackEventArgs view)
        {
            RhinoApp.KeyboardEvent -= OnKeyboard;
            RhinoApp.KeyboardEvent += OnKeyboard;
            CurrentDrawGuideMode.OnMouseEnter(view.View);
        }

        private void OnMouseLeave(MouseCallbackEventArgs view)
        {
            RhinoApp.KeyboardEvent -= OnKeyboard;
            CurrentDrawGuideMode.OnMouseLeave(view.View);
        }

        protected override void OnDynamicDraw(GetPointDrawEventArgs e)
        {
            base.OnDynamicDraw(e);
            RefreshViewPort();

            CurrentDrawGuideMode.OnDynamicDraw(e, this);

            _dataContext?.PositivePatchSurface?.ForEach(x =>
            {
                if (x.Key != null)
                {
                    e.Display.DrawBrepShaded(x.Key, new DisplayMaterial
                    {
                        Transparency = 0.5,
                        Diffuse = _positivePatchColor,
                        Specular = _positivePatchColor,
                        Emission = _positivePatchColor
                    });
                }
            });

            _dataContext?.NegativePatchSurface?.ForEach(x =>
            {
                if (x.Key != null)
                {
                    e.Display.DrawBrepShaded(x.Key, new DisplayMaterial
                    {
                        Transparency = 0.5,
                        Diffuse = _negativePatchColor,
                        Specular = _negativePatchColor,
                        Emission = _negativePatchColor
                    });
                }
            });

        }

        protected override void OnPostDrawObjects(DrawEventArgs e)
        {
            base.OnPostDrawObjects(e);
            RefreshViewPort();

            CurrentDrawGuideMode.OnPostDrawObjects(e, this);

            var skeletonMaterial = new DisplayMaterial
            {
                Transparency = 0.5,
                Diffuse = _positiveSkeletonColor,
                Specular = _positiveSkeletonColor,
                Emission = _positiveSkeletonColor
            };

            foreach (var tube in _dataContext.SkeletonTubes)
            {
                e.Display.DrawBrepShaded(tube.Key, skeletonMaterial);
            }

            var positivePatchMaterial = new DisplayMaterial
            {
                Transparency = 0.5,
                Diffuse = _positivePatchColor,
                Specular = _positivePatchColor,
                Emission = _positivePatchColor
            };

            foreach (var tube in _dataContext.PositivePatchTubes)
            {
                e.Display.DrawMeshShaded(tube.Key, positivePatchMaterial);
            }

            var negativePatchMaterial = new DisplayMaterial
            {
                Transparency = 0.5,
                Diffuse = _negativePatchColor,
                Specular = _negativePatchColor,
                Emission = _negativePatchColor
            };

            foreach (var tube in _dataContext.NegativePatchTubes)
            {
                e.Display.DrawMeshShaded(tube.Key, negativePatchMaterial);
            }
        }

        protected override void OnMouseMove(GetPointMouseEventArgs e)
        {
            RefreshViewPort();

            CurrentDrawGuideMode.OnMouseMove(e, this);
            RefreshViewPort();
        }

        public void RefreshViewPort()
        {
            RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.
                SetCameraLocations(RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraTarget,
                    RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraLocation);
        }

        public void SetToSkeletonDrawing()
        {
            IDSPluginHelper.WriteLine(LogCategory.Default, "Switching to Skeleton Drawing Mode");
            CurrentDrawGuideMode = _skeletonModeBuffer;
        }

        public void SetToPatchDrawing()
        {
            IDSPluginHelper.WriteLine(LogCategory.Default, "Switching to Patch Drawing Mode");
            CurrentDrawGuideMode = _patchModeBuffer;
        }

        private DisplayMaterial CreateTransparentDisplayMaterial(Color color)
        {
            return new DisplayMaterial
            {
                Transparency = 0.75,
                Diffuse = color,
                Specular = color,
                Emission = color
            };
        }
    }
}
