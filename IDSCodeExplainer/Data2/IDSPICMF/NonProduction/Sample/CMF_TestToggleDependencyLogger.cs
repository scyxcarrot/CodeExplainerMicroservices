using IDS.CMF;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Commands;

namespace IDS.PICMF.NonProduction
{
#if (INTERNAL)

    [System.Runtime.InteropServices.Guid("a1133e62-a527-4e4d-80fd-50d46f2fbd16")]
    public class CMF_TestToggleDependencyLogger : CmfCommandBase
    {
        static CMF_TestToggleDependencyLogger _instance;
        public CMF_TestToggleDependencyLogger()
        {
            _instance = this;
        }

        ///<summary>The only instance of the CMF_TestToggleDependencyLogger command.</summary>
        public static CMF_TestToggleDependencyLogger Instance => _instance;

        public override string EnglishName => "CMF_TestToggleDependencyLogger";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            director.IsTestingMode = !director.IsTestingMode;

            IDSPluginHelper.WriteLine(LogCategory.Default,
                director.IsTestingMode ? "[IDS] Dependency Logger is ON" : "[IDS] Dependency Logger is OFF");

            return Result.Success;
        }
    }

#endif
}
