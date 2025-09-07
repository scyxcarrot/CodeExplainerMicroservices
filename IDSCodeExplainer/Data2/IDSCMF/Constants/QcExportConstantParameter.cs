using System.Drawing;

namespace IDS.CMF.Constants
{
    public class BoneThicknessAnalysisForART
    {
        public const double ScreenshotsZoom = 2.0;
        public const double MinThickness = 0.8;
        public const double MaxThickness = 2.5;
        public const double Tolerant = 0.001;
        public static readonly Color OutOfRangeColor = Color.LightGray;

        public const string LefortImplantTypeName = "Lefort";
        public const string SkullRemainingSubLayerName = "Skull Remaining";
        public const string MaxillaSubLayerName = "Maxilla";

        public const string SkullRemainLeftViewFileName = "Skull_L.jpeg";
        public const string SkullRemainRightViewFileName = "Skull_R.jpeg";
        public const string MaxillaLeftViewFileName = "Maxilla_L.jpeg";
        public const string MaxillaRightViewFileName = "Maxilla_R.jpeg";
    }
}
