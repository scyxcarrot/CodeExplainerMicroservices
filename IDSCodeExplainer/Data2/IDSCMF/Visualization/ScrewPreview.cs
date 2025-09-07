using IDS.CMF.Factory;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.Core.Drawing;

namespace IDS.CMF.Visualization
{
    public class ScrewPreview : GenericScrewPreview
    {
        private readonly double _referenceLength;
        private readonly ScrewBrepFactory _factory;

        public ScrewPreview(Screw screw) : base(screw.HeadPoint, screw.TipPoint, Colors.ScrewTemporary, 0.75)
        {
            var dir = screw.TipPoint - screw.HeadPoint;
            _referenceLength = dir.Length;
            _factory = new ScrewBrepFactory(screw.GetScrewHeadAtOrigin());
            UpdateScrewBrep();
        }

        protected override string GetDisplayText()
        {
            return $"{(MovingPoint - FixedPoint).Length:F1}mm [was {_referenceLength:F1}mm]";
        }

        private void UpdateScrewBrep()
        {
            ScrewComponentBreps.Clear();
            screwPreview = _factory.CreateScrewBrep(FixedPoint, MovingPoint);
            ScrewComponentBreps.Add(screwPreview);
        }

        protected override void UpdateScrewComponentBreps()
        {
            UpdateScrewBrep();
        }
    }
} 