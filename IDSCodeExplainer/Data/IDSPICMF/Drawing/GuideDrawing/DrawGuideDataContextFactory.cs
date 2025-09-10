namespace IDS.PICMF.Drawing
{
    public class DrawGuideDataContextFactory
    {
        public DrawGuideDataContext CreateDrawGuideDataContextForGuideSurface()
        {
            return new DrawGuideDataContext
            {
                SkeletonTubeDiameter = 6.0,
                PatchTubeDiameter = 2.0,
                NegativePatchTubeDiameter = 2.0
            };
        }

        public DrawGuideDataContext CreateDrawGuideDataContextForGuideLink()
        {
            return new DrawGuideDataContext
            {
                DrawStepSize = 0.1,
                SkeletonTubeDiameter = 5.0,
                PatchTubeDiameter = 5.0,
                NegativePatchTubeDiameter = 5.0
            };
        }

        public DrawGuideDataContext CreateDrawGuideDataContextForGuideSolid()
        {
            return new DrawGuideDataContext
            {
                PatchTubeDiameter = 3.0
            };
        }
    }
}
