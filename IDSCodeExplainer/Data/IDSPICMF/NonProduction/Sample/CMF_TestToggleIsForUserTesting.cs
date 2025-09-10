using IDS.CMF;
using IDS.CMF.Preferences;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Visualization;
using Rhino;
using Rhino.Commands;

namespace IDS.PICMF.NonProduction
{
#if INTERNAL
    [System.Runtime.InteropServices.Guid("e6e9bac0-3f56-421c-9be0-4de41baf2d7f")]
    public class CMF_TestToggleIsForUserTesting : CmfCommandBase
    {
        static CMF_TestToggleIsForUserTesting _instance;
        public CMF_TestToggleIsForUserTesting()
        {
            _instance = this;
        }

        ///<summary>The only instance of the CMF_TestToggleIsForUserTesting command.</summary>
        public static CMF_TestToggleIsForUserTesting Instance => _instance;

        public override string EnglishName => "CMF_TestToggleIsForUserTesting";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {

            var defaultMode = CMFPreferences.GetIsForUserTesting();

            director.IsForUserTesting = !director.IsForUserTesting;

            var defaultModeString = defaultMode ? "On" : "Off";
            var currentMode = director.IsForUserTesting ? "On" : "Off";

            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"Testing Version flag is { currentMode }, Default is {defaultModeString}");
            UserTestingOverlayConduit.Instance.Enabled = director.IsForUserTesting;

            return Result.Success;
        }
    }
#endif
}
