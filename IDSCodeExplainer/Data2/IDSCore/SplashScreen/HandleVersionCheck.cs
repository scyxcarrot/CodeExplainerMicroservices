using IDS.Core.Enumerators;
using IDS.Core.ImplantDirector;
using IDS.Core.PluginHelper;

namespace IDS.Core.SplashScreen
{
    public static class HandleVersionCheck
    {
        public static void ShowSplashScreen(IPluginInfoModel model)
        {
            var splash = new frmAbout(model);
            splash.Show();
        }

        public static void DisplayFileVersion(IPluginInfoModel model)
        {
            IDSPluginHelper.WriteLine(LogCategory.Default,
                $"IDS Build Number: \t\t { model.GetFileVersionLabel() }");
        }

        public static void DisplayCommitHashes()
        {
            IDSPluginHelper.WriteLine(LogCategory.Default,
                $"IDS Version: \t\t {VersionControl.GetCurrentIDSVersion().Substring(0, 6)}");
            IDSPluginHelper.WriteLine(LogCategory.Default,
                $"RhinoMatSDK Version: \t {VersionControl.GetCurrentRhinoMatSdkVersion().Substring(0, 6)}");
        }

        public static void DisplayCommitHashes(IImplantDirector director, string productName)
        {
            if (director == null)
            {
                DisplayCommitHashes();
                return;
            }

            IDSPluginHelper.WriteLine(LogCategory.Default,
                $"IDS Version: \t\t {VersionControl.GenerateIDSLabelBuildCommitHash(director)}");
            IDSPluginHelper.WriteLine(LogCategory.Default,
                $"RhinoMatSDK Version: \t {VersionControl.GenerateRhinoMatSDKLabelBuildCommitHash(director)}");
        }
    }
}
