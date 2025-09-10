using System;
using System.Xml;

namespace IDS.Glenius.Operations
{
    public static class XmlSchemaChecker
    {
        public static bool ValidatePlaneXml(string filePath)
        {
            var resources = new Resources();
            var planeXmlXsd = resources.PlaneXmlSchemaPath;
            return ValidateXml(planeXmlXsd, filePath);
        }

        public static bool ValidateSphereXml(string filePath)
        {
            var resources = new Resources();
            var sphereXmlXsd = resources.SphereXmlSchemaPath;
            return ValidateXml(sphereXmlXsd, filePath);
        }

        public static bool ValidateXml(string xsdFilePath, string xmlFilePath)
        {
            try
            {
                var settings = new XmlReaderSettings();
                settings.Schemas.Add(null, xsdFilePath);
                settings.ValidationType = ValidationType.Schema;

                using (var reader = XmlReader.Create(xmlFilePath, settings))
                {
                    var doc = new XmlDocument();
                    doc.Load(reader);
                }
            }
            catch (Exception e)
            {
#if DEBUG
                Console.WriteLine(e.Message);
#endif
                return false;
            }

            return true;
        }
    }
}