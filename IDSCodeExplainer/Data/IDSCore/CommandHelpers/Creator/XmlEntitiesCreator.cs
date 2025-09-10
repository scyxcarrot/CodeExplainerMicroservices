using IDS.Core.Utilities;
using Rhino.Geometry;
using System.Globalization;
using System.Xml;

namespace IDS.Core.Operations
{
    public static class XmlEntitiesCreator
    {
        public static XmlNode CreatePoint3DNode(XmlDocument xmlDoc, Point3d point, string name)
        {
            var nodePoint = xmlDoc.CreateElement("Point");
            nodePoint.AppendChild(CreateNameElement(xmlDoc, name));
            nodePoint.AppendChild(CreatePoint3DValueElement(xmlDoc, point, "Coordinate"));
            return nodePoint;
        }

        public static XmlNode CreatePlaneNode(XmlDocument xmlDoc, Plane plane, string name)
        {
            var planeSize = 50.0;
            var nodePlane = CreatePlaneNode(xmlDoc, plane, name, planeSize);
            return nodePlane;
        }

        public static XmlNode CreatePlaneNode(XmlDocument xmlDoc, Plane plane, string name, double planeSize)
        {
            var nodePlane = xmlDoc.CreateElement("Plane");
            nodePlane.AppendChild(CreateNameElement(xmlDoc, name));
            nodePlane.AppendChild(CreatePoint3DValueElement(xmlDoc, plane.Origin, "Origin"));
            nodePlane.AppendChild(CreateVector3DValueElement(xmlDoc, plane.Normal, "Normal"));

            var xAxis = new Vector3d(plane.XAxis);
            xAxis = xAxis * planeSize;
            nodePlane.AppendChild(CreateVector3DValueElement(xmlDoc, xAxis, "X-axis"));

            var yAxis = new Vector3d(plane.YAxis);
            yAxis = yAxis * planeSize;
            nodePlane.AppendChild(CreateVector3DValueElement(xmlDoc, yAxis, "Y-axis"));

            return nodePlane;
        }

        public static XmlNode CreateLineNode(XmlDocument xmlDoc, Line line, string name)
        {
            var nodeLine = xmlDoc.CreateElement("Line");
            nodeLine.AppendChild(CreateNameElement(xmlDoc, name));
            nodeLine.AppendChild(CreatePoint3DValueElement(xmlDoc, line.From, "StartPoint"));
            nodeLine.AppendChild(CreatePoint3DValueElement(xmlDoc, line.To, "EndPoint"));
            return nodeLine;
        }

        public static XmlNode CreateSphereNode(XmlDocument xmlDoc, AnalyticSphere sphere, string name)
        {
            var nodeSphere = xmlDoc.CreateElement("Sphere");
            nodeSphere.AppendChild(CreateNameElement(xmlDoc, name));
            nodeSphere.AppendChild(CreatePoint3DValueElement(xmlDoc, sphere.CenterPoint, "CenterPoint"));
            nodeSphere.AppendChild(CreateDoubleValueElement(xmlDoc, sphere.Radius, "Radius"));
            return nodeSphere;
        }

        public static XmlNode CreatePlaneNode2(XmlDocument xmlDoc, Plane plane, string planeNodeName)
        {
            var planeNode = xmlDoc.CreateElement(planeNodeName);
            planeNode.AppendChild(CreatePointNode(xmlDoc, plane.Origin, "Center"));
            planeNode.AppendChild(CreatePointNode(xmlDoc, new Point3d(plane.Normal), "Normal"));

            return planeNode;
        }

        public static XmlNode CreatePointNode(XmlDocument xmlDoc, Point3d point, string pointNodeName)
        {
            var node = xmlDoc.CreateElement(pointNodeName);

            var nodeX = xmlDoc.CreateElement("X");
            nodeX.InnerText = string.Format(CultureInfo.InvariantCulture, "{0:F4}", point.X);

            var nodeY = xmlDoc.CreateElement("Y");
            nodeY.InnerText = string.Format(CultureInfo.InvariantCulture, "{0:F4}", point.Y);

            var nodeZ = xmlDoc.CreateElement("Z");
            nodeZ.InnerText = string.Format(CultureInfo.InvariantCulture, "{0:F4}", point.Z);

            node.AppendChild(nodeX);
            node.AppendChild(nodeY);
            node.AppendChild(nodeZ);
            return node;
        }

        private static XmlNode CreateNameElement(XmlDocument xmlDoc, string name)
        {
            var element = xmlDoc.CreateElement("Name");
            element.InnerText = name;
            return element;
        }

        private static XmlNode CreatePoint3DValueElement(XmlDocument xmlDoc, Point3d point, string elementName)
        {
            var element = xmlDoc.CreateElement(elementName);
            element.InnerText = string.Format(CultureInfo.InvariantCulture, "{0:F8} {1:F8} {2:F8}", point.X, point.Y, point.Z);
            return element;
        }

        private static XmlNode CreateVector3DValueElement(XmlDocument xmlDoc, Vector3d vector, string elementName)
        {
            return CreatePoint3DValueElement(xmlDoc, new Point3d(vector), elementName);
        }

        private static XmlNode CreateDoubleValueElement(XmlDocument xmlDoc, double value, string elementName)
        {
            var element = xmlDoc.CreateElement(elementName);
            element.InnerText = string.Format(CultureInfo.InvariantCulture, "{0:F8}", value);
            return element;
        }
    }
}