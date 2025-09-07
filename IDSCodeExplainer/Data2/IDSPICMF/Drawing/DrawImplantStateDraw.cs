using IDS.CMF;
using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.Helper;
using IDS.CMF.Utilities;
using IDS.CMF.V2.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Interface.Implant;
using IDS.PICMF.DrawingAction;
using IDS.RhinoInterfaces.Converter;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace IDS.PICMF.Drawing
{
    public class DrawImplantStateDraw : DrawImplantBaseState
    {
        private bool previewFollowMouseCursor;
        private readonly List<IUndoableAction> _undoList;

        public DrawImplantStateDraw(CMFImplantDirector implantDirector) : base(implantDirector)
        {
            LastSelectedIndexBeforeMoveToggle = -1;
            DoMove = false;
            previewFollowMouseCursor = false;
            _undoList = new List<IUndoableAction>();
            AcceptUndo(true); // Enable ctrl-z
            SetCommandPrompt(
                "Place screws and control points (Use P,L to switch). Press M to toggle MOVE mode ON. + and - to adjust safety region radius. Press ENTER to finish.");
        }

        protected override bool OnExecute()
        {
            Conduit = new ImplantConduit(ImplantDirector) {Enabled = true};
            InvalidatePreviewData();
            previewFollowMouseCursor = true;

            IDSPluginHelper.WriteLine(LogCategory.Default, $"Safety region radius = {StaticValues.SafetyRegionRadius}");

            while (true)
            {
                base.EnableTransparentCommands(false);
                var rc = base.Get();

                if (rc == GetResult.Point)
                {
                    if (LowLoDConstraintMesh == null)
                    { 
                        continue;
                    }

                    var point2d = base.Point2d();
                    var nearestIndex = GetPickedPointIndex(point2d);

                    if (Control.ModifierKeys == Keys.Alt)
                    {
                        if (nearestIndex != -1)
                        {
                            IDSPluginHelper.WriteLine(LogCategory.Default, "Draw point base has changed!");
                            SelectedIndex = nearestIndex;
                        }
                        else
                        {
                            IDSPluginHelper.WriteLine(LogCategory.Default, "No drawing point on selected location, please click near to any existing points!");
                        }
                        continue;
                    }
                    else if (Control.ModifierKeys == Keys.Shift)
                    {
                        if (nearestIndex == -1)
                        {
                            var point = base.Point();
                            HandleUndoableAddControlPoint(point);
                        }
                        else
                        {
                            var point = RhinoPoint3dConverter.ToPoint3d(DataModelBase.DotList[nearestIndex].Location);
                            HandleUndoableDeleteControlPoint(point);
                        }

                        InvalidatePreviewData();
                        continue;
                    }
                    else if (Control.ModifierKeys == Keys.Control)
                    {
                        if (nearestIndex == -1)
                        {
                            var point = base.Point();
                            HandleUndoableAddScrew(point);
                        }

                        InvalidatePreviewData();
                        continue;
                    }

                    if (LowLoDConstraintMesh == null || DoMove)
                    {
                        SelectedIndex = nearestIndex;
                        continue;
                    }

                    IDot dot;
                    if (nearestIndex != -1)
                    {
                        dot = DataModelBase.DotList[nearestIndex];
                    }
                    else if (DoMove)
                    {
                        continue;
                    }
                    else
                    {
                        var point = base.Point();
                        var meshPoint = LowLoDConstraintMesh.ClosestMeshPoint(point, ImplantCreation.DotMeshDistancePullTolerance);
                        var averageNormal = VectorUtilities.FindAverageNormal(LowLoDConstraintMesh, meshPoint.Point, 
                            PlaceScrew?ScrewAngulationConstants.AverageNormalRadiusPastille: ScrewAngulationConstants.AverageNormalRadiusControlPoint);

                        dot = CreateDot(meshPoint.Point, averageNormal);
                    }

                    if (HandleUndoableAddDot(dot)) //returns false if the dot created is the same dot
                    {
                        //Closing a loop
                        if (nearestIndex != -1)
                        {
                            SelectedIndex = nearestIndex;
                            InvalidatePreviewData();
                            continue;
                        }

                        SelectedIndex = DataModelBase.DotList.IndexOf(dot);

                        return true;
                    }

                    SelectedIndex = DataModelBase.DotList.IndexOf(dot);
                }
                else if (rc == GetResult.Nothing)
                {
                    break; // User pressed ENTER
                }
                else if (rc == GetResult.Cancel)
                {
                    return false;
                }
                else if (rc == GetResult.Undo)
                {
                    Undo();
                }
                else
                {
                    OnHandleTransparentCommands();
                }
            }

            return true;
        }

        protected override void OnExecuteSuccess()
        {
            Conduit.Enabled = false;
        }

        protected override void OnExecuteFail()
        {
            Conduit.Enabled = false;
        }

        private bool _updatePreviewData = true;

        protected override void OnDynamicDraw(GetPointDrawEventArgs e)
        {
            base.OnDynamicDraw(e);

            if (DoMove)
            {
                //update conduit
                Conduit.SetLineColorToDefault();

                if (_updatePreviewData)
                {
                    InvalidatePreviewData();
                }

                RefreshViewPort();
                _updatePreviewData = false;

                return;
            }

            Conduit.SetLineColorToDefault();
            var currentPoint = e.CurrentPoint;
            var currentDot = CreateDot(currentPoint, Vector3d.Zero);

            //mouse move
            //do not draw if in edit and have not select first point
            if (previewFollowMouseCursor && Control.ModifierKeys != Keys.Shift && Control.ModifierKeys != Keys.Control)
            {
                if (DataModelBase.DotList.Count > 0)
                {
                    Conduit.PointPreview = currentDot;

                    var width = CreatePlate ? PlateWidth : LinkWidth;
                    if (SelectedIndex != -1 && SelectedIndex < DataModelBase.DotList.Count)
                    {
                        Conduit.LinePreview = ImplantCreationUtilities.
                            CreateConnection(DataModelBase.DotList[SelectedIndex], currentDot, ConnectionThickness, width, CreatePlate);
                    }
                    else if (SelectedIndex >= DataModelBase.DotList.Count)
                    {
                        if (LastSelectedIndexBeforeMoveToggle == -1)
                        {
                            SelectedIndex = DataModelBase.DotList.Count - 1;
                        }
                        else
                        {
                            SelectedIndex = LastSelectedIndexBeforeMoveToggle;
                        }
                    }
                    else
                    {
                        Conduit.LinePreview = ImplantCreationUtilities.
                            CreateConnection(DataModelBase.DotList.Last(), currentDot, ConnectionThickness, width, CreatePlate);
                    }
                }
                else
                {
                    Conduit.PointPreview = currentDot;
                }
            }


            if (Control.ModifierKeys != Keys.Shift && Control.ModifierKeys != Keys.Control)
            {
                var nearestDotIndex = GetNearestPointIndex(currentPoint, 1.0);
                if (nearestDotIndex != -1)
                {
                    if (Control.ModifierKeys == Keys.Alt)
                    {
                        Conduit.LineColor = System.Drawing.Color.White;
                    }
                    else
                    {
                        Conduit.LineColor = System.Drawing.Color.LawnGreen;
                    }

                    IDot currDot;
                    if (SelectedIndex != -1)
                    {
                        currDot = DataModelBase.DotList[SelectedIndex];
                    }
                    else
                    {
                        currDot = DataModelBase.DotList.Last();
                    }

                    var width = CreatePlate ? PlateWidth : LinkWidth;
                    var testConn = ImplantCreationUtilities.
                        CreateConnection(currDot, DataModelBase.DotList[nearestDotIndex], ConnectionThickness, width, CreatePlate);
                    if (DataModelBase.ConnectionList.Any(x => DataModelUtilities.IsConnectionEquivalent(x, testConn)) && Control.ModifierKeys != Keys.Shift)
                    {
                        Conduit.LineColor = System.Drawing.Color.Red;
                    }
                }
            }

            RefreshViewPort();
        }

        protected bool HandleUndoableAddControlPoint(Point3d point)
        {
            var action = new InsertControlPointAction();
            action.ConstraintMesh = LowLoDConstraintMesh;
            action.PointToInsert = point;
            var handled = action.Do(this);

            if (handled)
            {
                AddToUndoList(action);
            }

            return handled;
        }

        protected bool HandleUndoableDeleteControlPoint(Point3d point)
        {
            var action = new DeleteControlPointAction();
            action.PointToDelete = point;
            var handled = action.Do(this);

            if (handled)
            {
                AddToUndoList(action);
            }

            return handled;
        }

        protected bool HandleUndoableAddDot(IDot dot)
        {
            var action = new AddDotAction();
            action.DotToAdd = dot;
            var handled = action.Do(this);
            if (!handled)
            {
                InvalidatePreviewData();
            }

            if (action.AddedConnection != null)
            {
                AddToUndoList(action);
            }

            return handled;
        }

        protected bool HandleUndoableAddScrew(Point3d point)
        {
            var action = new InsertScrewAction();
            action.ConstraintMesh = LowLoDConstraintMesh;
            action.ScrewToInsert = point;
            action.PastilleDiameter = PastilleDiameter;
            var handled = action.Do(this);

            if (handled)
            {
                AddToUndoList(action);
            }

            return handled;
        }

        protected void Undo()
        {
            if (_undoList.Count > 0)
            {
                var action = _undoList.Last();
                action.Undo(this);
                _undoList.Remove(action);
                InvalidatePreviewData();
            }
        }

        protected void AddToUndoList(IUndoableAction action)
        {
            if (_undoList.Count > 2)
            {
                _undoList.RemoveAt(0);
            }
            _undoList.Add(action);
        }

        protected override void OnMouseMove(GetPointMouseEventArgs e)
        {
            if (!DoMove)
            {
                return;
            }

            if (e.LeftButtonDown && SelectedIndex >= 0 && LowLoDConstraintMesh != null)
            {
                var moved_point = base.Point();
                var meshPoint = LowLoDConstraintMesh.ClosestMeshPoint(moved_point, IDS.CMF.Constants.ImplantCreation.DotMeshDistancePullTolerance);
                var pointOnMesh = meshPoint.Point;
                LowLoDConstraintMesh.FaceNormals.ComputeFaceNormals();

                var normalRadius = ScrewAngulationConstants.AverageNormalRadiusControlPoint;
                if (DataModelBase.DotList[SelectedIndex] is DotPastille)
                {
                    normalRadius = ScrewAngulationConstants.AverageNormalRadiusPastille;
                }

                //For preview we use a faster calculation, once the command is exited it will re-calculated with a more accurate math.
                //Recalculation is done in DrawImplantStateBase::FinalizeImplantData()
                var averageNormal = VectorUtilities.FindAverageNormalAtPoint(meshPoint.Point, LowLoDConstraintMesh, normalRadius, 1);

                var nearestDotIndex = GetNearestPointIndex(moved_point, 1.0);
                if (nearestDotIndex != -1)
                {
                    var nearestDot = DataModelBase.DotList[nearestDotIndex];
                    var relatedDots =
                        ConnectionUtilities.FindNeighbouringDots(DataModelBase.ConnectionList, nearestDot);
                    //If moved point is the same location as its connection's other point, Don't allow it!
                    if (relatedDots.Any(x => x.Location.EpsilonEquals(nearestDot.Location, 0.0001)))
                    {
                        return;
                    }
                }

                DataModelBase.DotList[SelectedIndex].Location = RhinoPoint3dConverter.ToIPoint3D(pointOnMesh);
                DataModelBase.DotList[SelectedIndex].Direction = RhinoVector3dConverter.ToIVector3D(averageNormal);
                Conduit.PointPreview = DataModelBase.DotList[SelectedIndex];

                _updatePreviewData = true;
            }
            else
            {
                Conduit.PointPreview = null;
            }
        }

    }
}
