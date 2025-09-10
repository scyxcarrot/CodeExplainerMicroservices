using IDS.Core.V2.Visualization;

namespace IDS.CMF.V2.Constants
{
    public static class BoneThicknessAnalysisConstants
    {
        public const int LowerPercentile = 1;
        public const int UpperPercentile = 99;

        public const double DefaultMinThickness = 0.8;
        public const double DefaultMaxThickness = 2.5;

        public const double MinGap = 0.1;

        public const double MinMinWallThickness = 0;
        public const double MaxMinWallThickness = 3;

        public const double MinMaxWallThickness = 1;
        public const double MaxMaxWallThickness = 10;
        public const double DefaultMaxWallThickness = 4;
    }
}
