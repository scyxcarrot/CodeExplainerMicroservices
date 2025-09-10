using Rhino;

namespace IDS.CMF.Utilities
{
    public static class ErrorUtilities
    {
        public static void RemoveErrorAnalysisLayerIfExist()
        {
            var doc = RhinoDoc.ActiveDoc;

            var layering = "ErrorAnalysis";
            var layer = doc.Layers.FindName(layering);
            if (layer != null)
            {
                var objs = doc.Objects.FindByLayer(layer);
                foreach (var obj in objs)
                {
                    doc.Objects.Unlock(obj.Id, true);
                    doc.Objects.Delete(obj.Id, true);
                }
                doc.Layers.Delete(layer, true);
            }
        }
    }
}
