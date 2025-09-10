using IDS.CMF;
using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.V2.Utilities;
using IDS.Core.Utilities;
using IDS.RhinoInterfaces.Converter;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Linq;
using System.Windows.Forms;

namespace IDS.PICMF.Drawing
{
    public class DrawImplantStateMove : DrawImplantBaseState
    {
        private bool _updatePreviewData = true;

        public DrawImplantStateMove(CMFImplantDirector implantDirector) : base(implantDirector)
        {
            SetCommandPrompt("Select screw or control point and move. Press Enter to finish.");
        }

        protected override bool OnExecute()
        {
            Conduit = new ImplantConduit(ImplantDirector) { Enabled = true };
            InvalidatePreviewData();

            //select a point first to continue
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
                    SelectedIndex = GetPickedPointIndex(point2d);

                    if (Control.ModifierKeys == Keys.Shift)
                    {
                        if (SelectedIndex == -1)
                        {
                            var point = base.Point();
                            HandleAddControlPoint(point);
                        }
                        else
                        {
                            var point = RhinoPoint3dConverter.ToPoint3d(DataModelBase.DotList[SelectedIndex].Location);
                            HandleDeleteControlPoint(point);                            
                        }
                        
                        InvalidatePreviewData();
                        continue;
                    }
                    else if (Control.ModifierKeys == Keys.Control)
                    {
                        if (SelectedIndex == -1)
                        {
                            var point = base.Point();
                            HandleAddScrew(point);
                        }

                        InvalidatePreviewData();
                        continue;
                    }

                    if (SelectedIndex == -1)
                    {
                        continue;
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
                    OnHandleTransparentCommands();
                }
            }

            return true;
        }

        protected override void OnExecuteFail()
        {
            Conduit.Enabled = false;
        }

        protected override void OnExecuteSuccess()
        {
            Conduit.Enabled = false;
        }

        protected override void OnDynamicDraw(GetPointDrawEventArgs e)
        {
            base.OnDynamicDraw(e);

            //update conduit
            Conduit.SetLineColorToDefault();

            if (_updatePreviewData)
            {
                InvalidatePreviewData();
            }

            RefreshViewPort();
            _updatePreviewData = false;
        }

        protected override void OnMouseMove(GetPointMouseEventArgs e)
        {
            if (e.LeftButtonDown && SelectedIndex >= 0 && LowLoDConstraintMesh != null)
            {
                var moved_point = base.Point();
                var meshPoint = LowLoDConstraintMesh.ClosestMeshPoint(moved_point, ImplantCreation.DotMeshDistancePullTolerance);
                var pointOnMesh = meshPoint.Point;
                LowLoDConstraintMesh.FaceNormals.ComputeFaceNormals();

                var normalRadius = ScrewAngulationConstants.AverageNormalRadiusControlPoint;
                if (DataModelBase.DotList[SelectedIndex] is DotPastille)
                {
                    normalRadius = ScrewAngulationConstants.AverageNormalRadiusPastille;
                }
                var averageNormal = VectorUtilities.FindAverageNormal(LowLoDConstraintMesh, meshPoint.Point, normalRadius);

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
