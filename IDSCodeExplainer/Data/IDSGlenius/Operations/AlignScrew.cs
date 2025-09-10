using IDS.Core.Operations;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Visualization;
using Rhino.Geometry;

namespace IDS.Glenius.Operations
{
    public class AlignScrew : AdjustGenericScrewLength
    {
        private readonly GleniusImplantDirector director;
        private readonly Screw referenceScrew;
        private readonly bool useHeadPointAsMovingPoint;

        public AlignScrew(Screw screw, bool alignHead)
            : base(alignHead ? screw.HeadPoint : screw.TipPoint,
                alignHead ? screw.TipPoint : screw.HeadPoint, new ScrewPreview(screw, alignHead))
        {
            director = screw.Director;
            referenceScrew = screw;
            useHeadPointAsMovingPoint = alignHead;
        }

        protected override void AdjustMovingPoint(Point3d toPoint)
        {
            if (toPoint != Point3d.Unset)
            {
                // Replace the old screw by the updated screw
                var headPoint = useHeadPointAsMovingPoint ? toPoint : referenceScrew.HeadPoint;
                var tipPoint = useHeadPointAsMovingPoint ? referenceScrew.TipPoint : toPoint;
                var screw = new Screw(referenceScrew.Director, headPoint, tipPoint, referenceScrew.ScrewType, referenceScrew.Index);
                screw.Set(referenceScrew.Id, false, false);
            }

            director.Document.Views.Redraw();
        }

        protected override double GetNearestAvailableScrewLength(double currentLength)
        {
            bool exceeded;
            var nearestLength = ScrewCalibrator.AdjustLengthToAvailableScrewLength(referenceScrew.ScrewType, currentLength, out exceeded);
            return nearestLength;
        }
    }
}