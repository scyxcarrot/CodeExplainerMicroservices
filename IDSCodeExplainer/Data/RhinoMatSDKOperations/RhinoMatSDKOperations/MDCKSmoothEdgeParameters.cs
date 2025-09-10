namespace RhinoMatSDKOperations.Smooth
{
    /// <summary>
    /// Smooth edge parameters
    /// </summary>
    public struct MDCKSmoothEdgeParameters
    {
        public bool USE_RegionOfInfluence;
        public double RegionOfInfluence;

        public bool USE_PointWeight;
        public int PointWeight;

        public bool USE_Iteration;
        public int Iteration;

        public bool USE_AutoSubdivide;
        public bool AutoSubdivide;

        public bool USE_MaxEdgeLength;
        public double MaxEdgeLength;

        public bool USE_MinEdgeLength;
        public double MinEdgeLength;

        public bool USE_BadThreshold;
        public double BadThreshold;

        public bool USE_FastCollapse;
        public bool FastCollapse;

        public bool USE_FlipEdges;
        public bool FlipEdges;

        public bool USE_IgnoreSurfaceInfo;
        public bool IgnoreSurfaceInfo;

        public bool USE_RemeshLowQuality;
        public bool RemeshLowQuality;

        public bool USE_SkipBorder;
        public bool SkipBorder;

        public bool USE_SubdivisionMethod;
        public SubdivisionMethod SubdivisionMethod;
    }
}