using Rhino.Geometry;
using System.Drawing;

namespace IDS.Core.Visualization
{
    public interface IPlateConduitProperties
    {
        double criticalEdgeAngle { get; }

        Brep CupBrepGeometry { get; }

        Color CupColor { get; }

        double PlateThickness { get; set; }
    }
}