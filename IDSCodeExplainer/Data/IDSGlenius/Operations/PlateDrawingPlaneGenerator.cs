using IDS.Glenius.Constants;
using Rhino.Geometry;

namespace IDS.Glenius.Operations
{
    public class PlateDrawingPlaneGenerator
    {
        private readonly GleniusImplantDirector director;
        private readonly GleniusObjectManager objectManager;

        public PlateDrawingPlaneGenerator(GleniusImplantDirector director)
        {
            this.director = director;
            this.objectManager = new GleniusObjectManager(director);
        }

        public Plane GenerateTopPlane()
        {
            return GeneratePlane(Plate.MetalBackingPlaneOffsetFromHead);
        }

        public Plane GenerateBottomPlane()
        {
            return GeneratePlane(Plate.MetalBackingPlaneOffsetFromHead + Plate.BasePlateThickness);
        }

        private Plane GeneratePlane(double offset)
        {
            var headAlignment = new HeadAlignment(director.AnatomyMeasurements, objectManager, director.Document, director.defectIsLeft);
            var headCoordinateSystem = headAlignment.GetHeadCoordinateSystem();
            var plateDrawringPlane = new Plane(headCoordinateSystem);

            var translationVector = Vector3d.Multiply(plateDrawringPlane.ZAxis, offset);
            var transform = Transform.Translation(translationVector);
            plateDrawringPlane.Transform(transform);

            return plateDrawringPlane;
        }
    }
}
