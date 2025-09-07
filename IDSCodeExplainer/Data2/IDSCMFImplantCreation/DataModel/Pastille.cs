using IDS.Interface.Geometry;

namespace IDS.CMFImplantCreation.DataModel
{
    internal class Pastille
    {
        public IPoint3D Location { get; set; }

        public IVector3D Direction { get; set; }

        public double Diameter { get; set; }

        public double Thickness { get; set; }

        public double StampImprintShapeOffset { get; set; }

        public double StampImprintShapeWidth { get; set; }

        public double StampImprintShapeHeight { get; set; }

        public double StampImprintShapeSectionHeightRatio { get; set; }

        public double StampImprintShapeCreationMaxPastilleThickness { get; set; }
    }
}
