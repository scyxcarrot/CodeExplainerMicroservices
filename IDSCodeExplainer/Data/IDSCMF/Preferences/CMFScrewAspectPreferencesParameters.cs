namespace IDS.CMF.Preferences
{
    public struct ScrewAspectParams
    {
        public ScrewAngulationParams ScrewAngulationParams { get; set; }
    }

    public struct ScrewAngulationParams
    {
        public double StandardAngleInDegrees { get; set; }
        public double MaximumAngleInDegrees { get; set; }
    }
}