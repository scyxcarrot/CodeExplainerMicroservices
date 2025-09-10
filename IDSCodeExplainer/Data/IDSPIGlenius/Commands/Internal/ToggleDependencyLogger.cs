using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Commands;


namespace IDS.Glenius.Commands.Internal
{
#if (INTERNAL)

    [System.Runtime.InteropServices.Guid("343cf5e1-85b5-46c4-84ca-7cdc4533f892")]
    public class ToggleDependencyLogger : Command
    {
        static ToggleDependencyLogger _instance;
        public ToggleDependencyLogger()
        {
            _instance = this;
        }

        ///<summary>The only instance of the ToggleDependencyLogger command.</summary>
        public static ToggleDependencyLogger Instance => _instance;
        public override string EnglishName => "ToggleDependencyLogger";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var director = IDSPluginHelper.GetDirector<GleniusImplantDirector>(doc.DocumentId);

            director.IsTestingMode = !director.IsTestingMode;

            IDSPluginHelper.WriteLine(LogCategory.Default,
                director.IsTestingMode ? "[IDS] Dependency Logger is ON" : "[IDS] Dependency Logger is OFF");

            return Result.Success;
        }
    }

#endif

}
