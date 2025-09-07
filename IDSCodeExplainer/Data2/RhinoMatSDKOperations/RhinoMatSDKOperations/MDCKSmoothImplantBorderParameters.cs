namespace RhinoMatSDKOperations.Smooth
{
    public struct MDCKSmoothImplantBorderParameters
    {
        public readonly double topEdgeRadius;
        public readonly double bottomEdgeRadius;
        public readonly double topMinEdgeLength;
        public readonly double topMaxEdgeLength;
        public readonly double bottomMinEdgeLength;
        public readonly double bottomMaxEdgeLength;
        public readonly int iterations;

        public MDCKSmoothImplantBorderParameters(double topEdgeRadius, double bottomEdgeRadius, double topMinEdgeLength, double topMaxEdgeLength, double bottomMinEdgeLength, double bottomMaxEdgeLength, int iterations = 10)
        {
            this.topEdgeRadius = topEdgeRadius;
            this.bottomEdgeRadius = bottomEdgeRadius;
            this.topMinEdgeLength = topMinEdgeLength;
            this.topMaxEdgeLength = topMaxEdgeLength;
            this.bottomMinEdgeLength = bottomMinEdgeLength;
            this.bottomMaxEdgeLength = bottomMaxEdgeLength;
            this.iterations = iterations;
        }
    }
}