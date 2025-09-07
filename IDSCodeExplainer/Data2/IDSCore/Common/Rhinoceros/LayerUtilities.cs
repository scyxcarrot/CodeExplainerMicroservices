using Rhino;
using Rhino.DocObjects;
using System;

namespace IDS.Core.Utilities
{
    public static class RhinoLayerUtilities 
    {
        /**
         * Get layer with given name or create it if it does not exist.
         */

        public static int GetLayerWithName(this RhinoDoc doc, string layerName)
        {
            int lidx = doc.Layers.Find(layerName, true);
            if (lidx < 0)
            {
                lidx = doc.Layers.Add(layerName, System.Drawing.Color.Black);
            }
            return lidx;
        }

        /**
         * Get a child layer of the given parent or create it
         * if it does not exist.
         */

        public static int GetLayerWithPath(this RhinoDoc doc, string layerHierarchy)
        {
            string[] layer_names = layerHierarchy.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries);
            return doc.GetLayerWithHierarchy(layer_names);
        }

        /**
         * Get a child layer of the given parent or create it
         * if it does not exist.
         */

        public static int GetLayerWithHierarchy(this RhinoDoc doc, params string[] layerHierarchy)
        {
            if (layerHierarchy.Length < 1)
            {
                return -1;
            }
            if (layerHierarchy.Length == 1)
            {
                // Get or create layer as root-level layer
                string cur_name = layerHierarchy[0];
                int lidx = doc.Layers.FindByFullPath(cur_name, true);
                if (lidx < 0)
                {
                    var layer = new Layer();
                    layer.Name = cur_name;
                    lidx = doc.Layers.Add(layer);
                }
                return lidx;
            }
            else
            {
                int lidx = doc.Layers.FindByFullPath(string.Join("::", layerHierarchy), true);
                if (lidx < 0)
                {
                    // If it doesn't exist, create is as a child of parent layer
                    string[] parentPath = new string[layerHierarchy.Length - 1];
                    Array.Copy(layerHierarchy, parentPath, layerHierarchy.Length - 1);
                    int pidx = doc.GetLayerWithHierarchy(parentPath);
                    var parentLayer = doc.Layers[pidx];
                    var layer = new Layer();
                    layer.Name = layerHierarchy[layerHierarchy.Length - 1];
                    layer.ParentLayerId = parentLayer.Id;
                    lidx = doc.Layers.Add(layer);
                }
                return lidx;
            }
        }

        public static void DeleteEmptyLayers(RhinoDoc document)
        {
            foreach (var lyr in document.Layers)
            {
                if (lyr.FullPath == "Default")
                    continue;

                var layerObjects = document.Objects.FindByLayer(lyr.FullPath);
                if ((lyr.LayerIndex != document.Layers.CurrentLayer.LayerIndex)
                    && (layerObjects != null)
                    && (layerObjects.Length == 0)) // empty layer, not the current one
                {
                    document.Layers.Delete(document.GetLayerWithPath(lyr.FullPath), true);
                }
            }
        }
    }
}