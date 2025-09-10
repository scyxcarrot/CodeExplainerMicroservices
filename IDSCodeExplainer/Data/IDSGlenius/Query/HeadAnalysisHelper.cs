using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Quality;
using Rhino.Geometry;

namespace IDS.Glenius.Query
{
    public class HeadAnalysisHelper
    {
        private readonly double boneHeadThreshold = 1.0;
        private readonly double boneTaperThreshold = 0.3;
        private readonly GleniusObjectManager objectManager;
        private readonly HeadAnalysis headAnalysis;

        public bool IsBoneHeadVicinityOK { get; private set; }
        public bool IsBoneTaperVicinityOK { get; private set; }

        public HeadAnalysisHelper(GleniusObjectManager objectManager)
        {
            this.objectManager = objectManager;
            var bone = GetBoneMesh();
            headAnalysis = new HeadAnalysis(bone, boneHeadThreshold, boneTaperThreshold);
        }

        public void UpdateBoneMesh()
        {
            var bone = GetBoneMesh();
            headAnalysis.UpdateBoneMesh(bone, boneHeadThreshold, boneTaperThreshold);
        }

        public void PerformVicinityCheck()
        {
            var taperBrep = objectManager.GetBuildingBlock(IBB.TaperMantleSafetyZone).Geometry as Brep;
            var head = objectManager.GetBuildingBlock(IBB.Head) as Head;
            var headBrep = head.Geometry as Brep;
            IsBoneHeadVicinityOK = headAnalysis.PerformBoneHeadVicinityCheck(headBrep);
            IsBoneTaperVicinityOK = headAnalysis.PerformBoneTaperVicinityCheck(taperBrep);
        }

        private Mesh GetBoneMesh()
        {
            return objectManager.GetBuildingBlock(IBB.ScapulaReamed).Geometry as Mesh;
        }
    }
}
