using IDS.Core.CommandBase;
using IDS.Core.PluginHelper;
using IDS.Glenius.ImplantBuildingBlocks;
using Rhino;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Glenius.Visualization
{
    public abstract class VisualizationBaseComponent : ICommandVisualizationComponent
    {
        protected void ApplyTransparencies(RhinoDoc doc, Dictionary<IBB, double> dict)
        {
            var director = IDSPluginHelper.GetDirector<GleniusImplantDirector>(doc.DocumentId);
            var objManager = new GleniusObjectManager(director);
            var filtered = dict.Where(d => objManager.HasBuildingBlock(d.Key)).ToDictionary(d => d.Key, d => d.Value);

            Visibility.SetTransparancies(doc, filtered);

            Core.Visualization.Visibility.SetVisible(doc,
                filtered.Select(x => BuildingBlocks.Blocks[x.Key].Layer).ToList(), true, true, false);

            doc.Views.Redraw();
        }

        public abstract void OnCommandBeginVisualization(RhinoDoc doc);

        public abstract void OnCommandSuccessVisualization(RhinoDoc doc);

        public abstract void OnCommandFailureVisualization(RhinoDoc doc);

        public abstract void OnCommandCanceledVisualization(RhinoDoc doc);
    }
}
