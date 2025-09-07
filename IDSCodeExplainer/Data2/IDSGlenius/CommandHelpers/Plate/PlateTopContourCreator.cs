using IDS.Glenius.Operations;
using Rhino.Geometry;

namespace IDS.Glenius.CommandHelpers
{
    public class PlateTopContourCreator
    {
        private readonly GleniusImplantDirector director;

        public Curve TopCurve { get; private set; }
        public Curve ExistingCurve { get; set; }

        public PlateTopContourCreator(GleniusImplantDirector director)
        {
            this.director = director;
        }

        //If not create new, it will edit previously created curve.
        public bool Create()
        {
            var generator = new PlateDrawingPlaneGenerator(director);
            var topPlane = generator.GenerateTopPlane();

            var plateDrawer = new DrawPlate(director);

            if (ExistingCurve == null)
            {
                TopCurve = plateDrawer.DrawTop(topPlane);
            }
            else
            {
                TopCurve = plateDrawer.EditTop(ExistingCurve, topPlane);
            }

            if (TopCurve != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}
