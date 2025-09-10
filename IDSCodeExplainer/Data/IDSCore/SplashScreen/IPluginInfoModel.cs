using System.Drawing;

namespace IDS.Core.SplashScreen
{
    //Perhaps name it into something else
    public interface IPluginInfoModel
    {
        string ProductName { get; set; }
        string GetVersionLabel();
        string GetFileVersionLabel();
        Image GetSplashScreenBackgroundImage();
        Point GetSplashScreenVersionLabelLocation();
        string GetManufacturedDate();
        string GetCopyrightYear();
        string GetLNumber();
    }
}
