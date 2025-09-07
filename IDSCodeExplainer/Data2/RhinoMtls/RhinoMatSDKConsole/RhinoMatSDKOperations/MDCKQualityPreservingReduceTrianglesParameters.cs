namespace RhinoMatSDKOperations.Remesh
{
    public struct MDCKQualityPreservingReduceTrianglesParameters
    {
        public double QualityThreshold;

        public double MaximalGeometricError;

        public bool CheckMaximalEdgeLength;

        public double MaximalEdgeLength;

        public int NumberOfIterations;

        public bool SkipBadEdges;

        public bool PreserveSurfaceBorders;

        public int OperationCount;
    }
}