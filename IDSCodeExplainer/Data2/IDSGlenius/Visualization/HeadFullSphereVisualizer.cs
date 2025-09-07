using IDS.Core.Drawing;
using IDS.Glenius.Forms;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Query;
using Rhino.Geometry;
using System;

namespace IDS.Glenius.Visualization
{
    public class HeadFullSphereVisualizer : IDisposable
    {
        private static HeadFullSphereVisualizer _instance;

        private FullSphereConduit _fullSphereConduit;

        private HeadFullSphereVisualizer()
        {
        }

        public static HeadFullSphereVisualizer Get()
        {
            return _instance ?? (_instance = new HeadFullSphereVisualizer());
        }

        public void ToggleVisualization(GleniusImplantDirector director)
        {
            CreateFullSphereConduitIfNotCreated(director);

            if (!_fullSphereConduit.Enabled)
            {
                var viewModel = HeadPanel.GetPanelViewModel();
                if (viewModel != null)
                {
                    _fullSphereConduit.Transparency = 1 - viewModel.Model.FullSphereOpacity;
                }
            }
            _fullSphereConduit.Enabled = !_fullSphereConduit.Enabled;
            director.Document.Views.Redraw();
        }

        public void Reset()
        {
            if (_fullSphereConduit == null)
            {
                return;
            }
            _fullSphereConduit.Enabled = false;
            _fullSphereConduit = null;
        }

        private void CreateFullSphereConduitIfNotCreated(GleniusImplantDirector director)
        {
            if (_fullSphereConduit != null)
            {
                return;
            }
            var objectManager = new GleniusObjectManager(director);
            var head = objectManager.GetBuildingBlock(IBB.Head) as Head;

            Plane headCoordinateSystem;
            objectManager.GetBuildingBlockCoordinateSystem(IBB.Head, out headCoordinateSystem);
            _fullSphereConduit = new FullSphereConduit(headCoordinateSystem.Origin,
                HeadQueries.GetHeadDiameter(head.HeadType));
            _fullSphereConduit.Enabled = false;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _fullSphereConduit.Dispose();
            }
        }
    }
}
