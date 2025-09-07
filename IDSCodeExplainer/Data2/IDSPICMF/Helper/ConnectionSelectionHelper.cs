using IDS.CMF;
using IDS.CMF.DataModel;
using IDS.CMF.Utilities;
using IDS.Interface.Implant;
using IDS.PICMF.Visualization;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace IDS.PICMF.Helper
{
    public class ConnectionSelectionHelper : GetPoint
    {
        protected CMFImplantDirector ImplantDirector;
        protected ImplantConnectionConduit Conduit;
        public ImplantDataModelBase DataModelBase { get; set; }
        private List<List<IConnection>> selectedConnections;

        public ConnectionSelectionHelper(CMFImplantDirector director)
        {
            AcceptNothing(true);
            AcceptNothing(true); // Pressing ENTER is allowed
            AcceptUndo(false); // Disables ctrl-z

            ImplantDirector = director;
            SetCommandPrompt("Select Plate/Link to edit width. Shift to deselect. Press ENTER to finish.");

            DataModelBase = new ImplantDataModelBase();
            selectedConnections = new List<List<IConnection>>();

            Conduit = new ImplantConnectionConduit();
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

        public List<List<IConnection>> GetConnectionListResult()
        {
            return selectedConnections;
        }

        public void SetExistingImplant(ImplantDataModel dataModel)
        {
            DataModelBase.Set(dataModel.DotList, dataModel.ConnectionList);
        }

        protected bool OnExecute()
        {
            Conduit.SetDotsAndConnections(DataModelBase.DotList, DataModelBase.ConnectionList);
            Conduit.Enabled = true;

            while (true)
            {
                base.EnableTransparentCommands(false);
                var rc = base.Get();

                if (rc == GetResult.Point)
                {
                    var pickedMesh = PickConnection(base.Point2d());
                    if (pickedMesh == null)
                    {
                        continue;
                    }

                    if (Control.ModifierKeys == Keys.Shift)
                    {
                        //deselect
                        Conduit.DeselectConnection(pickedMesh);
                    }
                    else
                    {
                        //select
                        Conduit.SelectConnection(pickedMesh);
                    }
                }
                else if (rc == GetResult.Nothing)
                {
                    break; // User pressed ENTER
                }
                else if (rc == GetResult.Cancel)
                {
                    return false;
                }
                else
                {
                    var helper = new TransparentCommandHelper();
                    helper.HandleTransparentCommands(this);
                }
            }

            selectedConnections = new List<List<IConnection>>();
            foreach (var connection in Conduit.SelectedConnections)
            {
                selectedConnections.Add(Conduit.ConnectionDictionary[connection]);
            }

            return true;
        }

        protected void OnExecuteSuccess()
        {
            Conduit.Enabled = false;
        }

        protected void OnExecuteFail()
        {
            Conduit.Enabled = false;
        }

        protected override void OnMouseMove(GetPointMouseEventArgs e)
        {
            if (!e.LeftButtonDown)
            {
                var pickedMesh = PickConnection(e.WindowPoint);
                Conduit.HighlightedConnection = pickedMesh;

                ConduitUtilities.RefeshConduit();
            }
        }

        private Mesh PickConnection(System.Drawing.Point point2d)
        {
            var viewPort = View().ActiveViewport;

            var picker = new PickContext();
            picker.View = viewPort.ParentView;
            picker.PickStyle = PickStyle.PointPick;
            var xform = viewPort.GetPickTransform(point2d);
            picker.SetPickTransform(xform);

            double depth = 0;
            Mesh selectedMesh = null;
            var refDepth = depth;

            foreach (var mesh in Conduit.ConnectionDictionary.Keys)
            {
                double distance;
                Point3d hitPoint;
                PickContext.MeshHitFlag hitFlag;
                int hitIndex;
                if (!picker.PickFrustumTest(mesh, PickContext.MeshPickStyle.ShadedModePicking, out hitPoint,
                        out depth, out distance, out hitFlag, out hitIndex) ||
                    !(Math.Abs(distance) < double.Epsilon) ||
                    selectedMesh != null && !(refDepth < depth))
                {
                    continue;
                }
                //depth returned here for point picks LARGER values are NEARER to the camera. SMALLER values are FARTHER from the camera.
                selectedMesh = mesh;
                refDepth = depth;
            }

            return selectedMesh;
        }
    }
}