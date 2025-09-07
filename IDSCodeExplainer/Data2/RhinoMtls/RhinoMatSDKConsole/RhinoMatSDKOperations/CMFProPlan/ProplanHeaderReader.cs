using System;
using System.Collections.Generic;
using System.Windows.Media.Media3D;
using System.Xml;
using System.Xml.Serialization;

namespace RhinoMatSDKOperations.CMFProPlan
{
    public class ProplanHeaderReader
    {
        public TransformationInfo transformationInfo {get; set;}
        public ProplanHeaderReader(string xmlHeader)
        {
            transformationInfo = new TransformationInfo();

            var header = xmlHeader;
            XmlDocument xdoc = new XmlDocument();
            xdoc.LoadXml(header);
            XmlElement root = xdoc.DocumentElement;
            XmlNodeList nodes = root.SelectNodes("//Database");
            if(nodes.Count != 1)
            {
                return;
            }

            XmlNode databaseNode = nodes[0];

            foreach (XmlNode nodeDatabase in databaseNode)
            {
                var coordSys = nodeDatabase.SelectSingleNode("CoordinateSystems");
                if(coordSys != null)
                {
                    var coordSysNodes = coordSys.SelectSingleNode("CoordinateSystem");
                    while (coordSysNodes != null)
                    {
                        var coordSystem = new CoordinateSystem();
                        coordSystem.id = Int32.Parse(coordSysNodes.SelectSingleNode("id").InnerText);
                        coordSystem.To = Int32.Parse(coordSysNodes.SelectSingleNode("To").InnerText);
                        coordSystem.TransformMatrix = Matrix3D.Parse(coordSysNodes.SelectSingleNode("TransformMatrix").InnerText);

                        transformationInfo.CoordinateSystems.Add(coordSystem);
                        var nextSibling = coordSysNodes.NextSibling;
                        coordSysNodes = nextSibling;
                    }

                    transformationInfo.SystemUI = Int32.Parse(nodeDatabase.SelectSingleNode("SystemUI").InnerText);
                    transformationInfo.SystemMM = Int32.Parse(nodeDatabase.SelectSingleNode("SystemMM").InnerText);
                    transformationInfo.SystemSTL = Int32.Parse(nodeDatabase.SelectSingleNode("SystemSTL").InnerText);
                    transformationInfo.SystemUIinMM = Boolean.Parse(nodeDatabase.SelectSingleNode("SystemUIinMM").InnerText);

                }
            }
        }
    }

    public class TransformationInfo
    {
        public List<CoordinateSystem> CoordinateSystems { get; set; }
        public int SystemUI { get; set; }
        public int SystemMM { get; set; }
        public int SystemSTL { get; set; }
        public bool SystemUIinMM { get; set; }
        public TransformationInfo()
        {
            CoordinateSystems = new List<CoordinateSystem>();
        }
    }
    
    public class CoordinateSystem
    {
        [XmlAttributeAttribute("id")]
        public int id { get; set; }
        [XmlAttributeAttribute("To")]
        public int To { get; set; }
        [XmlAttributeAttribute("Label")]
        public string Label { get; set; }
        public Matrix3D TransformMatrix { get; set; }        

        public CoordinateSystem()
        {
        }
    }
}
