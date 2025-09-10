using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Commands;

namespace IDS.PICMF.NonProduction
{
#if (INTERNAL)
    [System.Runtime.InteropServices.Guid("16A8F43C-2B94-42FC-8C4D-C4A8BA2D4AD8")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Any)]
    public class CMF_TestShowIdleTimeTrackerDashboard : CmfCommandBase
    {
        public CMF_TestShowIdleTimeTrackerDashboard()
        {
            Instance = this;
        }
        
        public static CMF_TestShowIdleTimeTrackerDashboard Instance { get; private set; }

        public override string EnglishName => "CMF_TestShowIdleTimeTrackerDashboard";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            Msai.ShowIdleTimeTrackerUI(director.PluginInfoModel);
            return Result.Success;
        }
    }
#endif
}
