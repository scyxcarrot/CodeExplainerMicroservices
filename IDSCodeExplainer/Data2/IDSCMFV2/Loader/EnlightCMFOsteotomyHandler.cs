using IDS.EnlightCMFIntegration.DataModel;
using IDS.Interface.Loader;

namespace IDS.CMF.V2.Loader
{
    public class EnlightCMFOsteotomyHandler : IOsteotomyHandler
    {
        public string Name { get; }

        public string Type { get; }

        public double Thickness { get; }

        public string[] Identifier { get; }

        public double[,] Coordinate { get; }

        public EnlightCMFOsteotomyHandler(OsteotomyProperties osteotomyProperties)
        {
            Name = osteotomyProperties.Name;
            Type = osteotomyProperties.Type;
            Thickness = osteotomyProperties.Thickness;
            Identifier = osteotomyProperties.HandlerIdentifier;
            Coordinate = osteotomyProperties.HandlerCoordinates;
        }
    }
}
