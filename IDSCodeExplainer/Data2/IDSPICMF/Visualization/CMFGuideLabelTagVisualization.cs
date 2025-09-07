using IDS.CMF;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.Core.CommandBase;
using IDS.Core.PluginHelper;
using Rhino;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.Visualization
{
    public class CMFGuideLabelTagVisualization : ICommandVisualizationComponent
    {
        private void GenericVisibility(RhinoDoc doc)
        {
            var director = IDSPluginHelper.GetDirector<CMFImplantDirector>(doc.DocumentId);
            var objManager = new CMFObjectManager(director);

            var layers = new List<string>();
            layers.AddRange(objManager.GetAllImplantBuildingBlocks(IBB.GuideFixationScrew).Select(s => s.Layer));
            layers.AddRange(objManager.GetAllImplantBuildingBlocks(IBB.GuideFixationScrewEye).Select(s => s.Layer));
            layers.AddRange(objManager.GetAllImplantBuildingBlocks(IBB.GuideFixationScrewLabelTag).Select(s => s.Layer));
            foreach (var layer in layers)
            {
                Core.Visualization.Visibility.SetVisibleWithParentLayers(doc, layer);
            }
        }

        public void OnCommandBeginVisualization(RhinoDoc doc)
        {
            GenericVisibility(doc);
        }

        public void OnCommandSuccessVisualization(RhinoDoc doc)
        {
            GenericVisibility(doc);
        }

        public void OnCommandFailureVisualization(RhinoDoc doc)
        {
            GenericVisibility(doc);
        }

        public void OnCommandCanceledVisualization(RhinoDoc doc)
        {
            GenericVisibility(doc);
        }
    }
}
