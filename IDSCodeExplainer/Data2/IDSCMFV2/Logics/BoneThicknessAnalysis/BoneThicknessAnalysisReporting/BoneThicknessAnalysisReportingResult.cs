using IDS.Core.V2.Logic;
using IDS.Interface.Geometry;
using System.Drawing;

namespace IDS.CMF.V2.Logics
{
    public class BoneThicknessAnalysisReportingResult : LogicResult
    {
        public double LowerBound { get; set; }

        public double UpperBound { get; set; }

        public IMesh NewBoneMesh { get; set; }

        public Color[] VerticesColors { get; set; }
    }
}
