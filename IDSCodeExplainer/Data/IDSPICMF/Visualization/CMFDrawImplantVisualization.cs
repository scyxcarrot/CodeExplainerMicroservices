using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.Common;
using IDS.Common.CommandBase;
using Rhino;

namespace IDS.CMF.Visualization
{
    public class CMFDrawImplantVisualization : ICommandVisualizationComponent
    {
        private static void SetTransparancies(RhinoDoc document, Dictionary<IBB, double> transparancies)
        {
            var dictionary = transparancies.ToDictionary(ibb => BuildingBlocks.Blocks[ibb.Key], ibb => ibb.Value);
            Common.Visualization.Visibility.SetTransparancies(document, dictionary);
        }

        protected void ApplyTransparencies(RhinoDoc doc, Dictionary<IBB, double> dict)
        {
            var director = IDSPluginHelper.GetDirector<CMFImplantDirector>(doc.DocumentId);
            var objManager = new CMFObjectManager(director);
            var filtered = dict.Where(d => objManager.HasBuildingBlock(d.Key)).ToDictionary(d => d.Key, d => d.Value);

            SetTransparancies(doc, filtered);

            Common.Visualization.Visibility.SetVisible(doc,
                filtered.Select(x => BuildingBlocks.Blocks[x.Key].Layer).ToList(), true, true, false);

            doc.Views.Redraw();
        }

        private void GenericVisibility(RhinoDoc doc)
        {
            var dict = new Dictionary<IBB, double>
            {
                {IBB.Implant, 0.0},
                {IBB.Preop, 0.0},
            };

            ApplyTransparencies(doc, dict);
        }

        public void OnCommandBeginVisualization(RhinoDoc doc)
        {

            var dict = new Dictionary<IBB, double>
            {
                {IBB.Implant, 1.0},
                {IBB.Preop, 0.0},
            };

            ApplyTransparencies(doc, dict);
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
