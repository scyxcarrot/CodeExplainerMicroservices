using IDS.Amace.Enumerators;
using IDS.Amace.Visualization;
using IDS.Visualization;
using Rhino;

namespace IDS.Amace.Proxies
{
    public static class ScrewInfo
    {
        public static ScrewConduit Numbers = null;

        public static DesignPhase DesignPhase { get; set; } = DesignPhase.None;

        public static void Disable(RhinoDoc doc)
        {
            Disable(doc, false);
        }

        public static void Disable(RhinoDoc doc, bool setVis)
        {
            if (null == Numbers)
            {
                return;
            }

            Numbers.Enabled = false;
            if (setVis)
            {
                Visibility.ScrewDefault(doc);
            }
        }

        public static void Update(RhinoDoc doc)
        {
            Update(doc, false);
        }

        public static void Update(RhinoDoc doc, bool setVis)
        {
            if (null == Numbers)
            {
                return;
            }

            Numbers.UpdateConduit(Numbers.ScrewConduitMode);
            if (setVis)
            {
                Visibility.ScrewNumbers(doc);
            }
            else
            {
                doc.Views.Redraw();
            }
        }
    }
}