using IDS.CMFImplantCreation.Configurations;

namespace IDS.CMFImplantCreation.Utilities
{
    internal static class ImplantWrapAndOffsetPredictor
    {
        public static double GetBasis(IndividualImplantParams individualImplantParams, double connectionThickness, double connectionWidth)
        {
            var useThickness = IsNeedToUseThickness(individualImplantParams, connectionThickness, connectionWidth);
            var condition = CalculateTubeRadiusCore(individualImplantParams, connectionThickness, connectionWidth) > 0.2;

            var initRes = !condition && useThickness ? connectionWidth : connectionThickness;
            return initRes;
        }

        public static double GetTubeRadius(IndividualImplantParams individualImplantParams, double connectionThickness, double connectionWidth)
        {
            var basis = GetBasis(individualImplantParams, connectionThickness, connectionWidth);
            return CalculateTubeRadiusCore(individualImplantParams, connectionThickness, connectionWidth, basis);
        }

        private static bool IsNeedToUseThickness(IndividualImplantParams individualImplantParams, double connectionThickness, double connectionWidth)
        {
            var condition1 = (connectionWidth * 0.5) * 0.5 > (connectionThickness * individualImplantParams.WrapOperationOffsetInDistanceRatio);
            var condition2 = connectionThickness > connectionWidth;

            return !condition1 || !condition2;
        }

        private static double CalculateTubeRadiusCore(IndividualImplantParams individualImplantParams, double connectionThickness, double connectionWidth, double basisOverride = double.NaN)
        {
            var wrapBasis = 0.0;

            if (double.IsNaN(basisOverride))
            {
                wrapBasis = IsNeedToUseThickness(individualImplantParams, connectionThickness, connectionWidth) ? connectionThickness : connectionWidth;
            }
            else
            {
                wrapBasis = basisOverride;
            }

            return (connectionWidth * 0.5 - (wrapBasis * 
                                             individualImplantParams.WrapOperationOffsetInDistanceRatio)) +
                                             individualImplantParams.TubeRadiusModifier;
        }

        public static double CalculateLowerOffsetCompensation(
            IndividualImplantParams individualImplantParams,
            double connectionThickness, double connectionWidth, 
            double wrapBasisOverride)
        {
            var gap = PredictGapToSupport(
                individualImplantParams, 
                connectionThickness, 
                connectionWidth,
                wrapBasisOverride);
            var target = -0.1;

            return gap - target;
        }

        //+ve if there is a gap, -ve if it intersects
        public static double PredictGapToSupport(
            IndividualImplantParams individualImplantParams, 
            double connectionThickness, 
            double connectionWidth, 
            double wrapBasisOverride)
        {
            double wrapBasis;
            if (double.IsNaN(wrapBasisOverride))
            {
                wrapBasis = GetBasis(
                    individualImplantParams, 
                    connectionThickness, 
                    connectionWidth);
            }
            else
            {
                wrapBasis = wrapBasisOverride;
            }

            var offsetRatio =
                individualImplantParams.WrapOperationOffsetInDistanceRatio;
            var finalWrapOffset = wrapBasis * offsetRatio;
            var offsetDistanceLower = 
                (connectionThickness - finalWrapOffset) / 2;
            return offsetDistanceLower - finalWrapOffset;
        }
    }
}
