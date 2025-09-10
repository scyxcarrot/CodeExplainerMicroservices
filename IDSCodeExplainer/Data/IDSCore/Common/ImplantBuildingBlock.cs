using Rhino.DocObjects;
using System.Drawing;

namespace IDS.Core.ImplantBuildingBlocks
{
    public class ImplantBuildingBlock
    {
        public int ID { get; set; }

        public string Name { get; set; }

        public ObjectType GeometryType { get; set; }

        public string Layer { get; set; }

        public Color Color { get; set; }

        public string ExportName { get; set; }

        public ImplantBuildingBlock Clone()
        {
            return new ImplantBuildingBlock
            {
                ID = this.ID,
                Name = this.Name,
                GeometryType = this.GeometryType,
                Layer = this.Layer,
                Color = this.Color,
                ExportName = this.ExportName
            };
        }
    }
}