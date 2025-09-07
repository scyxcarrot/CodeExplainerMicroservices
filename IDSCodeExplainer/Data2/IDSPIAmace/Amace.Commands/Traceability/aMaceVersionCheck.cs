using IDS.Amace;
using IDS.Core.PluginHelper;
using IDS.Core.SplashScreen;
using Rhino;
using Rhino.Commands;

namespace IDS.Commands.Traceability
{
    [System.Runtime.InteropServices.Guid("87373600-0f7c-4136-9ec1-bd614d4b5130")]
    public class aMaceVersionCheck : Command
    {
        public aMaceVersionCheck()
        {
            Instance = this;
        }

        ///<summary>The only instance of the aMaceVersionCheck command.</summary>
        public static aMaceVersionCheck Instance { get; private set; }

        public override string EnglishName => "aMaceVersionCheck";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            HandleVersionCheck.ShowSplashScreen(PlugInInfo.PluginModel);
            HandleVersionCheck.DisplayFileVersion(PlugInInfo.PluginModel);

            var director = IDSPluginHelper.GetDirector<ImplantDirector>(doc.DocumentId);
            HandleVersionCheck.DisplayCommitHashes(director, "aMace");

            return Result.Success;
        }
    }
}
