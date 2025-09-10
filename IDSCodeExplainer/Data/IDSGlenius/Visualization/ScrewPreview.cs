using IDS.Core.Drawing;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using Rhino.Geometry;

namespace IDS.Glenius.Visualization
{
    public class ScrewPreview : GenericScrewPreview
    {
        private readonly ScrewBrepFactory _factory;
        private readonly Brep _screwMantleOriginalBrep;
        private readonly bool useHeadPointAsMovingPoint;

        public ScrewPreview(Screw screw, bool useHeadPointAsMovingPoint) : 
            base(useHeadPointAsMovingPoint ? screw.HeadPoint : screw.TipPoint,
                useHeadPointAsMovingPoint ? screw.TipPoint : screw.HeadPoint, Colors.MobelifeRed, 0.75)
        {
            this.useHeadPointAsMovingPoint = useHeadPointAsMovingPoint;
            _factory = new ScrewBrepFactory(screw.ScrewType);

            var screwMantleBrepFactory = new ScrewMantleBrepFactory(screw.ScrewType);
            _screwMantleOriginalBrep = screwMantleBrepFactory.CreateScrewMantleBrep(screw.GetScrewMantle().ExtensionLength);

            UpdateScrewBrep();
        }

        private void UpdateScrewBrep()
        {
            ScrewComponentBreps.Clear();
            ScrewComponentBreps.Add(_factory.CreateScrewBrep(GetHeadPoint(), GetTipPoint()));
            var screwMantleBrep = _screwMantleOriginalBrep.DuplicateBrep();
            screwMantleBrep.Transform(GetCurrentTransform());
            ScrewComponentBreps.Add(screwMantleBrep);
        }

        protected override void UpdateScrewComponentBreps()
        {
            UpdateScrewBrep();
        }

        private Transform GetCurrentTransform()
        {
            var direction = GetTipPoint() - GetHeadPoint();
            direction.Unitize();
            var rotation = Transform.Rotation(-Plane.WorldXY.ZAxis, direction, Plane.WorldXY.Origin);
            var translation = Transform.Translation(GetHeadPoint() - Plane.WorldXY.Origin);
            var fullTransform = Transform.Multiply(translation, rotation);
            return fullTransform;
        }

        private Point3d GetHeadPoint()
        {
            return useHeadPointAsMovingPoint ? MovingPoint : FixedPoint;
        }

        private Point3d GetTipPoint()
        {
            return useHeadPointAsMovingPoint ? FixedPoint : MovingPoint;
        }
    }
} 