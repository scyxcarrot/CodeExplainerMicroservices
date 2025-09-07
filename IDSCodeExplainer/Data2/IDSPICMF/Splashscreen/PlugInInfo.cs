using IDS.Core.SplashScreen;
using System.Diagnostics;
using System.Reflection;

namespace IDS.PICMF
{
    public static class PlugInInfo
    {
        public static PluginInfoModel PluginModel { get; }

        static PlugInInfo()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);

            PluginModel = new PluginInfoModel()
            {
                VersionLabel = @"4-C6.1",
                FileVersionLabel = fileVersionInfo.FileVersion,
                ManufacturedDate = "Aug-2025",
                CopyrightYear = "2025",
                LNumber = "L-30920-26",
                ProductName = "CMF"
            };
        }
    }
}
