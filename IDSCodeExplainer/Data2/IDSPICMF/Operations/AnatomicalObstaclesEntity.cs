using Rhino.DocObjects;
using System.Drawing;

namespace IDS.PICMF.Operations
{
    public class AnatomicalObstaclesEntity
    {
        public RhinoObject Object { get; set; }
        public Color OriginalColor { get; set; }
        public bool IsAnatObstacles { get; set; }
        public bool IsAnatObstaclesLayer { get; set; }

        public AnatomicalObstaclesEntity(RhinoObject rhinoObject, Color originalColor, bool isAnatObstacles, bool isAnatObstaclesLayer)
        {
            Object = rhinoObject;
            OriginalColor = originalColor;
            IsAnatObstacles = isAnatObstacles;
            IsAnatObstaclesLayer = isAnatObstaclesLayer;
        }
    }
}
