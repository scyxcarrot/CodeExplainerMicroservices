using IDS.CMF;
using IDS.CMF.Constants;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.Core.PluginHelper;
using Rhino;
using System;

namespace IDS.PICMF.Visualization
{
    public class ToggleGuideDrawingTransparencyVisualization : CMFVisualizationComponentBase
    {
        public ToggleGuideDrawingTransparencyVisualization(bool toggleOn)
        {
            DoToggle(toggleOn);
        }

        public void DoToggle(bool toggleOn)
        {
            var director = IDSPluginHelper.GetDirector<CMFImplantDirector>(RhinoDoc.ActiveDoc.DocumentId);
            ApplyTransparency(IBB.GuideSurfaceWrap, director, Transparency.Medium, toggleOn);
            
            RhinoDoc.ActiveDoc.Views.Redraw();
        }

        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {
            throw new NotImplementedException();
        }

        public override void OnCommandCanceledVisualization(RhinoDoc doc)
        {
            throw new NotImplementedException();
        }

        public override void OnCommandFailureVisualization(RhinoDoc doc)
        {
            throw new NotImplementedException();
        }

        public override void OnCommandSuccessVisualization(RhinoDoc doc)
        {
            throw new NotImplementedException();
        }


    }
}
