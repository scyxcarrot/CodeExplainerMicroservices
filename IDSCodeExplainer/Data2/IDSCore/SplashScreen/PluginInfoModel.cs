using System.Drawing;

namespace IDS.Core.SplashScreen
{
    public class PluginInfoModel : IPluginInfoModel
    {
        public string VersionLabel { get; set; } = "UnSet";
        public string SplashScreenBackgroundImagePath { get; set; }
        public string FileVersionLabel { get; set; } = "UnSet";
        public Point SplashScreenVersionLabelLocation { get; set; } = Point.Empty;
        public string ManufacturedDate { get; set; } = "UnSet";
        public string CopyrightYear { get; set; } = "UnSet";
        public string LNumber { get; set; } = "UnSet";
        public string ProductName { get; set; } = "UnSet";

        public string GetVersionLabel() => VersionLabel;
        public string GetFileVersionLabel() => FileVersionLabel;
        public Point GetSplashScreenVersionLabelLocation() => SplashScreenVersionLabelLocation;
        public string GetManufacturedDate() => ManufacturedDate;
        public string GetCopyrightYear() => CopyrightYear;
        public string GetLNumber() => LNumber;

        public Image GetSplashScreenBackgroundImage()
        {
            return string.IsNullOrEmpty(SplashScreenBackgroundImagePath) ? null : Image.FromFile(SplashScreenBackgroundImagePath);
        }
    }
}
