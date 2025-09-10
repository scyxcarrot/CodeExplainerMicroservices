using IDS.CMF.FileSystem;
using System;
using System.Xml;

namespace IDS.CMF.Operations
{
    public static class CasePreferencesXmlSchemaChecker
    {
        public static bool ValidateCasePrefXml(string filePath)
        {
            var resources = new CMFResources();
            string schemaCasePreferencesFile = resources.CMFPreferenceXmlXsdPath;
            return ValidateXml(schemaCasePreferencesFile, filePath);
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
