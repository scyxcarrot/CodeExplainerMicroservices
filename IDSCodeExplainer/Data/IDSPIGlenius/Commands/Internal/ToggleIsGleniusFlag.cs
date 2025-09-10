using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDSPIGlenius;
using Rhino;
using Rhino.Commands;

namespace IDS.Glenius.Commands.Internal
{
#if (INTERNAL)

    [System.Runtime.InteropServices.Guid("EC29BCF5-158F-42B5-93E4-4E48D206C56D")]
    public class ToggleIsGleniusFlag : Command
    {
        public ToggleIsGleniusFlag()
        {
            Instance = this;
        }
        
        public static ToggleIsGleniusFlag Instance;

        public override string EnglishName => "ToggleIsGleniusFlag";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            IDSPIGleniusPlugIn.IsGlenius = !IDSPIGleniusPlugIn.IsGlenius;

            IDSPluginHelper.WriteLine(LogCategory.Default, IDSPIGleniusPlugIn.IsGlenius ? "[IDS] IsGlenius is TRUE" : "[IDS] IsGlenius is FALSE");

            return Result.Success;
        }
    }

#endif

}
