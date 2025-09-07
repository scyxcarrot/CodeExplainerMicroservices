using IDS.CMF.Visualization;
using Rhino;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.UI;
using RhinoMtlsCore.Operations;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using IDS.Core.Drawing;
using IDS.Core.Utilities;
using IDS.PICMF.Drawing;
#if (INTERNAL)
using IDS.Core.NonProduction;
#endif

namespace IDS.PICMF.Helper
{

    public class DrawGuideSupportRoI : GetCurvePoints
    {
        public DrawGuideResult ResultOfGuideDrawing { get; set; }

        private readonly DrawGuidePositivePatchMode _patchDrawingMode;

        private Mesh _constraintMesh;
        public Mesh ConstraintMesh
        {
            get { return _constraintMesh; }
            set
            {
                _constraintMesh = value;
                if (value != null)
                {
                    Constrain(value, false);
                }
            }
        }

        public List<Mesh> HighDefinitionMeshes { get; set; }

        private readonly DrawGuideDataContext _dataContext;

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

            _patchDrawingMode.OnKeyboard(key, null);

            switch (key)
            {
                case (79): //O

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

        public DrawGuideSupportRoI(Mesh constraintMesh, DrawGuideDataContext dataContext)
        {
            ConstraintMesh = constraintMesh;

            AcceptString(true);
            AcceptNothing(true); // Pressing ENTER is allowed
            AcceptUndo(true);
            PermitObjectSnap(true); // Only allow our own constraining geometry

            _dataContext = dataContext;

            _patchDrawingMode = new DrawGuidePositivePatchGuideRoIMode(ref _dataContext);
        }

        private int CalculateCreatedSurfaces()
        {
            return _dataContext.PositivePatchSurface.Count;
        }

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

            ResultOfGuideDrawing = null;

            //To detect changes
            var prevTotalSurface = 0;
            var drawCounter = 0;

            while (true)
            {
                this.EnableTransparentCommands(false);
                var rc = this.Get();
                if (rc == GetResult.Point)
                {
                    prevTotalSurface = CalculateCreatedSurfaces();

                    _patchDrawingMode.OnGetPoint(base.Point(), ConstraintMesh, this);

                    var currentTotalSurface = CalculateCreatedSurfaces();
                    if (currentTotalSurface != prevTotalSurface)
                    {
                        drawCounter++;
                    }
                }
                else if (rc == GetResult.Nothing ||
                         rc == GetResult.String && !StringResult().ToLower().Contains("redo"))
                {
                    drawCounter++;
                    bool continueLooping;
                    _patchDrawingMode.OnFinalize(ConstraintMesh, out continueLooping);

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
                            "Pressing Esc will delete the drawings. Do you want to proceed?",
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
                    _patchDrawingMode.OnUndo(ConstraintMesh);
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

        private void PrepareResult()
        {
            ResultOfGuideDrawing = new DrawGuideResult();
            
            var drawnPatchCurve = new List<Curve>();
            
            _dataContext.PositivePatchTubes.ForEach(x =>
            {
                drawnPatchCurve.Add(CurveUtilities.BuildCurve(x.Value.ControlPoints, 1, true));
            });

            var drawPatchBrepSurface = new List<Brep>();

            _dataContext.PositivePatchSurface.ForEach(x =>
            {
                drawPatchBrepSurface.Add(x.Key);
            });

            var roiMeshes = new List<Mesh>();

            drawPatchBrepSurface.ForEach(x =>
            {
                var drawnPatchMesh = MeshUtilities.UnionMeshes(Mesh.CreateFromBrep(x, MeshParameters.IDS()));
#if (INTERNAL)
                InternalUtilities.AddObject(drawnPatchMesh, "Intermediate - The RoI PATCH Original Brep Surface"); //TODO TEMPORARY
#endif
                var offsetVal = 10;
                var offsettedMesh = MeshUtilities.OffsetMesh(new []{ drawnPatchMesh }, offsetVal, 3);

                roiMeshes.Add(offsettedMesh);
#if (INTERNAL)
                InternalUtilities.AddObject(offsettedMesh, "Intermediate - The RoI PATCH Original Brep Surface Offsetted"); //TODO TEMPORARY
#endif
            });

            var roiResultRaw = MeshUtilities.OffsetMesh(roiMeshes.ToArray(), 0.1, 1);

            var roiResult = MeshUtilities.OffsetMesh(new[] { roiResultRaw }, 0.5, 2); // to clean up
#if (INTERNAL)
            InternalUtilities.AddObject(roiResult, "Intermediate - The RoI PATCH Original Brep Surface Offsetted (Cleaned)"); //TODO TEMPORARY
#endif
            //var support = Mesh.CreateBooleanIntersection(HighDefinitionMeshes, new List<Mesh>(){ roiResult });
            var support =
                Booleans.PerformBooleanIntersection(MeshUtilities.AppendMeshes(HighDefinitionMeshes), roiResult);

            ResultOfGuideDrawing.RoIMesh = support;//cleanedSupport;

            drawnPatchCurve.ForEach(x =>
            {
#if (INTERNAL)
                InternalUtilities.AddCurve(x, "Intermediate - The Curve drawn for creating the RoI", System.Drawing.Color.Magenta);
#endif
            });
#if (INTERNAL)
            InternalUtilities.AddObject(ResultOfGuideDrawing.RoIMesh, "Intermediate - The Base of Guide Support Creation (Drawn RoI)"); //TODO TEMPORARY
#endif
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
            _patchDrawingMode.OnMouseEnter(view.View);
        }

        private void OnMouseLeave(MouseCallbackEventArgs view)
        {
            RhinoApp.KeyboardEvent -= OnKeyboard;
            _patchDrawingMode.OnMouseLeave(view.View);
        }

        protected override void OnDynamicDraw(GetPointDrawEventArgs e)
        {
            base.OnDynamicDraw(e);
            RefreshViewPort();

            _patchDrawingMode.OnDynamicDraw(e, this);

            _dataContext.PositivePatchSurface.ForEach(x =>
            {
                e.Display.DrawBrepShaded(x.Key, new DisplayMaterial
                {
                    Transparency = 0.5,
                    Diffuse = Colors.GuidePositivePatchWireframe,
                    Specular = Colors.GuidePositivePatchWireframe,
                    Emission = Colors.GuidePositivePatchWireframe
                });
            });
        }

        protected override void OnPostDrawObjects(DrawEventArgs e)
        {
            base.OnPostDrawObjects(e);
            RefreshViewPort();

            _patchDrawingMode.OnPostDrawObjects(e, this);
            RefreshViewPort();

            var positivePatchMaterial = new DisplayMaterial
            {
                Transparency = 0.5,
                Diffuse = Colors.GuidePositivePatchWireframe,
                Specular = Colors.GuidePositivePatchWireframe,
                Emission = Colors.GuidePositivePatchWireframe
            };

            foreach (var tube in _dataContext.PositivePatchTubes)
            {
                e.Display.DrawMeshShaded(tube.Key, positivePatchMaterial);
            }
        }

        protected override void OnMouseMove(GetPointMouseEventArgs e)
        {
            RefreshViewPort();

            _patchDrawingMode.OnMouseMove(e, this);
        }

        public void RefreshViewPort()
        {
            RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.
                SetCameraLocations(RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraTarget,
                    RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraLocation);
        }

    }
}
