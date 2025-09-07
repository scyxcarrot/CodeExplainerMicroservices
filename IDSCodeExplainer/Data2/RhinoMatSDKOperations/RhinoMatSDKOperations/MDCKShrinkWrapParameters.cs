namespace RhinoMatSDKOperations.Wrap
{
    /**
    * ShrinkWrapParameters encapsulates the parameters for the
    * shrinkwrap operation.
    */

    public struct MDCKShrinkWrapParameters
    {
        public readonly double resolution;
        public readonly double gapSize;
        public readonly double resultingOffset;
        public readonly bool protectThinWalls;
        public readonly bool reduceTriangles;
        public readonly bool preserveSharpFeatures;
        public readonly bool preserveSurfaces;

        public MDCKShrinkWrapParameters(double resolution, double gapSize,
            double resultingOffset, bool protectThinWalls,
            bool reduceTriangles, bool preserveSharpFeatures,
            bool preserveSurfaces)
        {
            this.resolution = resolution;
            this.gapSize = gapSize;
            this.resultingOffset = resultingOffset;
            this.protectThinWalls = protectThinWalls;
            this.reduceTriangles = reduceTriangles;
            this.preserveSharpFeatures = preserveSharpFeatures;
            this.preserveSurfaces = preserveSurfaces;
        }
    }
}