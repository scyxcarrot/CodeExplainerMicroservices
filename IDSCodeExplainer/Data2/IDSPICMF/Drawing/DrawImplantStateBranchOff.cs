using IDS.CMF;
using IDS.RhinoInterfaces.Converter;
using Rhino.Input;
using System.Windows.Forms;

namespace IDS.PICMF.Drawing
{
    public class DrawImplantStateBranchOff : DrawImplantStateDraw
    {
        public DrawImplantStateBranchOff(CMFImplantDirector implantDirector) : base(implantDirector)
        {

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

                    if (Control.ModifierKeys == Keys.Shift && SelectedIndex == -1)
                    {
                        var point = base.Point();
                        HandleUndoableAddControlPoint(point);
                        InvalidatePreviewData();
                        continue;
                    }
                    else if (Control.ModifierKeys == Keys.Shift && SelectedIndex != -1)
                    {
                        var point = RhinoPoint3dConverter.ToPoint3d(DataModelBase.DotList[SelectedIndex].Location);
                        HandleUndoableDeleteControlPoint(point);
                        InvalidatePreviewData();
                        continue;
                    }
                    else if (Control.ModifierKeys == Keys.Control && SelectedIndex == -1)
                    {
                        var point = base.Point();
                        HandleUndoableAddScrew(point);
                        InvalidatePreviewData();
                        continue;
                    }

                    if (SelectedIndex == -1)
                    {
                        continue;
                    }
                    break;
                }
                else if (rc == GetResult.Nothing)
                {
                    return true; // User pressed ENTER
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

            //This will be invoked again in DrawImplantStateDraw
            Conduit.Enabled = false;
            return base.OnExecute();
        }

    }
}
