using IDS.Core.Utilities;
using IDS.Glenius.Operations;
using System.Xml;

namespace IDS.Glenius.Query
{
    public class ConfigurationInfoProvider
    {
        private XmlDocument configurationXmlDoc;
        private bool isConfigurationFileValid;

        //default configuration file will be used here
        public ConfigurationInfoProvider() : this(new Resources().GleniusIDSXmlFile)
        {
        }

        public ConfigurationInfoProvider(string configurationFilePath)
        {
            isConfigurationFileValid = false;

            var resources = new Resources();
            var xmlXsdPath = resources.GleniusIDSXmlSchemaPath;
            if (XmlSchemaChecker.ValidateXml(xmlXsdPath, configurationFilePath))
            {
                isConfigurationFileValid = true;
                configurationXmlDoc = new XmlDocument();
                configurationXmlDoc.Load(configurationFilePath);
            }
        }

        public bool IsConfigurationFileValid()
        {
            return isConfigurationFileValid;
        }

        public double GetBasePlateOffsetValue()
        {
            if (isConfigurationFileValid)
            {
                var valueNode = configurationXmlDoc.SelectSingleNode("//BasePlateOffset");
                var value = MathUtilities.ParseAsDouble(valueNode.InnerText);
                return value;
            }
            return GetDefaultBasePlateOffsetValue();
        }

        private double GetDefaultBasePlateOffsetValue()
        {
            return 1.0;
        }
    }
}
