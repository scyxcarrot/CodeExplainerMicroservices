using IDS.CMF.Preferences;

namespace IDS.CMF.Operations
{
    public static class ImplantWrapAndOffsetPredictor
    {

        public static bool IsNeedToUseThickness(double connectionThickness, double connectionWidth)
        {
            var casePref = CMFPreferences.GetActualImplantParameters();


            var condition1 = (connectionWidth * 0.5) * 0.5 > (connectionThickness * casePref.IndividualImplantParams.WrapOperationOffsetInDistanceRatio);
            var condition2 = connectionThickness > connectionWidth;

            return  !condition1 || !condition2;
        }

        //+ve if there is a gap, -ve if it intersects
        public static double PredictGapToSupport(double connectionThickness, double connectionWidth, double basisOverride = double.NaN)
        {
            var wrapBasis = 0.0;

            if (double.IsNaN(basisOverride))
            {
                wrapBasis = GetBasis(connectionThickness, connectionWidth);
            }
            else
            {
                wrapBasis = basisOverride;
            }

            var casePref = CMFPreferences.GetActualImplantParameters();
            var offsetRatio = casePref.IndividualImplantParams.WrapOperationOffsetInDistanceRatio;

            var finalWrapOffset = wrapBasis * offsetRatio;
            var offsetDistanceLower = (connectionThickness - finalWrapOffset) / 2;
            return offsetDistanceLower - finalWrapOffset;
        }

        public static double CalculateLowerOffsetCompensation(double connectionThickness, double connectionWidth, double basisOverride = double.NaN)
        {
            var gap = PredictGapToSupport(connectionThickness, connectionWidth, basisOverride);

            var target = -0.1;
            var ratioCompensation = 0.0;

            return ratioCompensation = gap - target;
        }

        public static double GetBasis(double connectionThickness, double connectionWidth)
        {
            var useThickness = IsNeedToUseThickness(connectionThickness, connectionWidth);
            var condition = CalculateTubeRadiusCore(connectionThickness, connectionWidth) > 0.2;

            var initRes = !condition && useThickness ? connectionWidth : connectionThickness;

            if (!condition && useThickness)
            {
                return connectionWidth;
            }

            return initRes;
        }

        private static double CalculateTubeRadiusCore(double connectionThickness, double connectionWidth, double basisOverride = double.NaN)
        {
            var casePref = CMFPreferences.GetActualImplantParameters();
            var wrapBasis = 0.0;

            if (double.IsNaN(basisOverride))
            {
                wrapBasis = IsNeedToUseThickness(connectionThickness, connectionWidth) ? connectionThickness : connectionWidth;
            }
            else
            {
                wrapBasis = basisOverride;
            }

            return (connectionWidth * 0.5 - (wrapBasis * 
                                             casePref.IndividualImplantParams.WrapOperationOffsetInDistanceRatio)) +
                                             casePref.IndividualImplantParams.TubeRadiusModifier;
        }

        public static double GetTubeRadius(double connectionThickness, double connectionWidth)
        {
            var basis = GetBasis(connectionThickness, connectionWidth);
            return CalculateTubeRadiusCore(connectionThickness, connectionWidth, basis);
        }
    }
}
