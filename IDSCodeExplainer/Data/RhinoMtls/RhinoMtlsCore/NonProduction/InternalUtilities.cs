using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Xml;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using IDS.RhinoMtlsCore.Utilities;

namespace IDS.RhinoMtlsCore.NonProduction
{
    public static class InternalUtilities
    {

        public static Guid AddObject(Mesh obj, string layerName)
        {
            return AddObject(obj, layerName, layerName);
        }

        public static Guid AddObject(Mesh obj, string objName, string layerName)
        {
            if (obj == null)
            {
                return Guid.Empty;
            }

            var doc = RhinoDoc.ActiveDoc;

            if (doc.Layers.Find(layerName, false) < 0)
            {
                doc.Layers.Add(layerName, Color.Firebrick);
            }

            var oa = new ObjectAttributes
            {
                LayerIndex = doc.GetLayerWithPath(layerName + "::" + objName),
                MaterialSource = ObjectMaterialSource.MaterialFromObject,
                ColorSource = ObjectColorSource.ColorFromMaterial,
                Name = objName
            };

            return doc.Objects.AddMesh(obj, oa);
        }

        public static Guid AddObject(Brep obj, string objName, string layerName)
        {
            if (obj == null)
            {
                return Guid.Empty;
            }

            var doc = RhinoDoc.ActiveDoc;

            if (doc.Layers.Find(layerName, false) < 0)
            {
                doc.Layers.Add(layerName, Color.Firebrick);
            }

            var oa = new ObjectAttributes
            {
                LayerIndex = doc.GetLayerWithPath(layerName + "::" + objName),
                MaterialSource = ObjectMaterialSource.MaterialFromObject,
                ColorSource = ObjectColorSource.ColorFromMaterial,
                Name = objName
            };

            return doc.Objects.AddBrep(obj, oa);
        }

        public static Guid AddObject(Surface obj, string objName, string layerName)
        {
            if (obj == null)
            {
                return Guid.Empty;
            }

            var doc = RhinoDoc.ActiveDoc;

            if (doc.Layers.Find(layerName, false) < 0)
            {
                doc.Layers.Add(layerName, Color.Firebrick);
            }

            var oa = new ObjectAttributes
            {
                LayerIndex = doc.GetLayerWithPath(layerName + "::" + objName),
                MaterialSource = ObjectMaterialSource.MaterialFromObject,
                ColorSource = ObjectColorSource.ColorFromMaterial,
                Name = objName
            };

            return doc.Objects.AddSurface(obj, oa);
        }

        public static Guid AddObject(Curve obj, string objName, string layerName)
        {
            if (obj == null)
            {
                return Guid.Empty;
            }

            var doc = RhinoDoc.ActiveDoc;

            if (doc.Layers.Find(layerName, false) < 0)
            {
                doc.Layers.Add(layerName, Color.Aquamarine);
            }

            var oa = new ObjectAttributes
            {
                LayerIndex = doc.GetLayerWithPath(layerName + "::" + objName),
                MaterialSource = ObjectMaterialSource.MaterialFromObject,
                ColorSource = ObjectColorSource.ColorFromMaterial,
                Name = objName
            };

            return doc.Objects.AddCurve(obj, oa);
        }

        public static Guid AddObject(Brep obj, string layerName)
        {
            return AddObject(obj, layerName, layerName);
        }

        public static Brep[] SmartBooleanUnion(Brep[] breps)
        {
            const int maxIterations = 20;
            var currIteration = 0;
            var tolerance = 0.01;

            Brep[] result = null;

            do
            {
                result = Brep.CreateBooleanUnion(breps, tolerance);
                tolerance /= 2;
                currIteration++;
            } while (result == null && currIteration < maxIterations);

            return result;
        }

        public static ArrowConduit AddVector(Point3d ptStart, Vector3d vec, double length, Color color)
        {
            vec.Unitize();

            var cond = new ArrowConduit(ptStart, vec, length, color) {Enabled = true};
            return cond;
        }

        public static PointConduit AddPoint(Point3d pt, string name, Color color)
        {
            var cond = new PointConduit(pt, name, color) {Enabled = true};
            return cond;
        }

        public static PlaneConduit AddPlane(Plane plane, Color color, int size)
        {
            var cond = new PlaneConduit();
            cond.SetPlane(plane, size);
            cond.SetColor(color);
            cond.Enabled = true;
            return cond;
        }

        public static PlaneConduit AddPlane(Point3d origin, Vector3d normal, Color color, int size)
        {
            var cond = new PlaneConduit();
            cond.SetPlane(new Plane(origin, normal), size);
            cond.SetColor(color);
            cond.Enabled = true;
            return cond;
        }

        public static void AddPlaneAsCoordinateSystem(Plane plane, int size)
        {
            PlaneConduit dummyPlane;
            ArrowConduit dummyX, dummyY, dummyZ;

            AddPlaneAsCoordinateSystem(plane, Color.Blue, size, out dummyPlane, out dummyX, out dummyY, out dummyZ);
            dummyPlane.Enabled = false;
        }

        public static void AddPlaneAsCoordinateSystem(Plane plane, Color color, int size, out PlaneConduit planeCond, out ArrowConduit xAxis, out ArrowConduit yAxis, out ArrowConduit zAxis)
        {
            //Make the Plane
            planeCond = new PlaneConduit();
            planeCond.SetPlane(plane, size);
            planeCond.SetColor(color);
            planeCond.Enabled = true;

            //The axes
            xAxis = new ArrowConduit(plane.Origin, plane.XAxis, (double)size / 4, Color.Red) { Enabled = true };
            yAxis = new ArrowConduit(plane.Origin, plane.YAxis, (double)size / 4, Color.Green) { Enabled = true };
            zAxis = new ArrowConduit(plane.Origin, plane.ZAxis, (double)size / 4, Color.Blue) { Enabled = true };
        }

        public static void ExportPlaneXml(Plane plane, string name, string path)
        {
            var xmlDoc = new XmlDocument();

            var nodeDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", string.Empty);
            xmlDoc.AppendChild(nodeDeclaration);

            var nodeEntities = xmlDoc.CreateElement("Entities");
            var attrMaterialise = xmlDoc.CreateAttribute("xmlns", "mat", "http://www.w3.org/2000/xmlns/");
            attrMaterialise.Value = "urn:materialise";
            nodeEntities.Attributes.Append(attrMaterialise);
            xmlDoc.AppendChild(nodeEntities);

            nodeEntities.AppendChild(CreatePlaneNode(xmlDoc, plane, name));

            xmlDoc.Save(path + name + ".xml");
        }

        private static XmlNode CreatePlaneNode(XmlDocument xmlDoc, Plane plane, string name)
        {
            const double planeSize = 50.0;
            var nodePlane = xmlDoc.CreateElement("Plane");

            var nodeName = xmlDoc.CreateElement("Name");

            nodeName.InnerText = name;
            nodePlane.AppendChild(nodeName);

            var nodeOrigin = xmlDoc.CreateElement("Origin");
            nodeOrigin.InnerText = string.Format(CultureInfo.InvariantCulture, "{0:F16} {1:F16} {2:F16}", plane.Origin.X, plane.Origin.Y, plane.Origin.Z);
            nodePlane.AppendChild(nodeOrigin);

            var nodeNormal = xmlDoc.CreateElement("Normal");
            nodeNormal.InnerText = string.Format(CultureInfo.InvariantCulture, "{0:F16} {1:F16} {2:F16}", plane.Normal.X, plane.Normal.Y, plane.Normal.Z);
            nodePlane.AppendChild(nodeNormal);

            var nodeXaxis = xmlDoc.CreateElement("X-axis");
            nodeXaxis.InnerText = string.Format(CultureInfo.InvariantCulture, "{0:F16} {1:F16} {2:F16}", plane.XAxis.X * planeSize, plane.XAxis.Y * planeSize, plane.XAxis.Z * planeSize);
            nodePlane.AppendChild(nodeXaxis);

            var nodeYaxis = xmlDoc.CreateElement("Y-axis");
            nodeYaxis.InnerText = string.Format(CultureInfo.InvariantCulture, "{0:F16} {1:F16} {2:F16}", plane.YAxis.X * planeSize, plane.YAxis.Y * planeSize, plane.YAxis.Z * planeSize);
            nodePlane.AppendChild(nodeYaxis);

            return nodePlane;
        }

        public static Guid AddCurve(Curve curve, string objName, string layerName, Color color)
        {
            if (curve == null)
            {
                return Guid.Empty;
            }

            var doc = RhinoDoc.ActiveDoc;

            var layering = layerName + "::" + objName;

            if (doc.Objects.FindByLayer(objName) != null)
            {
                var obj = doc.Objects.FindByLayer(objName).ToList();
                obj.ForEach(x => doc.Objects.Delete(x.Id, true));
            }

            if (doc.GetLayerWithPath(layering) > 0)
            {
                doc.Layers.Delete(doc.GetLayerWithPath(layering), true);
            }

            var oa = new ObjectAttributes
            {
                LayerIndex = doc.GetLayerWithPath(layering),
                ObjectColor = color,
                MaterialSource = ObjectMaterialSource.MaterialFromObject,
                ColorSource = ObjectColorSource.ColorFromObject,
                Name = objName
            };

            return doc.Objects.AddCurve(curve, oa);
        }

        public static Guid AddPoint(Point3d pt, string objName, string layerName, Color color)
        {
            if (!pt.IsValid)
            {
                return Guid.Empty;
            }

            var doc = RhinoDoc.ActiveDoc;

            var layering = layerName + "::" + objName;

            var oa = new ObjectAttributes
            {
                LayerIndex = doc.GetLayerWithPath(layering),
                ObjectColor = color,
                MaterialSource = ObjectMaterialSource.MaterialFromObject,
                ColorSource = ObjectColorSource.ColorFromObject,
                Name = objName
            };

            return doc.Objects.AddPoint(pt, oa);
        }

        public static Guid AddLine(Line line, string objName, string layerName, Color color)
        {
            var doc = RhinoDoc.ActiveDoc;

            var layering = layerName + "::" + objName;

            if (doc.Objects.FindByLayer(objName) != null)
            {
                var obj = doc.Objects.FindByLayer(objName).ToList();
                obj.ForEach(x => doc.Objects.Delete(x.Id, true));
            }

            if (doc.GetLayerWithPath(layering) > 0)
            {
                doc.Layers.Delete(doc.GetLayerWithPath(layering), true);
            }

            var oa = new ObjectAttributes
            {
                LayerIndex = doc.GetLayerWithPath(layering),
                ObjectColor = color,
                MaterialSource = ObjectMaterialSource.MaterialFromObject,
                ColorSource = ObjectColorSource.ColorFromObject,
                Name = objName
            };

            return doc.Objects.AddLine(line, oa);
        }
    }
}
