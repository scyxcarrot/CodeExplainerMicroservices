using Rhino.Geometry;
using System.Drawing;

namespace IDS.CMF.DataModel
{
    public class BrepProperties
    {
        public Brep Brep { get; private set; }
        public Transform Transform { get; private set; }
        public Color Color { get; private set; }

        public BrepProperties(Brep brep, Transform transform, Color color)
        {
            Brep = brep;
            Transform = transform;
            Color = color;
        }
    }
}
