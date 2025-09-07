using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;

namespace IDS.PICMF.Drawing
{
    [System.Runtime.InteropServices.Guid("ed7bdf30-4860-4f3e-b888-8f5f54dae9dc")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Any)]
    public class ToggleGuideDrawingTransparency : CmfCommandBase
    {
        static ToggleGuideDrawingTransparency _instance;
        public ToggleGuideDrawingTransparency()
        {
            _instance = this;
        }

        ///<summary>The only instance of the ToggleGuideDrawingTransparency command.</summary>
        public static ToggleGuideDrawingTransparency Instance => _instance;

        public override string EnglishName => "ToggleGuideDrawingTransparency";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            GuideDrawingTransparencyProxy.IsTransparent = !GuideDrawingTransparencyProxy.IsTransparent;

            var toggler = new ToggleGuideDrawingTransparencyVisualization(GuideDrawingTransparencyProxy.IsTransparent);
            toggler.DoToggle(GuideDrawingTransparencyProxy.IsTransparent);

            return Result.Success;
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, CMFImplantDirector director)
        {
            doc.Objects.UnselectAll();
            doc.Views.Redraw();
        }
        public override void OnCommandExecuteFailed(RhinoDoc doc, CMFImplantDirector director)
        {
            doc.Objects.UnselectAll();
            doc.Views.Redraw();
        }
        public override void OnCommandExecuteCanceled(RhinoDoc doc, CMFImplantDirector director)
        {
            doc.Views.Redraw();
        }

    }
}
