using IDS.Core.Drawing;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.UI;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace IDS.PICMF.Drawing
{
    public abstract class DrawSurface : GetCurvePoints
    {
        protected IDrawSurfaceState CurrentDrawSurfaceMode;
        private readonly DrawSurfaceDataContext _dataContext;

        private readonly Color _positivePatchColor = System.Drawing.Color.Red;
        private readonly Color _skeletonColor = System.Drawing.Color.Yellow;
        private Mesh _constraintMesh;
        public Mesh ConstraintMesh
        {
            get => _constraintMesh;
            set
            {
                _constraintMesh = value;
                if (value != null)
                {
                    Constrain(value, false);
                }
            }
        }

        protected DrawSurface(
            Mesh constraintMesh, 
            DrawSurfaceDataContext dataContext,
            IDrawSurfaceState drawSurfaceMode)
        {
            ConstraintMesh = constraintMesh;
            AcceptString(true); // Press ENTER is allowed when contain text like -,+,o in console
            AcceptNothing(true); // Pressing ENTER is allowed
            AcceptUndo(true);
            PermitObjectSnap(true); // Only allow our own constraining geometry

            _dataContext = dataContext;

            CurrentDrawSurfaceMode = drawSurfaceMode;
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

            CurrentDrawSurfaceMode.OnKeyboard(key, this);

            switch (key)
            {
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
            return _dataContext.PositivePatchSurface.Count + _dataContext.SkeletonCurves.Count;
        }

        public bool Execute()
        {
            if (ConstraintMesh == null)
            {
                return false;
            }

            var mouseCallback = RegisterEvents();
            var executionResult = ExecuteDrawingLoop();

            UnregisterEvents(mouseCallback);

            return executionResult && FinalizeExecution();
        }

        private IDSMouseCallback RegisterEvents()
        {
            RhinoApp.KeyboardEvent += OnKeyboard;
            var mouseCallback = new IDSMouseCallback { Enabled = true };
            mouseCallback.MouseEnter += OnMouseEnter;
            mouseCallback.MouseLeave += OnMouseLeave;
            return mouseCallback;
        }

        private bool ExecuteDrawingLoop()
        {
            var prevTotalSurface = 0;
            var drawCounter = 0;

            while (true)
            {
                EnableTransparentCommands(false);
                var rc = Get();

                if (HandleStringResult(rc))
                    continue;

                if (HandlePointResult(rc, ref prevTotalSurface, ref drawCounter))
                    continue;

                if (HandleNothingOrStringResult(rc, ref drawCounter))
                    break;

                if (HandleCancelResult(rc, drawCounter))
                    return false;

                if (HandleUndoResult(rc))
                    continue;
            }

            return true;
        }

        private bool HandleStringResult(GetResult rc)
        {
            if (rc != GetResult.String)
                return false;

            var stringRes = StringResult().ToLower();
            return stringRes.Contains("redo") || stringRes.Contains("/_copy");
        }

        private bool HandlePointResult(GetResult rc, ref int prevTotalSurface, ref int drawCounter)
        {
            if (rc != GetResult.Point)
                return false;

            prevTotalSurface = CalculateCreatedSurfaces();
            CurrentDrawSurfaceMode.OnGetPoint(Point(), ConstraintMesh, this);

            var currentTotalSurface = CalculateCreatedSurfaces();
            if (currentTotalSurface != prevTotalSurface)
            {
                drawCounter++;
            }

            return true;
        }

        private bool HandleNothingOrStringResult(GetResult rc, ref int drawCounter)
        {
            if (rc != GetResult.Nothing && rc != GetResult.String)
                return false;

            drawCounter++;
            CurrentDrawSurfaceMode.OnFinalize(ConstraintMesh, out bool continueLooping);
            return !continueLooping;
        }

        private bool HandleCancelResult(GetResult rc, int drawCounter)
        {
            if (rc != GetResult.Cancel)
                return false;

            if (drawCounter > 0)
            {
                var dlgRes = MessageBox.Show(
                    "Pressing Esc will delete the surfaces that you have drawn and are currently drawing. Do you want to proceed?",
                    "Drawing Surface", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);

                return dlgRes == DialogResult.OK;
            }

            return true;
        }

        private bool HandleUndoResult(GetResult rc)
        {
            if (rc != GetResult.Undo)
                return false;

            CurrentDrawSurfaceMode.OnUndo(ConstraintMesh);
            RefreshViewPort();
            return true;
        }

        private bool FinalizeExecution()
        {
            if (!_dataContext.ContainsDrawing())
                return false;

            PrepareResult();
            return true;
        }

        private void UnregisterEvents(IDSMouseCallback mouseCallback)
        {
            RhinoApp.KeyboardEvent -= OnKeyboard;
            mouseCallback.Enabled = false;
            mouseCallback.MouseEnter -= OnMouseEnter;
            mouseCallback.MouseLeave -= OnMouseLeave;
        }

        private void OnMouseEnter(MouseCallbackEventArgs view)
        {
            RhinoApp.KeyboardEvent -= OnKeyboard;
            RhinoApp.KeyboardEvent += OnKeyboard;
            CurrentDrawSurfaceMode.OnMouseEnter(view.View);
        }

        private void OnMouseLeave(MouseCallbackEventArgs view)
        {
            RhinoApp.KeyboardEvent -= OnKeyboard;
            CurrentDrawSurfaceMode.OnMouseLeave(view.View);
        }

        protected override void OnDynamicDraw(GetPointDrawEventArgs e)
        {
            base.OnDynamicDraw(e);
            RefreshViewPort();

            CurrentDrawSurfaceMode.OnDynamicDraw(e, this);

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

            _dataContext?.SkeletonTubes?.ForEach(x =>
            {
                if (x.Key != null)
                {
                    e.Display.DrawBrepShaded(x.Key, new DisplayMaterial
                    {
                        Transparency = 0.5,
                        Diffuse = _skeletonColor,
                        Specular = _skeletonColor,
                        Emission = _skeletonColor
                    });
                }
            });
        }

        protected override void OnPostDrawObjects(DrawEventArgs e)
        {
            base.OnPostDrawObjects(e);
            RefreshViewPort();

            CurrentDrawSurfaceMode.OnPostDrawObjects(e, this);

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

            var curvesToDraw = _dataContext.SkeletonCurves
                .Select(skeletonCurve => skeletonCurve.Key)
                .SelectMany(curves => curves);
            foreach (var curve in curvesToDraw)
            {
                e.Display.DrawCurve(curve, _skeletonColor);
            }
        }

        protected override void OnMouseMove(GetPointMouseEventArgs e)
        {
            RefreshViewPort();

            CurrentDrawSurfaceMode.OnMouseMove(e, this);
            RefreshViewPort();
        }

        public static void RefreshViewPort()
        {
            RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.
                SetCameraLocations(RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraTarget,
                    RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraLocation);
        }

        protected abstract void PrepareResult();
    }
}
