using IDS.Common;
using IDS.Glenius.Enumerators;
using Rhino;

namespace IDS.Glenius.Visualization
{
    public class ImportNewScanVisualization : VisualizationBaseComponent
    {
        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {
        
        }

        public override void OnCommandSuccessVisualization(RhinoDoc doc)
        {
            doc = RhinoDoc.ActiveDoc;
            var director = IDSPluginHelper.GetDirector<GleniusImplantDirector>(doc.DocumentId);
            if (director != null)
            {
                switch (director.CurrentDesignPhase)
                {
                    case DesignPhase.ScrewQC:
                        var screwQcPhaseVisualization = new ScrewQCPhaseVisualization();
                        screwQcPhaseVisualization.OnCommandSuccessVisualization(doc);
                        break;
                    case DesignPhase.ScaffoldQC:
                        var scaffoldQcPhaseVisualization = new ScaffoldQCPhaseVisualization();
                        scaffoldQcPhaseVisualization.OnCommandSuccessVisualization(doc);
                        break;
                }
            }
        }

        public override void OnCommandFailureVisualization(RhinoDoc doc)
        {
        
        }

        public override void OnCommandCanceledVisualization(RhinoDoc doc)
        {

        }
    }
}
