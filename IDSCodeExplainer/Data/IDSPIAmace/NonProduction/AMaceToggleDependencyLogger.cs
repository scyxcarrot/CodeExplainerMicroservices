using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Commands;

namespace IDS.Commands.NonProduction
{
#if (INTERNAL)

    [System.Runtime.InteropServices.Guid("CF567527-A1D9-4ACE-90CB-CCDBAE5FAA16")]
    public class AMaceToggleDependencyLogger : Command
    {
        public AMaceToggleDependencyLogger()
        {
            Instance = this;
        }
        
        public static AMaceToggleDependencyLogger Instance { get; private set; }

        public override string EnglishName => "AMace_ToggleDependencyLogger";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var director = IDSPluginHelper.GetDirector<ImplantDirector>(doc.DocumentId);
            director.IsTestingMode = !director.IsTestingMode;
            IDSPluginHelper.WriteLine(LogCategory.Default, $"[IDS] AMace Dependency Logger is {(director.IsTestingMode ? "ON" : "OFF")}");

            return Result.Success;
        }
    }

#endif

}
