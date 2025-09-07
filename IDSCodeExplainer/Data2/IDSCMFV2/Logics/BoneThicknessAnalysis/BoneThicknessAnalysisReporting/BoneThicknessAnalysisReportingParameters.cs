using IDS.Core.V2.Logic;
using IDS.Interface.Geometry;
using System;
using System.Drawing;

namespace IDS.CMF.V2.Logics
{
    public class BoneThicknessAnalysisReportingParameters : LogicParameters
    {
        public Guid ObjectId { get; set; }

        public IMesh BoneMesh { get; set; }

        public double[] ThicknessData { get; set; }

        public Color DefaultColor { get; set; }

        public double MinWallThickness { get; set; }

        public double MaxWallThickness { get; set; }
    }
}
