using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Core.V2.Utilities;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;

namespace IDS.CMF.V2.ExternalTools
{
    public class ProPlanPlanesExtractor
    {
        private readonly IConsole _console;

        public IPlane SagittalPlane { get; private set; }

        public IPlane AxialPlane { get; private set; }

        public IPlane CoronalPlane { get; private set; }

        public IPlane MidSagittalPlane { get; private set; }

        public ProPlanPlanesExtractor(IConsole console)
        {
            _console = console;
        }

        public bool GetPlanesFromSppc(string sppcPath)
        {
            var header = ExternalToolsUtilities.PerformExtractMatSaxHeader(sppcPath, _console);
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
            
            if (sagittalNormal.EpsilonEquals(IDSVector3D.Unset, 0.001))
            {
                return false;
            }

            SetPlanes(axialNormal, sagittalNormal, midPoint);
            SetMidSagittalPlane(databaseNode, imageTransformation);

            return true;
        }

        private IPoint3D GetMidPoint(XmlNode databaseNode)
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

            return new IDSPoint3D(reconstructionX, reconstructionY, zCenter);
        }

        private IVector3D CalculateAxialNormal(IPoint3D midPoint, ITransform resliceTransformation)
        {
            var point1 = TransposeDot(new IDSPoint3D(-100.0, -100.0, 0.0), resliceTransformation);
            point1 = point1.Add(midPoint);

            var point2 = TransposeDot(new IDSPoint3D(100.0, -100.0, 0.0), resliceTransformation);
            point2 = point2.Add(midPoint);

            var point3 = TransposeDot(new IDSPoint3D(100.0, 100.0, 0), resliceTransformation);
            point3 = point3.Add(midPoint);

            var u = point2.Sub(point1);
            var v = point3.Sub(point1);
            var normal = VectorUtilitiesV2.CrossProduct(u, v);
            normal.Unitize();
            return normal;
        }

        private XmlNode GetMidSagittalPlaneNode(XmlNode databaseNode)
        {
            var planes = databaseNode.SelectNodes("Objects/Object/CustomSagittalPlane");
            foreach (XmlNode plane in planes)
            {
                var label = plane.SelectSingleNode("Label").InnerXml;
                if (label.ToLower() == "midsagittalplane")
                {
                    return plane;
                }
            }

            return null;
        }

        private IVector3D CalculateSagittalNormal(XmlNode databaseNode, ITransform resliceTransformation)
        {
            var plane = GetMidSagittalPlaneNode(databaseNode);
            if (plane != null)
            {
                var points = plane.SelectNodes("DefiningPoint");
                if (points.Count == 3)
                {
                    var planePoints = new List<IPoint3D>();

                    foreach (XmlNode point in points)
                    {
                        planePoints.Add(new IDSPoint3D(ParserUtilities.GetPointArray(point.InnerXml)));
                    }

                    planePoints[0] = TransposeDot(planePoints[0], resliceTransformation);
                    planePoints[1] = TransposeDot(planePoints[1], resliceTransformation);
                    planePoints[2] = TransposeDot(planePoints[2], resliceTransformation);

                    var xAxis = planePoints[0].Sub(planePoints[1]);
                    var yAxis = planePoints[2].Sub(planePoints[0]);
                    var normal = VectorUtilitiesV2.CrossProduct(xAxis, yAxis);
                    normal.Unitize();
                    if (normal.X > 0)
                    {
                        normal = normal.Invert(); //sagittal's normal should always points to the right side
                    }
                    return normal;
                }
            }

            return IDSVector3D.Unset;
        }

        private void SetPlanes(IVector3D axialVector, IVector3D sagittalVector, IPoint3D midPoint)
        {
            var coronalVector = VectorUtilitiesV2.CrossProduct(sagittalVector, axialVector);
            coronalVector.Unitize();

            SagittalPlane = new IDSPlane(midPoint, sagittalVector);
            AxialPlane = new IDSPlane(midPoint, axialVector);
            CoronalPlane = new IDSPlane(midPoint, coronalVector);
        }

        private void SetMidSagittalPlane(XmlNode databaseNode, ITransform resliceTransformation)
        {
            var plane = GetMidSagittalPlaneNode(databaseNode);
            if (plane != null)
            {
                var points = plane.SelectNodes("DefiningPoint");
                if (points.Count == 3)
                {
                    var planePoints = new List<IPoint3D>();

                    foreach (XmlNode point in points)
                    {
                        planePoints.Add(new IDSPoint3D(ParserUtilities.GetPointArray(point.InnerXml)));
                    }

                    planePoints[0] = InverseDot(planePoints[0], resliceTransformation);
                    planePoints[1] = InverseDot(planePoints[1], resliceTransformation);
                    planePoints[2] = InverseDot(planePoints[2], resliceTransformation);

                    var xAxis = planePoints[0].Sub(planePoints[1]);
                    var yAxis = planePoints[2].Sub(planePoints[0]);
                    var normal = VectorUtilitiesV2.CrossProduct(xAxis, yAxis);
                    normal.Unitize();

                    MidSagittalPlane = new IDSPlane(planePoints[0], normal);

                    return;
                }
            }

            MidSagittalPlane = IDSPlane.Unset;
        }

        private double ParseInnerXmlToDouble(XmlNode node, string tag)
        {
            return double.Parse(node.SelectSingleNode(tag).InnerXml, CultureInfo.InvariantCulture);
        }

        private ITransform GetTransform(XmlNode databaseNode)
        {
            var matrix = ParserUtilities.GetMatrix(databaseNode.SelectSingleNode("PatientPlans/PatientPlan/ResliceTransformMatrix").InnerXml);
            return ParserUtilities.GetTransform(matrix);
        }

        private IPoint3D TransposeDot(IPoint3D point, ITransform transformation)
        {
            var transposeTransform = TransformUtilities.Transpose(transformation);
            TransformUtilities.TransformWithoutScaling(transposeTransform, point.X, point.Y, point.Z,
                out var xNew, out var yNew, out var zNew, out var scale);

            return new IDSPoint3D(xNew, yNew, zNew);
        }

        private IPoint3D InverseDot(IPoint3D point, ITransform transformation)
        {
            var inverseTransform = TransformUtilities.Inverse(transformation);
            TransformUtilities.TransformWithoutScaling(inverseTransform, point.X, point.Y, point.Z,
                out var xNew, out var yNew, out var zNew, out var scale);

            return new IDSPoint3D(xNew, yNew, zNew);
        }
    }
}
