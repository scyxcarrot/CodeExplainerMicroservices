using IDS.Core.V2.Geometries;

namespace IDS.CMFImplantCreation.UnitTests
{
    internal class ConnectionInputValue
    {
        public bool IsActual { get; set; }
        public double Width { get; set; }
        public double Thickness { get; set; }
        public IDSVector3D AverageConnectionDirection { get; set; }
    }
}