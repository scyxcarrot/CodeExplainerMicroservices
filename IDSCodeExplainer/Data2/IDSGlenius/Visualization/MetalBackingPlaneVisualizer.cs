using IDS.Core.Drawing;
using IDS.Glenius.Operations;

namespace IDS.Glenius.Visualization
{
    public class MetalBackingPlaneVisualizer
    {
        private static MetalBackingPlaneVisualizer _instance;

        private PlaneConduit _planeConduit;

        private MetalBackingPlaneVisualizer()
        {
        }

        public static MetalBackingPlaneVisualizer Get()
        {
            return _instance ?? (_instance = new MetalBackingPlaneVisualizer());
        }

        public void ToggleVisualization(GleniusImplantDirector director)
        {
            CreatePlaneConduitIfNotCreated(director);
            _planeConduit.Enabled = !_planeConduit.Enabled;
            director.Document.Views.Redraw();
        }

        public void Reset()
        {
            if (_planeConduit == null)
            {
                return;
            }

            _planeConduit.Enabled = false;
            _planeConduit = null;
        }

        private void CreatePlaneConduitIfNotCreated(GleniusImplantDirector director)
        {
            if (_planeConduit != null)
            {
                return;
            }

            var derivedEntities = new ImplantDerivedEntities(director);
            var plane = derivedEntities.GetMetalBackingPlane();
            _planeConduit = new PlaneConduit();
            _planeConduit.SetColor(128, 128, 128);
            _planeConduit.SetTransparency(0); //Opaque
            _planeConduit.SetPlane(plane.Origin, plane.Normal, 100);
            _planeConduit.SetRenderBack(true);
            _planeConduit.UsePostRendering = true;
            _planeConduit.Enabled = false;
        }
    }
}
