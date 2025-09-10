using IDS.CMF.DataModel;
using IDS.Core.Drawing;
using IDS.Core.Utilities;
using IDS.PICMF.Helper;
using IDS.PICMF.Visualization;
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
    public class EditGuide : GetCurvePoints
    {
        public PatchData ResultOfGuideEdit { get; set; }

        private readonly IEditGuideState _currentEditGuideMode;
        private readonly DrawGuideDataContext _dataContext;
        private readonly ToggleGuideDrawingTransparencyVisualization _guideSupportVisualizationToggler;

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

        public Mesh GuideSurfaceCreationBase { get; }

        public EditGuide(Mesh lowLoDConstraintMesh, Mesh guideSurfaceCreationBase, PatchData surface, ref DrawGuideDataContext dataContext)
        {
            LowLoDConstraintMesh = lowLoDConstraintMesh;
            GuideSurfaceCreationBase = guideSurfaceCreationBase;

            AcceptString(true); // Press ENTER is allowed when contain text like -,+,o in console
            AcceptNothing(true); // Pressing ENTER is allowed
            AcceptUndo(false); // Disables ctrl-z
            PermitObjectSnap(true); // Only allow our own constraining geometry

            _dataContext = dataContext;

            if (surface.GuideSurfaceData is SkeletonSurface data)
            {
                _currentEditGuideMode = new EditGuideSkeletonMode(ref _dataContext, data);
            }
            else
            {
                if (!surface.GuideSurfaceData.IsNegative)
                {
                    _currentEditGuideMode = new EditGuidePositivePatchMode(ref _dataContext, (PatchSurface)surface.GuideSurfaceData);
                }
                else
                {
                    _currentEditGuideMode = new EditGuideNegativePatchMode(ref _dataContext, (PatchSurface)surface.GuideSurfaceData);
                }
            }

            _guideSupportVisualizationToggler = new ToggleGuideDrawingTransparencyVisualization(GuideDrawingTransparencyProxy.IsTransparent);
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

            _currentEditGuideMode.OnKeyboard(key, this);

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
            RhinoDoc.ActiveDoc.Views.Redraw();
        }

        #endregion

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

            ResultOfGuideEdit = null;

            _currentEditGuideMode.OnExecute(this);

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
                    _currentEditGuideMode.OnGetPoint(base.Point(), this);
                }
                else if (rc == GetResult.Nothing || rc == GetResult.String)
                {
                    bool continueLooping;
                    _currentEditGuideMode.OnFinalize(this, out continueLooping);
                    
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
                else
                {
                    var helper = new TransparentCommandHelper();
                    helper.HandleGuideDrawingTransparentCommands(this);
                }
            }
            
            PrepareResult();

            RhinoApp.KeyboardEvent -= OnKeyboard;
            mouseCallback.Enabled = false;
            mouseCallback.MouseEnter -= OnMouseEnter;
            mouseCallback.MouseLeave -= OnMouseLeave;

            return true;
        }

        private void PrepareResult()
        {
            //only edit one at the moment
            if (_dataContext.SkeletonSurfaces.Any())
            {
                ResultOfGuideEdit = _dataContext.SkeletonSurfaces.First();
            }
            else if (_dataContext.PatchSurfaces.Any())
            {
                ResultOfGuideEdit = _dataContext.PatchSurfaces.First();
            }
            else if (_dataContext.NegativePatchSurfaces.Any())
            {
                ResultOfGuideEdit = _dataContext.NegativePatchSurfaces.First();
            }
        }

        private void OnMouseEnter(MouseCallbackEventArgs view)
        {
            RhinoApp.KeyboardEvent -= OnKeyboard;
            RhinoApp.KeyboardEvent += OnKeyboard;
            _currentEditGuideMode.OnMouseEnter(view.View, this);
        }

        private void OnMouseLeave(MouseCallbackEventArgs view)
        {
            RhinoApp.KeyboardEvent -= OnKeyboard;
            _currentEditGuideMode.OnMouseLeave(view.View, this);
        }

        protected override void OnDynamicDraw(GetPointDrawEventArgs e)
        {
            base.OnDynamicDraw(e);
            RefreshViewPort();

            _currentEditGuideMode.OnDynamicDraw(e, this);
        }

        protected override void OnPostDrawObjects(DrawEventArgs e)
        {
            base.OnPostDrawObjects(e);
            RefreshViewPort();

            _currentEditGuideMode.OnPostDrawObjects(e, this);
        }

        protected override void OnMouseMove(GetPointMouseEventArgs e)
        {
            RefreshViewPort();

            _currentEditGuideMode.OnMouseMove(e, this);
        }

        public void RefreshViewPort()
        {
            RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.
                SetCameraLocations(RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraTarget,
                    RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraLocation);
        }
    }
}
