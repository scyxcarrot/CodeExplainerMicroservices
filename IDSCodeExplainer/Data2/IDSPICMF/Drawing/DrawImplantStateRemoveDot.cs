using IDS.CMF;
using IDS.CMF.DataModel;
using IDS.Interface.Implant;
using IDS.RhinoInterfaces.Converter;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.Drawing
{
    public class DrawImplantStateRemoveDot : DrawImplantBaseState
    {
        private readonly List<IConnection> _deletedConnections;

        public DrawImplantStateRemoveDot(CMFImplantDirector implantDirector) : base(implantDirector)
        {
            SetCommandPrompt("Delete Connection. Press ENTER to finish.");
            _deletedConnections = new List<IConnection>();
        }

        protected override bool OnExecute()
        {
            Conduit = new ImplantConduit(ImplantDirector) { Enabled = true };
            InvalidatePreviewData();

            const double epsilon = 0.0001;

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
                    if (SelectedIndex == -1)
                    {
                        InvalidatePreviewData();
                        continue;
                    }

                    var nearestPoint = DataModelBase.DotList[SelectedIndex].Location;
                    var affectedConnections = FindAffectedConnections(RhinoPoint3dConverter.ToPoint3d(nearestPoint));
                    affectedConnections.ForEach(x =>
                    {
                        DataModelBase.ConnectionList.Remove(x);
                        _deletedConnections.Add(x);
                    });

                    var cleanedPointList = new List<IDot>();
                    DataModelBase.DotList.ForEach(x =>
                    {
                        if (DataModelBase.ConnectionList.Any(c => c.A.Location.EpsilonEquals(x.Location, epsilon) || c.B.Location.EpsilonEquals(x.Location, epsilon)))
                        {
                            cleanedPointList.Add(x);
                        }
                    });
                    DataModelBase.DotList = cleanedPointList;

                    InvalidatePreviewData();
                }
                else if (rc == GetResult.Cancel)
                {
                    return false;
                }
                else if (rc == GetResult.Nothing)
                {
                    return true;
                }
                else
                {
                    OnHandleTransparentCommands();
                }
            }
        }

        protected override void OnExecuteSuccess()
        {
            Conduit.Enabled = false;

            var screws = new List<IScrew>();
            var pastilles = new List<DotPastille>();

            _deletedConnections.ForEach(x =>
            {
                TryGetScrewAndPastilleToDelete(x.A, ref screws, ref pastilles);
                TryGetScrewAndPastilleToDelete(x.B, ref screws, ref pastilles);
            });

            if (screws.Any())
            {
                var objectManager = new CMFObjectManager(ImplantDirector);
                screws.ForEach(x => { objectManager.DeleteScrew(x.Id); });
                ImplantDirector.ScrewGroups.PurgeGroups();
            }

            InvalidatePreviewData();
        }

        private void TryGetScrewAndPastilleToDelete(IDot dot, ref List<IScrew> screws, ref List<DotPastille> pastilles)
        {
            if (DataModelBase.DotList.Contains(dot))
            {
                return;
            }

            var pastille = dot as DotPastille;

            if (pastille?.Screw == null)
            {
                if (pastille != null && !pastilles.Contains(pastille))
                {
                    pastilles.Add(pastille);
                }
                return;
            }

            if (!pastilles.Contains(pastille))
            {
                pastilles.Add(pastille);
            }

            if (!screws.Contains(pastille.Screw))
            {
                screws.Add(pastille.Screw);
            }
        }


        protected override void OnExecuteFail()
        {
            Conduit.Enabled = false;
        }

        protected override void OnDynamicDraw(GetPointDrawEventArgs e)
        {
            base.OnDynamicDraw(e);

            //update conduit
            Conduit.SetLineColorToDefault();

            RefreshViewPort();
        }
    }
}
