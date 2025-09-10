using IDS.Core.Visualization;
using Rhino;

namespace IDS.Amace.Proxies
{
    public static class PerformFea
    {
        public static FeaConduit FeaConduit { get; set; }
        
        public static void DisableConduit(RhinoDoc doc)
        {
            if (FeaConduit != null)
            {
                FeaConduit.Enabled = false;
                Visualization.Visibility.ImplantQcDefault(doc);
            }
        }

        public static void InvalidateFeaConduit()
        {
            if (FeaConduit != null && FeaConduit.Enabled)
            {
                FeaConduit.Enabled = false;
            }
            FeaConduit = null;
        }
    }
}
