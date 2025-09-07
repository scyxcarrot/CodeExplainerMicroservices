using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Commands;

#if (INTERNAL)

namespace IDS.NonProduction.Commands
{
    [System.Runtime.InteropServices.Guid("2d4686ee-a085-466a-b9ea-48e5f701dba3")]
    public class AMace_ToggleDebugMode : Command
    {
        static AMace_ToggleDebugMode _instance;
        public AMace_ToggleDebugMode()
        {
            _instance = this;
        }

        ///<summary>The only instance of the ToggleAMaceTestingMode command.</summary>
        public static AMace_ToggleDebugMode Instance => _instance;

        public override string EnglishName => "AMace_ToggleDebugMode";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            ImplantDirector.IsDebugMode = !ImplantDirector.IsDebugMode;

            IDSPluginHelper.WriteLine(LogCategory.Default, ImplantDirector.IsDebugMode ? "IDS 3.1 aMace Debug mode is ON" : "IDS 3.1 aMace Debug mode is OFF");

            return Result.Success;
        }
    }
}

#endif
