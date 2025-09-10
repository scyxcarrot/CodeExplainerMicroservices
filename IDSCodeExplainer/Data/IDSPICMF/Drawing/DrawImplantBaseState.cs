using IDS.CMF;
using IDS.CMF.DataModel;
using IDS.CMF.Utilities;
using IDS.Core.Utilities;
using IDS.Interface.Implant;
using IDS.PICMF.DrawingAction;
using IDS.PICMF.Helper;
using IDS.RhinoInterfaces.Converter;
using Rhino;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.Drawing
{
    public abstract class DrawImplantBaseState : GetCurvePoints, IUiImplantManipulatorState
    {
        protected CMFImplantDirector ImplantDirector;
        protected ImplantConduit Conduit;
        public ImplantDataModelBase DataModelBase { get; set; }

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

        //Kind of a hack :(
        public int LastSelectedIndexBeforeMoveToggle { get; set; }
        public double ConnectionThickness { get; set; }
        public double PlateWidth { get; set; }
        public double LinkWidth { get; set; }
        public bool CreatePlate { get; set; }
        public bool PlaceScrew { get; set; }
        public bool DoMove { get; set; }
        public int SelectedIndex { get; set; }
        public double PastilleDiameter { get; set; }

        ////////////////////////////////////////////////////

        protected DrawImplantBaseState(CMFImplantDirector director)
        {
            AcceptNothing(true);
            AcceptNothing(true); // Pressing ENTER is allowed
            AcceptUndo(false); // Disables ctrl-z
            PermitObjectSnap(true); // Only allow our own constraining geometry
            PermitElevatorMode(0); // Disables ctrl key elevation

            ImplantDirector = director;
            Conduit = new ImplantConduit(director);
        }

        public bool Execute()
        {
            AcceptString(true);

            var result = OnExecute();

            if (result)
            {
                OnExecuteSuccess();
            }
            else
            {
                OnExecuteFail();
            }

            return result;
        }

        protected abstract bool OnExecute();
        protected abstract void OnExecuteSuccess();
        protected abstract void OnExecuteFail();

        public ImplantDataModelBase GetImplantDataModelResult()
        {
            return DataModelBase;
        }

        public void SetBaseImplantData(ImplantDataModelBase dataModel)
        {
            DataModelBase = dataModel;
        }

        ////////////////////////////////////////////////////
        
        protected bool HandleAddControlPoint(Point3d point)
        {
            var action = new InsertControlPointAction();
            action.ConstraintMesh = LowLoDConstraintMesh;
            action.PointToInsert = point;
            var handled = action.Do(this);
            return handled;
        }

        protected bool HandleDeleteControlPoint(Point3d point)
        {
            var action = new DeleteControlPointAction();
            action.PointToDelete = point;
            var handled = action.Do(this);
            return handled;
        }

        protected bool HandleAddScrew(Point3d point)
        {
            var action = new InsertScrewAction();
            action.ConstraintMesh = LowLoDConstraintMesh;
            action.ScrewToInsert = point;
            action.PastilleDiameter = PastilleDiameter;
            var handled = action.Do(this);
            return handled;
        }

        protected int GetNearestPointIndex(Point3d selectedPoint, double maxDistance)
        {
            if (!DataModelBase.DotList.Any())
            {
                return -1;
            }

            var ordered = DataModelBase.DotList.Select((p, i) => 
                new { length = (selectedPoint - RhinoPoint3dConverter.ToPoint3d(p.Location)).Length, index = i }).OrderBy(o => o.length).ToList();
            if (ordered.First().length < maxDistance)
            {
                return ordered.First().index;
            }

            return -1;
        }

        protected int GetPickedPointIndex(System.Drawing.Point selectedPoint)
        {
            return PickUtilities.GetPickedPoint3DIndexFromPoint2d(selectedPoint, DataModelBase.DotList.Select(d => d.Location));
        }

        protected IDot CreateDot(Point3d point, Vector3d normal)
        {
            normal.Unitize();

            //factory
            if (PlaceScrew)
            {
                return DataModelUtilities.CreateDotPastille(point, normal, ConnectionThickness, PastilleDiameter);
            }
            else
            {
                return DataModelUtilities.CreateDotControlPoint(point, normal);
            }
        }
        
        protected List<IConnection> FindAffectedConnections(Point3d pt)
        {
            const double epsilon = 0.0001;
            return DataModelBase.ConnectionList.FindAll(x => RhinoPoint3dConverter.ToPoint3d(x.A.Location).EpsilonEquals(pt, epsilon)
                                                             || RhinoPoint3dConverter.ToPoint3d(x.B.Location).EpsilonEquals(pt, epsilon));
        }

        protected void InvalidatePreviewData()
        {
            Conduit.DotPreview = DataModelBase.DotList.ToList();
            Conduit.ConnectionPreview = DataModelBase.ConnectionList.ToList();
            Conduit.PastilleDiameter = PastilleDiameter;
        }

        public void RefreshViewPort()
        {
            RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.
                SetCameraLocations(RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraTarget,
                    RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraLocation);
        }

        public Brep GetImplantBrep()
        {
            return Conduit.GeneratePlanningBrep();
        }

        protected void OnHandleTransparentCommands()
        {
            var helper = new TransparentCommandHelper();
            helper.HandleTransparentCommands(this);
        }
    }
}
