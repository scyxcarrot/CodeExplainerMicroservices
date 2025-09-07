using IDS.Core.Enumerators;
using IDS.Core.Importer;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;

namespace IDS.Glenius.Operations
{
    public class PreopCorImporter
    {
        public AnalyticSphere PreopCor { get; private set; }

        public bool ImportData(string fileToImport)
        {
            PreopCor = null;
            
            var xmlValid = XmlSchemaChecker.ValidateSphereXml(fileToImport);
            if (!xmlValid)
            {
                return false;
            }

            var preopCor = SphereImporter.ImportXmlSphere(fileToImport);
            if (preopCor == null)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Something went wrong while reading the XML file of Preop COR");
                return false;
            }

            PreopCor = preopCor;
            return true;
        }
    }
}