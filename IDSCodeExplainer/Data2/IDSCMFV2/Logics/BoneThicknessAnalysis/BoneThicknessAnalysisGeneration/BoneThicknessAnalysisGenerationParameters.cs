using IDS.Core.V2.Logic;
using IDS.Interface.Geometry;
using System;

namespace IDS.CMF.V2.Logics
{
    public class BoneThicknessAnalysisGenerationParameters : LogicParameters
    {
        public Guid ObjectId { get; set; }

        public IMesh BoneMesh { get; set; }
    }
}
