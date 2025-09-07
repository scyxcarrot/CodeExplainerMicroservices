namespace IDS.CMF.Preferences
{
    public struct ActualImplantParams
    {
        public OverallImplantParams OverallImplantParams { get; set; }
        public IndividualImplantParams IndividualImplantParams { get; set; }
        public LandmarkImplantParams LandmarkImplantParams { get; set; }
    }

    public struct OverallImplantParams
    {
        public double WrapOperationSmallestDetails { get; set; }
        public double WrapOperationGapClosingDistance { get; set; }
        public double WrapOperationOffset { get; set; }
        public bool IsDoPostProcessing { get; set; }
        public int FixingIterations { get; set; }
    }

    public struct IndividualImplantParams
    {
        public double WrapOperationSmallestDetails { get; set; }
        public double WrapOperationGapClosingDistance { get; set; }
        public double WrapOperationOffsetInDistanceRatio { get; set; }
        public double OffsetOperation_InDistanceRatio { get; set; }
        public double TubeRadiusModifier { get; set; }
        public double PastillePlacementModifier { get; set; }
    }

    public struct LandmarkImplantParams
    {
        public double SquareExtensionFromPastilleCircumference { get; set; }
        public double CircleCenterRatioWithPastilleRadius { get; set; }
        public double SquareWidthRatioWithPastilleRadius { get; set; }
        public double CircleRadiusRatioWithPastilleRadius { get; set; }
        public double TriangleHeightRatioWithDefault { get; set; }
    }

    public struct AutoDeploymentParams
    {
        public string AutoDeployBuildPropertiesUrl { get; set; }
        public string AutoDeployBuildDownloadUrl { get; set; }
        public string SmartDesignPropertiesUrl { get; set; }
        public string SmartDesignDownloadUrl { get; set; }
        public string PBAPythonPropertiesUrl { get; set; }
        public string PBAPythonDownloadUrl { get; set; }
        public string PBAPythonVariableName { get; set; }
        public string PluginVariableName { get; set; }
        public string SmartDesignVariableName { get; set; }
        public string ChecksumSha256VariableName { get; set; }
        public double DownloadTimeOutMin { get; set; }
        // Only for development team
        public bool Enable { get; set; }
    }
}
