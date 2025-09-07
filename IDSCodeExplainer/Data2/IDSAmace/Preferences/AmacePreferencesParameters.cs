namespace IDS.Amace.Preferences
{
    public struct TransitionIntermediatesParams
    {
        public double IntersectionEntityWrapResolution { get; set; }
    }

    public struct TransitionParams
    {
        public double WrapOperationSmallestDetails { get; set; }
        public double WrapOperationGapClosingDistance { get; set; }
        public double WrapOperationOffset { get; set; }
    }

    public struct ScrewBumpTransitionParams
    {
        public TransitionParams Parameters { get; set; }
        public double RoiOffset { get; set; }
    }

    public struct TransitionActualParams
    {
        public TransitionParams FlangesTransitionParams { get; set; }
        public ScrewBumpTransitionParams ScrewBumpsTransitionParams { get; set; }
    }

    public struct TransitionPreviewParams
    {
        public TransitionParams FlangesTransitionParams { get; set; }
        public bool IsDoPostProcessing { get; set; }
    }

}
