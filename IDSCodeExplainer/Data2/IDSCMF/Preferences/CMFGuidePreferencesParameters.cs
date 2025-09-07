using System.Collections.Generic;

namespace IDS.CMF.Preferences
{
    public struct GuideParams
    {
        public double GuideSurfaceOffset { get; set; }
        public double GuideSurfaceIsoCurveDistance { get; set; }
        public RemeshParams FirstRemeshParams { get; set; }
        public RemeshParams RemeshParams { get; set; }

        public LightweightParams LightweightParams { get; set; }
        public NonMeshParams NonMeshParams { get; set; }
        public GuideBooleanParams GuideBooleanParams { get; set; }
    }

    public struct RemeshParams
    {
        public int OperationCount { get; set; }
        public double QualityThreshold { get; set; }
        public double MaximalGeometricError { get; set; }
        public bool CheckMaximalEdgeLength { get; set; }
        public double MaximalEdgeLength { get; set; }
        public int NumberOfIterations { get; set; }
        public bool SkipBadEdges { get; set; }
        public bool PreserveSurfaceBorders { get; set; }
    }

    public struct LightweightParams
    {
        public double SegmentRadius { get; set; }
        public double OctagonalBridgeCompensation { get; set; }
        public double FractionalTriangleEdgeLength { get; set; }
    }

    public struct NonMeshParams
    {
        public double NonMeshHeight { get; set; }
        public double NonMeshIsoCurveDistance { get; set; }
    }

    public struct GuideBooleanParams
    {
        public bool UnionBuildingBlocks { get; set; }
        public bool SubtractWithScrewEntities { get; set; }
        public bool SubtractWithSupport { get; set; }
    }

    public struct GuideBarrelLevelingParams
    {
        public double AdditonalOffset { get; set; }
        public double DefaultFrance { get; set; }
        public double DefaultUsCanada { get; set; }
        public double DefaultRoW { get; set; }
        public List<BarrelLevelingBarrelTypeParams> AdditionalRanges { get; set; }
    }

    public struct BarrelLevelingBarrelTypeParams
    {
        public string Type { get; set; }
        public double Default { get; set; }
    }

    public struct GuideBridgeParams
    {
        public double DefaultDiameter { get; set; }
        public double MinimumDiameter { get; set; }
        public double MaximumDiameter { get; set; }
    }
}
