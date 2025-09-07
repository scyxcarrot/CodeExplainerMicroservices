using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Constants;
using IDS.CMF.Enumerators;
using IDS.CMF.Visualization;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Commands;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("850DAC2B-4D9F-4B3C-8169-606472926363")]
    [IDSCMFCommandAttributes(DesignPhase.Implant)]
    public class CMFToggleConnectionInfoBubble : CmfCommandBase
    {
        public CMFToggleConnectionInfoBubble()
        {
            TheCommand = this;
        }

        public static CMFToggleConnectionInfoBubble TheCommand { get; private set; }

        public override string EnglishName => CommandEnglishName.CMFToggleConnectionInfoBubble;

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var conduitProxyInstance = ConnectionInfoConduitProxy.GetInstance();

            var isShowing = conduitProxyInstance.IsShowing();

            if (!isShowing)
            {
                var done = conduitProxyInstance.SetUp(director, true);
                conduitProxyInstance.Show(true);
                if (!done)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Warning, "No connection width overridden!");
                }
            }
            else
            {
                conduitProxyInstance.Show(false);
                conduitProxyInstance.Reset();
            }

            return Result.Success;
        }
    }
}