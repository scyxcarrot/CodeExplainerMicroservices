using IDS.Core.Operations;
using IDS.Core.Utilities;
using IDS.Glenius.ImplantBuildingBlocks;
using Rhino.Geometry;
using System.Xml;

namespace IDS.Glenius.Operations
{
    public static class MedicalCoordinateSystemXMLGenerator
    {
        public static XmlDocument GenerateXMLDocument(GleniusImplantDirector director)
        {
            var implantDerivedEntities = new ImplantDerivedEntities(director);

            var metalBackingPlane = implantDerivedEntities.GetMetalBackingPlane();
            var angInf = director.AnatomyMeasurements.AngleInf;
            var trig = director.AnatomyMeasurements.Trig;
            var glenoidPlane = director.AnatomyMeasurements.PlGlenoid;

            var processor = new ReconstructionMeasurementProcessor(angInf,trig,glenoidPlane.Origin, glenoidPlane.Normal, director.defectIsLeft);

            Plane axialPlane, coronalPlane, sagittalPlane;
            processor.CalculateMCSPlane(out coronalPlane, out axialPlane, out sagittalPlane);

            //Backward compatibility, older MCS is not oriented properly so recreate it back, Assert it
            if (!MathUtilities.IsPlaneMathemathicallyEqual(axialPlane, director.AnatomyMeasurements.PlAxial) ||
                !MathUtilities.IsPlaneMathemathicallyEqual(coronalPlane, director.AnatomyMeasurements.PlCoronal) ||
                !MathUtilities.IsPlaneMathemathicallyEqual(sagittalPlane, director.AnatomyMeasurements.PlSagittal))
            {
                throw new Core.PluginHelper.IDSException("There has been inconsistencies with MCS Planes and this shouldn't happen! Be sure to check your case!");
            }

            var generator = new ImplantFileNameGenerator(director);

            var xmlDoc = new XmlDocument();

            var nodeDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", string.Empty);
            xmlDoc.AppendChild(nodeDeclaration);

            var nodeEntities = xmlDoc.CreateElement("Entities");
            var attrMaterialise = xmlDoc.CreateAttribute("xmlns", "mat", "http://www.w3.org/2000/xmlns/");
            attrMaterialise.Value = "urn:materialise";
            nodeEntities.Attributes.Append(attrMaterialise);
            xmlDoc.AppendChild(nodeEntities);

            nodeEntities.AppendChild(XmlEntitiesCreator.CreatePlaneNode(xmlDoc, axialPlane, generator.GenerateFileName("AxialPlane")));
            nodeEntities.AppendChild(XmlEntitiesCreator.CreatePlaneNode(xmlDoc, coronalPlane, generator.GenerateFileName("CoronalPlane")));
            nodeEntities.AppendChild(XmlEntitiesCreator.CreatePlaneNode(xmlDoc, sagittalPlane, generator.GenerateFileName("SagittalPlane")));
            nodeEntities.AppendChild(XmlEntitiesCreator.CreatePlaneNode(xmlDoc, metalBackingPlane, generator.GenerateFileName("MetalBackingPlane")));
            nodeEntities.AppendChild(XmlEntitiesCreator.CreatePoint3DNode(xmlDoc, angInf, generator.GenerateFileName("AngInf")));
            nodeEntities.AppendChild(XmlEntitiesCreator.CreatePoint3DNode(xmlDoc, trig, generator.GenerateFileName("Trig")));
            nodeEntities.AppendChild(XmlEntitiesCreator.CreatePlaneNode(xmlDoc, glenoidPlane, generator.GenerateFileName("GlenPlane")));
            if (director.PreopCor != null)
            {
                nodeEntities.AppendChild(XmlEntitiesCreator.CreateSphereNode(xmlDoc, director.PreopCor, generator.GenerateFileName("PreopCOR")));
            }

            return xmlDoc;
        }

        //This will include additional entities
        public static XmlDocument GenerateXMLDocumentExtended(GleniusImplantDirector director)
        {
            var xmlDoc = GenerateXMLDocument(director);

            var angInf = director.AnatomyMeasurements.AngleInf;
            var trig = director.AnatomyMeasurements.Trig;
            var glenoidPlane = director.AnatomyMeasurements.PlGlenoid;

            var processor = new ReconstructionMeasurementProcessor(angInf, trig, glenoidPlane.Origin, glenoidPlane.Normal, director.defectIsLeft);

            Vector3d axML, axAP, axIS;
            processor.CalculateMCSAxes(out axML, out axAP, out axIS);

            var generator = new ImplantFileNameGenerator(director);

            var lineLength = 50.0;
            var nodeEntities = xmlDoc.SelectSingleNode("Entities");
            nodeEntities.AppendChild(XmlEntitiesCreator.CreateLineNode(xmlDoc, LineUtilities.CreateLine(glenoidPlane.Origin, axAP, lineLength), generator.GenerateFileName("AP-axis")));
            nodeEntities.AppendChild(XmlEntitiesCreator.CreateLineNode(xmlDoc, LineUtilities.CreateLine(glenoidPlane.Origin, axML, lineLength), generator.GenerateFileName("ML-axis")));
            nodeEntities.AppendChild(XmlEntitiesCreator.CreateLineNode(xmlDoc, LineUtilities.CreateLine(glenoidPlane.Origin, axIS, lineLength), generator.GenerateFileName("IS-axis")));

            var objectManager = new GleniusObjectManager(director);
            var headBuildingBlock = objectManager.GetBuildingBlock(IBB.Head);
            if (headBuildingBlock != null)
            {
                var head = headBuildingBlock as Head;
                nodeEntities.AppendChild(XmlEntitiesCreator.CreatePoint3DNode(xmlDoc, head.CoordinateSystem.Origin, generator.GenerateFileName("HeadCOR")));
                
                var headCS = head.CoordinateSystem;
                nodeEntities.AppendChild(XmlEntitiesCreator.CreatePlaneNode(xmlDoc, headCS, generator.GenerateFileName("HeadCS")));
                nodeEntities.AppendChild(XmlEntitiesCreator.CreateLineNode(xmlDoc, LineUtilities.CreateLine(headCS.Origin, headCS.XAxis, lineLength), generator.GenerateFileName("HeadX-axis")));
                nodeEntities.AppendChild(XmlEntitiesCreator.CreateLineNode(xmlDoc, LineUtilities.CreateLine(headCS.Origin, headCS.YAxis, lineLength), generator.GenerateFileName("HeadY-axis")));
                nodeEntities.AppendChild(XmlEntitiesCreator.CreateLineNode(xmlDoc, LineUtilities.CreateLine(headCS.Origin, headCS.ZAxis, lineLength), generator.GenerateFileName("HeadZ-axis")));
            }

            return xmlDoc;
        }
    }
}
