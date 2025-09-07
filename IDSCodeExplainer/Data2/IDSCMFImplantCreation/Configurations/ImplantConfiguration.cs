namespace IDS.CMFImplantCreation.Configurations
{
    public struct OverallImplantParams
    {
        public double WrapOperationSmallestDetails { get; set; }
        public double WrapOperationGapClosingDistance { get; set; }
        public double WrapOperationOffset { get; set; }
        public int FixingIterations { get; set; }
    }

    public struct IndividualImplantParams
    {
        public double WrapOperationSmallestDetails { get; set; }
        public double WrapOperationGapClosingDistance { get; set; }
        public double WrapOperationOffsetInDistanceRatio { get; set; }
        public double TubeRadiusModifier { get; set; }
    }

    public struct LandmarkImplantParams
    {
        public double SquareExtensionFromPastilleCircumference { get; set; }
        public double CircleCenterRatioWithPastilleRadius { get; set; }
        public double SquareWidthRatioWithPastilleRadius { get; set; }
        public double CircleRadiusRatioWithPastilleRadius { get; set; }
        public double TriangleHeightRatioWithDefault { get; set; }
    }
}
