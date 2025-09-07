using IDS.CMF;
using IDS.Core.PluginHelper;
using IDS.Core.SplashScreen;
using Rhino;
using Rhino.Commands;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("F856619A-B45A-4D8F-8848-69B261C47CDC")]
    public class CMFVersionCheck : Command
    {
        public CMFVersionCheck()
        {
            Instance = this;
        }

        ///<summary>The only instance of the CMFVersionCheck command.</summary>
        public static CMFVersionCheck Instance { get; private set; }

        public override string EnglishName => "CMFVersionCheck";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            HandleVersionCheck.ShowSplashScreen(PlugInInfo.PluginModel);
            HandleVersionCheck.DisplayFileVersion(PlugInInfo.PluginModel);

            var director = IDSPluginHelper.GetDirector<CMFImplantDirector>(doc.DocumentId);
            HandleVersionCheck.DisplayCommitHashes(director, "CMF");

            return Result.Success;
        }
    }
}
