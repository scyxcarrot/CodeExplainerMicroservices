using System.IO;
using System.Reflection;

namespace IDS.Testing
{
    public class TestResources
    {
        private readonly string _executingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        
        public string GleniusColorsXmlFile => Path.Combine(_executingPath, "Resources", "Glenius_Colors.xml");

        public string SinglePlaneXmlFile => Path.Combine(_executingPath, "Resources", "SinglePlane.xml");

        public string MultiplePlanesXmlFile => Path.Combine(_executingPath, "Resources", "MultiplePlanes.xml");

        public string MultipleEntitiesXmlFile => Path.Combine(_executingPath, "Resources", "MultipleEntities.xml");

        public string NoPlaneXmlFile => Path.Combine(_executingPath, "Resources", "NoPlane.xml");
    }
}