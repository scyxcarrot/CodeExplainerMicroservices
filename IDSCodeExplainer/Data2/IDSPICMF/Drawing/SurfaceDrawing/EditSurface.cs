using IDS.CMF.DataModel;
using IDS.Core.Drawing;
using IDS.Core.Utilities;
using IDS.Core.V2.Common;
using Rhino;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.UI;
using System.Linq;
using System.Runtime.InteropServices;

namespace IDS.PICMF.Drawing
{
    public class EditSurface : GetCurvePoints
    {
        public PatchData ResultOfSurfaceEdit { get; set; }

        public IEditSurfaceState CurrentEditSurfaceMode;
        private readonly DrawSurfaceDataContext _dataContext;

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

        public EditSurface(
            Mesh constraintMesh,
            PatchData surface,
            ref DrawSurfaceDataContext dataContext)
        {
            ConstraintMesh = constraintMesh;

            AcceptString(true); // Press ENTER is allowed when contain text like -,+ in console
            AcceptNothing(true); // Pressing ENTER is allowed
            AcceptUndo(false); // Disables ctrl-z
            PermitObjectSnap(true); // Only allow our own constraining geometry

            _dataContext = dataContext;

            switch (surface.GuideSurfaceData)
            {
                case SkeletonSurface skeletonSurface:
                    CurrentEditSurfaceMode = new EditSurfaceSkeletonMode(
                        ref _dataContext, skeletonSurface);
                    break;
                case PatchSurface patchSurface:
                    CurrentEditSurfaceMode = new EditSurfacePatchMode(
                        ref _dataContext, patchSurface);
                    break;
                default:
                    throw new IDSExceptionV2(
                        $"Invalid GuideSurfaceData = {surface.GuideSurfaceData.SerializationLabel}");
            }
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

            CurrentEditSurfaceMode.OnKeyboard(key, this);

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
            RhinoDoc.ActiveDoc.Views.Redraw();
        }

        #endregion

        public bool Execute()
        {
            if (ConstraintMesh == null)
            {
                return false;
            }

            RhinoApp.KeyboardEvent += OnKeyboard;

            var mouseCallback = new IDSMouseCallback { Enabled = true };
            mouseCallback.MouseEnter += OnMouseEnter;
            mouseCallback.MouseLeave += OnMouseLeave;

            ResultOfSurfaceEdit = null;

            CurrentEditSurfaceMode.OnExecute(this);

            while (true)
            {
                EnableTransparentCommands(false);
                var rc = Get();

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
                    CurrentEditSurfaceMode.OnGetPoint(base.Point(), this);
                }
                else if (rc == GetResult.Nothing || rc == GetResult.String)
                {
                    CurrentEditSurfaceMode.OnFinalize(this, out _);
                    break; // User pressed ENTER
                }
                else if (rc == GetResult.Cancel)
                {
                    RhinoApp.KeyboardEvent -= OnKeyboard;
                    mouseCallback.Enabled = false;
                    mouseCallback.MouseEnter -= OnMouseEnter;
                    mouseCallback.MouseLeave -= OnMouseLeave;

                    return false;
                }
                else if (rc == GetResult.Undo)
                {
                    // Ignore
                }
            }

            UpdateResult();

            RhinoApp.KeyboardEvent -= OnKeyboard;
            mouseCallback.Enabled = false;
            mouseCallback.MouseEnter -= OnMouseEnter;
            mouseCallback.MouseLeave -= OnMouseLeave;

            return true;
        }

        private void UpdateResult()
        {
            //only edit one at the moment
            if (_dataContext.SkeletonSurfaces.Any())
            {
                ResultOfSurfaceEdit = _dataContext.SkeletonSurfaces[0];
            }
            else if (_dataContext.PatchSurfaces.Any())
            {
                ResultOfSurfaceEdit = _dataContext.PatchSurfaces[0];
            }
        }

        private void OnMouseEnter(MouseCallbackEventArgs view)
        {
            RhinoApp.KeyboardEvent -= OnKeyboard;
            RhinoApp.KeyboardEvent += OnKeyboard;
            CurrentEditSurfaceMode.OnMouseEnter(view.View, this);
        }

        private void OnMouseLeave(MouseCallbackEventArgs view)
        {
            RhinoApp.KeyboardEvent -= OnKeyboard;
            CurrentEditSurfaceMode.OnMouseLeave(view.View, this);
        }

        protected override void OnDynamicDraw(GetPointDrawEventArgs e)
        {
            base.OnDynamicDraw(e);
            RefreshViewPort();

            CurrentEditSurfaceMode.OnDynamicDraw(e, this);
        }

        protected override void OnPostDrawObjects(DrawEventArgs e)
        {
            base.OnPostDrawObjects(e);
            RefreshViewPort();

            CurrentEditSurfaceMode.OnPostDrawObjects(e, this);
        }

        protected override void OnMouseMove(GetPointMouseEventArgs e)
        {
            RefreshViewPort();

            CurrentEditSurfaceMode.OnMouseMove(e, this);
        }

        public void RefreshViewPort()
        {
            RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.
                SetCameraLocations(RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraTarget,
                    RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraLocation);
        }
    }
}
