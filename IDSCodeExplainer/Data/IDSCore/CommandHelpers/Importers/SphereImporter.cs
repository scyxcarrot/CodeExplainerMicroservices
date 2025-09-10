using IDS.Core.Utilities;
using System.Xml;

namespace IDS.Core.Importer
{
    public static class SphereImporter
    {
        public static AnalyticSphere ImportXmlSphere(string filePath)
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.Load(filePath);

            try
            {
                var sphereNode = xmlDocument.SelectSingleNode("//Sphere");

                var centerPointNode = sphereNode.SelectSingleNode("CenterPoint");
                var radiusNode = sphereNode.SelectSingleNode("Radius");
                
                var centerPoint = PointUtilities.ParseString(centerPointNode.InnerText);
                var radius = MathUtilities.ParseAsDouble(radiusNode.InnerText);

                var sphere = new AnalyticSphere
                {
                    CenterPoint = centerPoint,
                    Radius = radius
                };
                return sphere;
            }
            catch
            {
                return null;
            }
        }
    }
}