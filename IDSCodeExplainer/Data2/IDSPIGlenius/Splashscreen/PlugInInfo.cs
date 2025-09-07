using IDS.Core.SplashScreen;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace IDS.Glenius
{
    public static class PlugInInfo
    {
        private static readonly Resources Res = new Resources();
        private static readonly string SplashScreenFolderPath = Path.Combine(Res.ExecutingPath, "Splashscreen");

        public static PluginInfoModel PluginModel { get; }

        static PlugInInfo()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);

            PluginModel = new PluginInfoModel
            {
                VersionLabel = @"3.0.1",
                FileVersionLabel = fileVersionInfo.FileVersion,
                SplashScreenBackgroundImagePath = Path.Combine(SplashScreenFolderPath, "GleniusSplashScreenBG.png"),
                ProductName = "Glenius"
            };

        }
    }
}
