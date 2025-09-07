namespace RhinoMatSDKOperations.Smooth
{
    public struct MDCKSmoothParameters
    {
        public string SmoothenAlgorithm;

        public bool Compensation;

        public bool PreserveBadEdges;

        public bool PreserveSharpEdges;

        public double SharpEdgeAngle;

        public double SmoothenFactor;

        public int SmoothenIterations;
    }
}