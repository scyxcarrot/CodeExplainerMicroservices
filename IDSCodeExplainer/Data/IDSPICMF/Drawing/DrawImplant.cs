using IDS.CMF;
using IDS.CMF.DataModel;
using IDS.CMF.Enumerators;
using IDS.CMF.Helper;
using IDS.CMF.Operations;
using IDS.CMF.Visualization;
using IDS.Core.Drawing;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Geometry;
using Rhino.UI;
using System;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using DrawMode = IDS.Core.Drawing.DrawMode;

namespace IDS.PICMF.Drawing
{
    public class DrawImplant : IDisposable
    {
        private readonly CMFImplantDirector _implantDirector;
        private DrawImplantBaseState _drawingState;

        private DrawMode _drawMode;
        private DrawMode drawMode
        {
            get { return _drawMode; }
            set
            {
                _drawMode = value;
                switch (drawMode)
                {
                    case DrawMode.Indicate:
                        if (DataModelBase.DotList.Any() || DataModelBase.ConnectionList.Any())
                        {
                            _drawingState = new DrawImplantStateBranchOff(_implantDirector);
                        }
                        else
                        {
                            _drawingState = new DrawImplantStateDraw(_implantDirector);
                        }
                        break;
                    case DrawMode.Delete:
                        _drawingState = new DrawImplantStateRemoveDot(_implantDirector);
                        break;
                    case DrawMode.Move:
                        _drawingState = new DrawImplantStateMove(_implantDirector);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

            }
        }
        private Mesh _lowLoDConstraintMesh;

        public Mesh LowLoDConstraintMesh
        {
            get { return _lowLoDConstraintMesh; }
            set
            {
                _lowLoDConstraintMesh = value;

                if (_drawingState != null)
                {
                    _drawingState.LowLoDConstraintMesh = _lowLoDConstraintMesh;
                }
            }
        }

        public ImplantDataModelBase DataModelBase { get; set; }
        public double ConnectionThickness { get; set; }
        public double PlateWidth { get; set; }
        public double LinkWidth { get; set; }
        public bool CreatePlate { get; set; }
        public bool PlaceScrew { get; set; }
        public bool DoMove { get; set; }
        public int SelectedIndex { get; set; }
        public double PastilleDiameter { get; set; }

        public DrawImplant(CMFImplantDirector director)
        {
            _implantDirector = director;
            LowLoDConstraintMesh = null;

            DataModelBase = new ImplantDataModelBase();

            PlaceScrew = true;
            CreatePlate = true;
            SelectedIndex = -1;

            ConnectionThickness = 0.7;
            PlateWidth = 1.5;
            LinkWidth = 1.0;
            PastilleDiameter = 5.0;

            drawMode = DrawMode.Indicate;
        }

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

        //This should be in the State class!
        protected void OnKeyboard(int key)
        {
            // Only execute if key is down (avoid triggering on key release)
            if (!IsKeyDown(key))
                return;

            switch (key)
            {
                case (76): // L
                    CreatePlate = !CreatePlate;
                    _drawingState.CreatePlate = CreatePlate;
                    break;

                case (80): // P
                    PlaceScrew = !PlaceScrew; 
                    _drawingState.PlaceScrew = PlaceScrew;
                    break;

                case (77): //M
                    DoMove = !DoMove;
                    if (DoMove)
                    {
                        IDSPluginHelper.WriteLine(LogCategory.Default, "MOVE mode is ON.");
                        _drawingState.SetCommandPrompt("Move the points. Press M to toggle MOVE mode OFF, + and - to adjust safety region radius or Press ENTER to finish.");
                        _drawingState.LastSelectedIndexBeforeMoveToggle = _drawingState.SelectedIndex;
                    }
                    else
                    {
                        IDSPluginHelper.WriteLine(LogCategory.Default, "MOVE mode is OFF.");
                        _drawingState.SetCommandPrompt("Place screws and control points (Use P,L to switch). Press M to toggle MOVE mode ON. + and - to adjust safety region radius. Press ENTER to finish.");
                        if (_drawingState.LastSelectedIndexBeforeMoveToggle != -1)
                        {
                            _drawingState.SelectedIndex = _drawingState.LastSelectedIndexBeforeMoveToggle;
                            _drawingState.LastSelectedIndexBeforeMoveToggle = -1;
                        }
                    }

                    _drawingState.DoMove = DoMove;
                    break;
                case (187):
                case (107):
                    double max = StaticValues.SafetyRegionMaxRadius;
                    if (StaticValues.SafetyRegionRadius < max)
                    {
                        StaticValues.SafetyRegionRadius += 0.1;
                        if (StaticValues.SafetyRegionRadius > max) //Possible due to precision
                        {
                            StaticValues.SafetyRegionRadius = max;
                        }
                        IDSPluginHelper.WriteLine(LogCategory.Default, $"Safety region radius increased to {StaticValues.SafetyRegionRadius.ToString("0.0", CultureInfo.InvariantCulture) } mm");
                    }
                    else
                    {
                        StaticValues.SafetyRegionRadius = max;
                        IDSPluginHelper.WriteLine(LogCategory.Error, $"Maximum radius { StaticValues.SafetyRegionRadius.ToString("0.0", CultureInfo.InvariantCulture) } mm reached!");
                    }

                    _implantDirector.Document.Views.Redraw();
                    
                    break;
                case (189):
                case (109):
                    double min = StaticValues.SafetyRegionMinRadius;
                    if (StaticValues.SafetyRegionRadius > min)
                    {
                        StaticValues.SafetyRegionRadius -= 0.1;
                        if (StaticValues.SafetyRegionRadius < min) //Possible due to precision
                        {
                            StaticValues.SafetyRegionRadius = min;
                        }
                        IDSPluginHelper.WriteLine(LogCategory.Default, $"Safety region radius decreased to {StaticValues.SafetyRegionRadius.ToString("0.0", CultureInfo.InvariantCulture) } mm");
                    }
                    else
                    {
                        StaticValues.SafetyRegionRadius = min;
                        IDSPluginHelper.WriteLine(LogCategory.Error, $"Minimum radius { StaticValues.SafetyRegionRadius.ToString("0.0", CultureInfo.InvariantCulture) } mm reached!");
                    }
                    _implantDirector.Document.Views.Redraw();
                    
                    break;
                default:
                    return; // nothing to do
            }

            LogCurrentStates();

            RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.SetCameraLocations(RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraTarget, RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraLocation);
        }

        private void LogCurrentStates()
        {
            switch (drawMode)
            {
                case DrawMode.Indicate:
                case DrawMode.Edit:
                    RhinoApp.WriteLine($"PlaceScrew [Key P]: {PlaceScrew}, CreatePlate [Key L]: {CreatePlate}");
                    break;
                case DrawMode.Delete:
                    break;
                case DrawMode.Move:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void SetExistingImplant(ImplantDataModel dataModel, DrawMode mode)
        {
            DataModelBase.Set(dataModel.DotList, dataModel.ConnectionList);
            this.drawMode = mode;
        }

        public void SetDefaultValues(double defaultConnectionThickness, double defaultPlateWidth, double defaultLinkWidth, double defaultPastilleDiam)
        {
            ConnectionThickness = defaultConnectionThickness;
            PlateWidth = defaultPlateWidth;
            LinkWidth = defaultLinkWidth;
            PastilleDiameter = defaultPastilleDiam;
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

            LogCurrentStates();

            InitializeDrawStateProperties();
            Locking.LockAll(_implantDirector.Document);

            RelativeDotPastilleDistanceConduit distanceConduit = null;
            if (_implantDirector.CurrentDesignPhase == DesignPhase.Implant)
            {
                distanceConduit = new RelativeDotPastilleDistanceConduit(_implantDirector, DataModelBase)
                {
                    Enabled = true
                };
            }

            var success = _drawingState.Execute();

            if (distanceConduit != null)
            {
                distanceConduit.Enabled = false;
            }

            RhinoApp.KeyboardEvent -= OnKeyboard;
            mouseCallback.Enabled = false;
            mouseCallback.MouseEnter -= OnMouseEnter;
            mouseCallback.MouseLeave -= OnMouseLeave;

            _drawingState = null;
            return success;
        }

        private void InitializeDrawStateProperties()
        {
            _drawingState.DataModelBase = DataModelBase;
            _drawingState.LowLoDConstraintMesh = LowLoDConstraintMesh;
            _drawingState.ConnectionThickness = ConnectionThickness;
            _drawingState.PlateWidth = PlateWidth;
            _drawingState.LinkWidth = LinkWidth;
            _drawingState.CreatePlate = CreatePlate;
            _drawingState.PlaceScrew = PlaceScrew;
            _drawingState.SelectedIndex = SelectedIndex;
            _drawingState.PastilleDiameter = PastilleDiameter;
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

        public Brep GetImplantBrep()
        {
            return _drawingState.GetImplantBrep();
        }

        public void Dispose()
        {
            _drawingState.Dispose();
        }
    }
}