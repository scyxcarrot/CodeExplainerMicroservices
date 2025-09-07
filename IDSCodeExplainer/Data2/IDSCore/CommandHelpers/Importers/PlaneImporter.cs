using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino.Geometry;
using System.Windows.Forms;
using System.Xml;

namespace IDS.Core.Importer
{
    public static class PlaneImporter
    {
        public static bool ImportMimicsPlane(string initDict, out Plane importPlane)
        {
            // init
            importPlane = new Plane();

            // Show file dialog
            Rhino.UI.OpenFileDialog fileSelection = new Rhino.UI.OpenFileDialog();
            fileSelection.Title = "Select text file containing MIMICS Plane definition";
            fileSelection.Filter = "Text files (*.txt)|*.txt||";
            fileSelection.InitialDirectory = initDict;
            DialogResult rc = fileSelection.ShowDialog();
            if (rc != System.Windows.Forms.DialogResult.OK)
            {
                return false;
            }
            string filepath = fileSelection.FileName;

            // Read the plane from the file
            bool read = MimicsUtilities.ReadMimicsPlane(filepath, out importPlane);
            if (!read)
            {
                return false;
            }

            // success
            return true;
        }

        public static bool ImportXMLPlane(string filePath, out Plane importPlane)
        {
            importPlane = new Plane();

            var xmlDocument = new XmlDocument();
            xmlDocument.Load(filePath);

            try
            {
                var planeNode = xmlDocument.SelectSingleNode("//Plane");

                var originNode = planeNode.SelectSingleNode("Origin");
                var normalNode = planeNode.SelectSingleNode("Normal");
                var xAxisNode = planeNode.SelectSingleNode("X-axis");
                var yAxisNode = planeNode.SelectSingleNode("Y-axis");

                var origin = PointUtilities.ParseString(originNode.InnerText);
                
                var xAxis = new Vector3d(PointUtilities.ParseString(xAxisNode.InnerText));
                xAxis.Unitize();

                var yAxis = new Vector3d(PointUtilities.ParseString(yAxisNode.InnerText));
                yAxis.Unitize();
                
                var normal = new Vector3d(PointUtilities.ParseString(normalNode.InnerText));
                normal.Unitize();

                importPlane = new Plane(origin, xAxis, yAxis);
                const double epsilon = 0.001;
                if (!importPlane.Normal.EpsilonEquals(normal, epsilon))
                {
                    throw new IDSException("Plane's Normal is not compatible");
                }
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}