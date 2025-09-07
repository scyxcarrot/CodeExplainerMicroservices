using IDS.Core.Plugin;
using IDS.Core.V2.Utilities;
using IDS.RhinoInterfaces.Converter;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;

namespace IDS.CMF.Utilities
{
    public class ProPlanPlanesExtractor
    {
        public Plane SagittalPlane { get; private set; }

        public Plane AxialPlane { get; private set; }

        public Plane CoronalPlane { get; private set; }

        public bool GetPlanesFromSppc(string sppcPath)
        {
            var header = ExternalToolsUtilities.PerformExtractMatSaxHeader(sppcPath, new IDSRhinoConsole());
            var xdoc = new XmlDocument();
            xdoc.LoadXml(header);

            var root = xdoc.DocumentElement;
            var nodes = root.SelectNodes("//Database");
            if (nodes.Count != 1)
            {
                return false;
            }

            var databaseNode = nodes[0];

            var midPoint = GetMidPoint(databaseNode);
            var imageTransformation = GetTransform(databaseNode);
            var axialNormal = CalculateAxialNormal(midPoint, imageTransformation);

            var sagittalNormal = CalculateSagittalNormal(databaseNode, imageTransformation);
            if (sagittalNormal == Vector3d.Unset)
            {
                return false;
            }

            SetPlanes(axialNormal, sagittalNormal, midPoint);

            return true;
        }
        
        private Point3d GetMidPoint(XmlNode databaseNode)
        {
            var imageBlock = databaseNode.SelectSingleNode("PatientStudies/PatientStudy/Studies/Study/ImageBlock");
            var images = imageBlock.SelectNodes("Images/ScannerImage");
            var imagesCount = images.Count;
            var firstTablePosition = ParseInnerXmlToDouble(images[0], "TablePosition");
            var lastTablePosition = ParseInnerXmlToDouble(images[imagesCount - 1], "TablePosition");
            var imageRange = Math.Abs(lastTablePosition - firstTablePosition);
            var zCenter = firstTablePosition + (imageRange * 0.5);

            var reconstructionX = ParseInnerXmlToDouble(imageBlock, "ReconstructionCenterX");
            var reconstructionY = ParseInnerXmlToDouble(imageBlock, "ReconstructionCenterY");

            return new Point3d(reconstructionX, reconstructionY, zCenter);
        }

        private Vector3d CalculateAxialNormal(Point3d midPoint, Transform resliceTransformation)
        {
            var point1 = TransposeDot(new Point3d(-100.0, -100.0, 0.0), resliceTransformation);
            point1 += midPoint;

            var point2 = TransposeDot(new Point3d(100.0, -100.0, 0.0), resliceTransformation);
            point2 += midPoint;

            var point3 = TransposeDot(new Point3d(100.0, 100.0, 0), resliceTransformation);
            point3 += midPoint;

            var u = point2 - point1;
            var v = point3 - point1;
            var normal = Vector3d.CrossProduct(u, v);
            normal.Unitize();
            return normal;
        }

        private Vector3d CalculateSagittalNormal(XmlNode databaseNode, Transform resliceTransformation)
        {
            var planes = databaseNode.SelectNodes("Objects/Object/CustomSagittalPlane");
            foreach (XmlNode plane in planes)
            {
                var label = plane.SelectSingleNode("Label").InnerXml;
                if (label.ToLower() == "midsagittalplane")
                {
                    var points = plane.SelectNodes("DefiningPoint");
                    if (points.Count == 3)
                    {
                        var planePoints = new List<Point3d>();

                        foreach (XmlNode point in points)
                        {
                            var pt = RhinoPoint3dConverter.ToPoint3d(ParserUtilities.GetPointArray(point.InnerXml));
                            planePoints.Add(pt);
                        }

                        planePoints[0] = TransposeDot(planePoints[0], resliceTransformation);
                        planePoints[1] = TransposeDot(planePoints[1], resliceTransformation);
                        planePoints[2] = TransposeDot(planePoints[2], resliceTransformation);

                        var xAxis = planePoints[0] - planePoints[1];
                        var yAxis = planePoints[2] - planePoints[0];
                        var normal = Vector3d.CrossProduct(xAxis, yAxis);
                        normal.Unitize();
                        if (normal.X > 0)
                        {
                            normal = -normal; //sagittal's normal should always points to the right side
                        }
                        return normal;
                    }
                }
            }

            return Vector3d.Unset;
        }

        private void SetPlanes(Vector3d axialVector, Vector3d sagittalVector, Point3d midPoint)
        {
            var coronalVector = Vector3d.CrossProduct(sagittalVector, axialVector);
            coronalVector.Unitize();

            SagittalPlane = new Plane(midPoint, sagittalVector);
            AxialPlane = new Plane(midPoint, axialVector);
            CoronalPlane = new Plane(midPoint, coronalVector);
        }
        
        private double ParseInnerXmlToDouble(XmlNode node, string tag)
        {
            return double.Parse(node.SelectSingleNode(tag).InnerXml, CultureInfo.InvariantCulture);
        }
        
        private Transform GetTransform(XmlNode databaseNode)
        {
            var matrix = ParserUtilities.GetMatrix(databaseNode.SelectSingleNode("PatientPlans/PatientPlan/ResliceTransformMatrix").InnerXml);
            return ParserUtilities.GetTransform(matrix);
        }

        private Point3d TransposeDot(Point3d point, Transform transformation)
        {
            var transMatrix = new Matrix(transformation.Transpose());
            var pointMatrix = new Matrix(4, 1)
            {
                [0, 0] = point.X,
                [1, 0] = point.Y,
                [2, 0] = point.Z,
                [3, 0] = 1.0
            };
            var multiplied = transMatrix * pointMatrix;

            var transformedPoint = new Point3d(multiplied[0, 0], multiplied[1, 0], multiplied[2, 0]);
            return transformedPoint;
        }
    }
}