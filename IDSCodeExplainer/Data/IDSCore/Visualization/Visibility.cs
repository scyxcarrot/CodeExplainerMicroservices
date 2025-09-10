using IDS.Core.ImplantBuildingBlocks;
using Rhino;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Core.Visualization
{
    public static class Visibility
    {
        public static bool IsVisibilityAutomaticallyAdjusted { get; set; } = false;

        public static void HideAll(RhinoDoc doc)
        {
            SetVisible(doc, new List<string>());
        }

        private static void ConvertFullPathToAllPaths(IEnumerable<string> inPaths, out List<string> allPaths)
        {
            allPaths = new List<string>();
            foreach (var theInPath in inPaths)
            {
                var plist = theInPath.Split(new [] { "::" }, StringSplitOptions.None).ToList();
                var firstAdd = true;
                foreach (var theChild in plist)
                {
                    if (firstAdd)
                    {
                        allPaths.Add(theChild);
                        firstAdd = false;
                    }
                    else
                    {
                        allPaths.Add(allPaths.Last() + "::" + theChild);
                    }
                }
            }
        }
        public static void ResetTransparancies(RhinoDoc document)
        {
            SetTransparancies(document, new Dictionary<ImplantBuildingBlock, double>(0));
        }
        public static void SetTransparancies(RhinoDoc document, ImplantBuildingBlock block1, double transparancy1)
        {
            SetTransparancies(document, new Dictionary<ImplantBuildingBlock, double>() { { block1, transparancy1 } });
        }

        public static void SetTransparancies(RhinoDoc document, Dictionary<ImplantBuildingBlock, double> transparancies)
        {
            IsVisibilityAutomaticallyAdjusted = true;
            // Stop recording actions for Ctrl-Z
            document.UndoRecordingEnabled = false;

            // Transparencies of objects
            ImplantBuildingBlockProperties.ResetTransparencies(document);
            foreach (var buildingBlockTransparancy in transparancies)
            {
                ImplantBuildingBlockProperties.SetTransparency(buildingBlockTransparancy.Key, document, buildingBlockTransparancy.Value);
            }

            // Restart recording actions for Ctrl-Z
            document.UndoRecordingEnabled = true;
            IsVisibilityAutomaticallyAdjusted = false;
        }

        /// <summary>
        /// Shows the single building block.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="buildingBlock">The building block.</param>
        public static void ShowSingleBuildingBlock(RhinoDoc document, ImplantBuildingBlock buildingBlock)
        {
            IsVisibilityAutomaticallyAdjusted = true;
            // Add layers that need to be shown
            var showPaths = new List<string>
            {
                buildingBlock.Layer
            };

            // Manage visualisations
            ResetTransparancies(document);
            SetVisible(document, showPaths);
            IsVisibilityAutomaticallyAdjusted = false;
        }

        public static void SetVisible(RhinoDoc doc, List<string> showPaths, bool applyOnParentLayers = true, bool forceInvisible = true, bool layerExpansion = true)
        {
            IsVisibilityAutomaticallyAdjusted = true;

            // Stop recording actions for Ctrl-Z
            doc.UndoRecordingEnabled = false;

            // Include parent layers if necessary
            List<string> allPaths;
            if (applyOnParentLayers)
            {
                ConvertFullPathToAllPaths(showPaths, out allPaths);
            }
            else
            {
                allPaths = showPaths;
            }

            //Remove potential dupes
            allPaths = allPaths.Distinct().ToList();

            // Manage visibility
            foreach (var layer in doc.Layers)
            {
                if (!layer.IsValid) //skip if layer is invalid, usually not refreshed
                {
                    continue;
                }

                if (allPaths.Contains(layer.FullPath))
                {
                    if (!layer.IsVisible)
                    {
                        doc.Layers.ForceLayerVisible(layer.Id);
                    }

                    if (layerExpansion)
                    {
                        layer.IsExpanded = true;
                    }
                }
                else if (layer.FullPath != "Default" && forceInvisible)
                {
                    layer.IsVisible = false;
                    if (layerExpansion)
                    {
                        layer.IsExpanded = false;
                    }
                    if (layer.GetChildren() == null)
                    {
                        layer.SetPersistentVisibility(false);
                    }
                }

                layer.CommitChanges();
            }

            // Update views
            doc.Views.Redraw();

            // Restart recording actions for Ctrl-Z
            doc.UndoRecordingEnabled = true;
            IsVisibilityAutomaticallyAdjusted = false;
        }

        public static bool SetVisible(RhinoDoc doc, string showPath)
        {
            return SetIsVisibleValue(doc, new List<string> { showPath }, true);
        }

        public static bool SetHidden(RhinoDoc doc, string hidePath)
        {
            return SetHidden(doc, new List<string> { hidePath });
        }

        public static bool SetHidden(RhinoDoc doc, List<string> hidePaths)
        {
            return SetIsVisibleValue(doc, hidePaths, false);
        }

        //does not check parent layer
        private static bool SetIsVisibleValue(RhinoDoc doc, List<string> showPaths, bool isVisible)
        {
            IsVisibilityAutomaticallyAdjusted = true;
            var set = true;

            // Stop recording actions for Ctrl-Z
            doc.UndoRecordingEnabled = false;

            // Manage visibility
            foreach (var layer in doc.Layers)
            {
                if (!layer.IsValid) //skip if layer is invalid, usually not refreshed
                {
                    continue;
                }

                if (!showPaths.Contains(layer.FullPath))
                {
                    continue;
                }

                if (isVisible)
                {
                    if (!layer.IsVisible)
                    {
                        doc.Layers.ForceLayerVisible(layer.Id);
                    }
                }
                else
                {
                    layer.IsVisible = false;
                }

                if (!isVisible && layer.GetChildren() == null)
                {
                    layer.SetPersistentVisibility(false);
                }
                layer.CommitChanges();
                var setCorrectly = doc.Layers[layer.LayerIndex].IsVisible == isVisible;
                set = set && setCorrectly;
            }

            // Update views
            doc.Views.Redraw();

            // Restart recording actions for Ctrl-Z
            doc.UndoRecordingEnabled = true;

            IsVisibilityAutomaticallyAdjusted = false;
            return set;
        }

        public static void SetVisibleWithParentLayers(RhinoDoc doc, string showPath)
        {
            IsVisibilityAutomaticallyAdjusted = true;
            // Stop recording actions for Ctrl-Z
            doc.UndoRecordingEnabled = false;

            List<string> allPaths;
            var showPaths = new List<string> { showPath };
            ConvertFullPathToAllPaths(showPaths, out allPaths);

            allPaths = allPaths.Distinct().ToList();

            // Manage visibility
            foreach (var layer in doc.Layers)
            {
                if (!layer.IsValid) //skip if layer is invalid, usually not refreshed
                {
                    continue;
                }

                if (!allPaths.Contains(layer.FullPath))
                {
                    continue;
                }

                if (!layer.IsVisible)
                {
                    doc.Layers.ForceLayerVisible(layer.Id);
                }

                layer.CommitChanges();
            }

            // Update views
            doc.Views.Redraw();

            // Restart recording actions for Ctrl-Z
            doc.UndoRecordingEnabled = true;
            IsVisibilityAutomaticallyAdjusted = false;
        }
    }
}