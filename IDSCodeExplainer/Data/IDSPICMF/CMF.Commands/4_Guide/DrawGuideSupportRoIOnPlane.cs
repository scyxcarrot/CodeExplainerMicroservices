using Rhino;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.UI;
using RhinoMtlsCore.Operations;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using IDS.CMF.Visualization;
using IDS.Core.Drawing;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
#if (INTERNAL)
using IDS.Core.NonProduction;
#endif

namespace IDS.PICMF.Helper
{
    public class DrawGuideSupportRoIOnPlane
    {
        private readonly DrawGuideOnPlaneDataContext _dataContext;

        public Mesh RoIMesh { get; set; }

        public Mesh OperationConstraintMesh { get; set; }

        private readonly RhinoDoc _doc;
        private readonly Mesh _boundingBoxMesh;

        public DrawGuideSupportRoIOnPlane(RhinoDoc doc, Mesh constraintMesh)
        {
            OperationConstraintMesh = constraintMesh;
            var boundingBox = OperationConstraintMesh.GetBoundingBox(true);
            _boundingBoxMesh = Mesh.CreateFromBox(boundingBox, 100, 100, 100);

            _dataContext = new DrawGuideOnPlaneDataContext();
            _dataContext.PreviewMesh = constraintMesh;
            _dataContext.GetInnerMesh = true;

            _doc = doc;
        }

        public bool Execute()
        {
            if (OperationConstraintMesh == null)
            {
                return false;
            }

            RhinoApp.KeyboardEvent += OnKeyboard;

            var mouseCallback = new IDSMouseCallback { Enabled = true };
            mouseCallback.MouseEnter += OnMouseEnter;
            mouseCallback.MouseLeave += OnMouseLeave;

            LogCurrentStates();

            var success = Executing();

            RhinoApp.KeyboardEvent -= OnKeyboard;
            mouseCallback.Enabled = false;
            mouseCallback.MouseEnter -= OnMouseEnter;
            mouseCallback.MouseLeave -= OnMouseLeave;

            return success;
        }

        private bool Executing()
        {
            RoIMesh = null;

            var conduit = new DrawGuideOnPlaneConduit(_dataContext);
            conduit.Enabled = true;
            _doc.Views.Redraw();

            while (true)
            {
                var getNext = new GetPoint();
                getNext.Constrain(_boundingBoxMesh, true);
                getNext.SetCommandPrompt("LMB to start drawing curve, Press <Enter> to finalize RoI for current bone, <Esc> to discard changes to current bone.");
                getNext.AcceptNothing(true); // accept ENTER to confirm

                var result = getNext.Get();
                //GetResult.Nothing - user pressed enter 
                //GetResult.Cancel - user cancel string getting
                if (result == GetResult.Nothing)
                {
                    break;
                }
                else if (result == GetResult.Cancel)
                {
                    if (_dataContext.ContainsDrawing())
                    {
                        var dlgRes = MessageBox.Show(
                            "Pressing Esc will delete the drawings. Do you want to proceed?",
                            "Drawing Surface", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);

                        if (dlgRes == DialogResult.OK)
                        {
                            conduit.Enabled = false;
                            conduit.CleanUp();
                            return false;
                        }

                        continue;
                    }

                    conduit.Enabled = false;
                    conduit.CleanUp();
                    return false;
                }

                var drawer = new DrawCurveOnPlane(_doc, OperationConstraintMesh, getNext.Point());
                conduit.Drawer = drawer;
                var contour = drawer.Draw();
                conduit.Drawer = null;
                RhinoApp.SetFocusToMainWindow(_doc);
                RhinoApp.KeyboardEvent -= OnKeyboard;
                RhinoApp.KeyboardEvent += OnKeyboard;
                if (contour == null || !contour.IsClosed)
                {
                    continue;
                }

                var data = new GuideOnPlaneData(contour, drawer.GetConstraintPlane(), drawer.GetPointList());

                if (PrepareResult(data))
                {
                    _dataContext.Surfaces.Add(data);
                }
            }

            conduit.Enabled = false;
            conduit.CleanUp();
            return _dataContext.ContainsDrawing();
        }

        private bool PrepareResult(GuideOnPlaneData data)
        {
            Mesh outputMesh = null;

            try
            {
                var ops = new TrimIntoHalves();
                outputMesh = _dataContext.GetInnerMesh ?
                    ops.PerformTrimToGetInnerMesh(OperationConstraintMesh, data.PointList, data.Plane) :
                    ops.PerformTrimToGetOuterMesh(OperationConstraintMesh, data.PointList, data.Plane);
            }
            catch
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Error while trim processing...");
                return false;
            }

            if (!outputMesh.Vertices.Any() || !outputMesh.Faces.Any())
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "No parts left after trimmed, please trim again the mesh");
                return false;
            }

#if (INTERNAL)
            InternalUtilities.AddCurve(data.Contour, $"Contour", "Intermediate - TrimEntities", Color.Red);  //temporary
#endif

            RoIMesh = outputMesh;
#if (INTERNAL)
            InternalUtilities.ReplaceObject(RoIMesh, "Intermediate - RoIMesh from TrimEntities"); //temporary
#endif

            OperationConstraintMesh = outputMesh;
            _dataContext.PreviewMesh = outputMesh;
            return true;
        }

        #region Keyboard, Mouse, Logging

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
                return;

            switch (key)
            {
                case (86): // V
                    _dataContext.GetInnerMesh = !_dataContext.GetInnerMesh;
                    break;
                default:
                    return; // nothing to do
            }

            LogCurrentStates();
        }

        private void LogCurrentStates()
        {
            RhinoApp.WriteLine($"GetInnerMesh [Key V]: {_dataContext.GetInnerMesh}");
        }

        private void OnMouseEnter(MouseCallbackEventArgs e)
        {
            RhinoApp.KeyboardEvent -= OnKeyboard;
            RhinoApp.KeyboardEvent += OnKeyboard;
        }

        private void OnMouseLeave(MouseCallbackEventArgs e)
        {
            RhinoApp.KeyboardEvent -= OnKeyboard;
        }

        #endregion
    }
}