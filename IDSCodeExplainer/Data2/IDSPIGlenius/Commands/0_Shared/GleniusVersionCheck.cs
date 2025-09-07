using IDS.Core.PluginHelper;
using IDS.Core.SplashScreen;
using IDS.Glenius;
using Rhino;
using Rhino.Commands;

namespace IDSPIGlenius.Commands
{
    [System.Runtime.InteropServices.Guid("ccc79e1f-2767-4edb-bcac-bac3ac96ebfb")]
    public class GleniusVersionCheck : Command
    {
        static GleniusVersionCheck _instance;
        public GleniusVersionCheck()
        {
            _instance = this;
        }

        ///<summary>The only instance of the GleniusVersionCheck command.</summary>
        public static GleniusVersionCheck Instance => _instance;

        public override string EnglishName => "GleniusVersionCheck";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            HandleVersionCheck.ShowSplashScreen(PlugInInfo.PluginModel);
            HandleVersionCheck.DisplayFileVersion(PlugInInfo.PluginModel);

            var director = IDSPluginHelper.GetDirector<GleniusImplantDirector>(doc.DocumentId);
            HandleVersionCheck.DisplayCommitHashes(director, "Glenius");

            return Result.Success;
        }
        
    }
}
