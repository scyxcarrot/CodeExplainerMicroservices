using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.Core.ImplantBuildingBlocks;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Visualization
{
    public static class Visibility
    {
        public static void Default(RhinoDoc document)
        {
            var transparancies = new Dictionary<IBB, double>()
            {
                {IBB.ProPlanImport, Transparency.Opaque},
                {IBB.NervesWrapped, Transparency.Opaque}
            };

            SetVisualization(document, transparancies);
        }

        public static void ImplantDefault(RhinoDoc document)
        {
            var transparancies = new Dictionary<IBB, double>()
            {
                {IBB.Screw, Transparency.Opaque},
                {IBB.Connection, Transparency.Opaque},
            };

            var director = IDSPluginHelper.GetDirector<CMFImplantDirector>(document.DocumentId);
            var objManager = new CMFObjectManager(director);

            if (objManager.HasBuildingBlock(IBB.ImplantSupport))
            {
                // Set 0.7 only when enter implant phase
                transparancies.Add(IBB.ImplantSupport, 0.7);
            }
            else
            {
                transparancies.Add(IBB.ProPlanImport, Transparency.Medium);
            }

            SetVisualization(document, transparancies);
            ResetOutdatedImplantSupportTransparencies(director);
        }

        public static void SetVisibilityByPhase(CMFImplantDirector director)
        {
            var doc = director.Document;
            switch (director.CurrentDesignPhase)
            {
                case DesignPhase.Initialization:
                    Default(doc);
                    break;
                case DesignPhase.Planning:
                    Default(doc);
                    break;
                case DesignPhase.Implant:
                    ImplantDefault(doc);
                    break;
                default:
                    Default(doc);
                    break;
            }
        }

        public static void HideTheOtherLayer(RhinoDoc doc)
        {
            Core.Visualization.Visibility.IsVisibilityAutomaticallyAdjusted = true;
            List<string> hidePaths = new List<string>();

            //TODO: RH6, Is this correct?
            var othersLayers = new List<Rhino.DocObjects.Layer>();
            doc.Layers.ToList().ForEach(x =>
            {
                if (x.FullPath != null && 
                    ProPlanImportUtilities.IsPartAsPartType(ProPlanImportPartType.Other, x.FullPath))
                {
                    othersLayers.Add(x);
                }
            });

            othersLayers.ForEach(l => hidePaths.Add(l.FullPath));
            Core.Visualization.Visibility.SetHidden(doc, hidePaths);
            Core.Visualization.Visibility.IsVisibilityAutomaticallyAdjusted = false;
        }

        public static void SetVisualization(RhinoDoc document, Dictionary<IBB, double> transparancies)
        {
            Core.Visualization.Visibility.IsVisibilityAutomaticallyAdjusted = true;

            var filtered = GetImplantBuildingBlockVisualization(document, transparancies);

            // Add layers that need to be shown
            var showPaths = filtered.Select(x => x.Key.Layer).ToList();

            Core.Visualization.Visibility.SetTransparancies(document, filtered);

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths);

            Core.Visualization.Visibility.IsVisibilityAutomaticallyAdjusted = false;
        }

        public static void ResetOutdatedImplantSupportTransparencies(CMFImplantDirector director)
        {
            var outdatedImplantSupports = OutdatedImplantSupportHelper.GetOutdatedImplantSupports(director);
            RhinoObjectUtilities.ResetRhObjTransparencies(director, outdatedImplantSupports);
        }

        public static Dictionary<ImplantBuildingBlock, double> GetImplantBuildingBlockVisualization(RhinoDoc document, Dictionary<IBB, double> transparancies)
        {
            Core.Visualization.Visibility.IsVisibilityAutomaticallyAdjusted = true;
            var director = IDSPluginHelper.GetDirector<CMFImplantDirector>(document.DocumentId);
            var objManager = new CMFObjectManager(director);

            var filtered = new Dictionary<ImplantBuildingBlock, double>();
            foreach (var d in transparancies)
            {
                var list = objManager.GetAllImplantBuildingBlocks(d.Key);
                foreach (var block in list)
                {
                    filtered.Add(block, d.Value);
                }
            }

            Core.Visualization.Visibility.IsVisibilityAutomaticallyAdjusted = false;
            return filtered;
        }
    }
}