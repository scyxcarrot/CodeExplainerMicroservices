using IDS.CMF;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Commands;

#if (INTERNAL)

namespace IDS.PICMF.NonProduction
{
    [System.Runtime.InteropServices.Guid("44901B29-7FF7-487C-9FCD-6B9E03036490")]
    public class CMF_TestToggleDebugMode : Command
    {
        public CMF_TestToggleDebugMode()
        {
            Instance = this;
        }
        
        public static CMF_TestToggleDebugMode Instance { get; private set; }

        public override string EnglishName => "CMF_TestToggleDebugMode";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            CMFImplantDirector.IsDebugMode = !CMFImplantDirector.IsDebugMode;

            IDSPluginHelper.WriteLine(LogCategory.Default, CMFImplantDirector.IsDebugMode ? "IDS CMF Debug mode is ON" : "IDS CMF Debug mode is OFF");

            return Result.Success;
        }
    }
}

#endif
