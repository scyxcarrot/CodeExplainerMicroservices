using IDS.Glenius.Operations;
using IDS.Core.Drawing;
using System;

namespace IDS.Glenius.Visualization
{
    public class ScapulaReamedOffsetVisualizer : IDisposable
    {
        private static ScapulaReamedOffsetVisualizer _instance;

        private MeshConduit _meshConduit;

        private ScapulaReamedOffsetVisualizer()
        {
        }

        public static ScapulaReamedOffsetVisualizer Get()
        {
            return _instance ?? (_instance = new ScapulaReamedOffsetVisualizer());
        }

        public void ToggleVisualization(GleniusImplantDirector director)
        {
            CreateScapulaReamedOffsetIfNotCreated(director);
            _meshConduit.Enabled = !_meshConduit.Enabled;
            director.Document.Views.Redraw();
        }

        public void Reset()
        {
            if (_meshConduit == null)
            {
                return;
            }

            _meshConduit.Enabled = false;
            _meshConduit = null;
        }

        private void CreateScapulaReamedOffsetIfNotCreated(GleniusImplantDirector director)
        {
            if (_meshConduit != null)
            {
                return;
            }

            var derivedEntities = new ImplantDerivedEntities(director);
            var scapulaReamedOffset = derivedEntities.GetScapulaReamedWithWrap();
            _meshConduit = new MeshConduit();
            _meshConduit.SetMesh(scapulaReamedOffset, Colors.ScapulaReamedOffset, 0.75);
            _meshConduit.Enabled = false;
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
                _meshConduit.Dispose();
            }
        }
    }
}
