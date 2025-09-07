using IDS.Glenius.Enumerators;

namespace IDS.Glenius.ImplantBuildingBlocks
{
    public struct HeadTypeHelper
    {
        public HeadType Type { get; set; }
        public string DisplayText { get; set; }
        public double Diameter { get; set; }

        public HeadTypeHelper(HeadType type, string displayText, double diameter)
        {
            this.Type = type;
            this.DisplayText = displayText;
            this.Diameter = diameter;
        }
    };
}