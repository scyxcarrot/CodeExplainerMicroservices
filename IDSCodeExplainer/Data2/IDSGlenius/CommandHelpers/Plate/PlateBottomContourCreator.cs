using IDS.Glenius.Operations;
using Rhino.Geometry;

namespace IDS.Glenius.CommandHelpers
{
    public class PlateBottomContourCreator
    {
        private readonly GleniusImplantDirector director;
        public Curve BottomCurve { get; private set; }
        public Curve ExistingCurve { get; set; }

        private readonly Curve topCurve;

        public PlateBottomContourCreator(GleniusImplantDirector director, Curve topCurve)
        {
            this.director = director;
            this.topCurve = topCurve;
        }

        public bool Create()
        {
            if (topCurve == null)
            {
                return false;
            }

            var generator = new PlateDrawingPlaneGenerator(director);
            var bottomPlane = generator.GenerateBottomPlane();

            if (ExistingCurve == null)
            {
                BottomCurve = Curve.ProjectToPlane(topCurve, bottomPlane);
            }
            else
            {
                var plateDrawer = new DrawPlate(director);
                BottomCurve = plateDrawer.DrawBottom(topCurve, ExistingCurve, bottomPlane);
            }

            return BottomCurve != null;
        }
    }
}
