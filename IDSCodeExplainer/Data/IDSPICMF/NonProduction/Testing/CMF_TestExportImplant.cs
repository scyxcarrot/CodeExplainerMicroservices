using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.CommandHelpers;
using IDS.CMF.DataModel;
using IDS.CMF.Enumerators;
using IDS.CMF.FileSystem;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.CMF.V2.DataModel;
using IDS.Core.Operations;
using IDS.Core.Utilities;
using IDS.RhinoInterfaces.Converter;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace IDS.PICMF.NonProduction
{
#if (STAGING)

    [System.Runtime.InteropServices.Guid("711D5B54-2387-4DDF-A551-E8C9FE95A46A")]
    [IDSCMFCommandAttributes(DesignPhase.Any)]
    public class CMF_TestExportImplant : CmfCommandBase
    {
        public CMF_TestExportImplant()
        {
            Instance = this;
        }

        public static CMF_TestExportImplant Instance { get; private set; }

        public override string EnglishName => "CMF_TestExportImplant";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var workingDir = DirectoryStructure.GetWorkingDir(director.Document);
            var caseId = director.caseId;
            var prefix = caseId;

            var objectManager = new CMFObjectManager(director);
            var implantComponent = new ImplantCaseComponent();

            foreach (var casePreferenceData in director.CasePrefManager.CasePreferences)
            {
                var suffix = casePreferenceData.CaseGuid;
                var xmlPath = $"{workingDir}\\{prefix}_Implant_{suffix}.xml";

                var screwBuildingBlock = implantComponent.GetImplantBuildingBlock(IBB.Screw, casePreferenceData);
                var screws = objectManager.GetAllBuildingBlocks(screwBuildingBlock).Select(s => (Screw)s).ToList();
                WriteXMLFor3matic(xmlPath, casePreferenceData, screws);

                var extendedBuildingBlock = implantComponent.GetImplantBuildingBlock(IBB.PlanningImplant, casePreferenceData);
                var implant = objectManager.GetBuildingBlock(extendedBuildingBlock);
                if (implant != null)
                {
                    ExportImplant(workingDir, implant);
                }
            }

            WriteXMLForTesting($"{workingDir}\\{prefix}_Implants.xml", director.CasePrefManager.CasePreferences);

            var openedFolder = SystemTools.OpenExplorerInFolder(workingDir);
            if (!openedFolder)
            {
                return Result.Failure;
            }

            RhinoApp.WriteLine("Implant(s) were exported to the following folder:");
            RhinoApp.WriteLine("{0}", workingDir);
            return Result.Success;
        }
    
        private void WriteXMLFor3matic(string xmlPath, CasePreferenceDataModel casePreferenceData, List<Screw> screws)
        {
            var xmlDoc = new XmlDocument();

            var nodeDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", string.Empty);
            xmlDoc.AppendChild(nodeDeclaration);

            var nodeEntities = xmlDoc.CreateElement("Entities");
            var attrMaterialise = xmlDoc.CreateAttribute("xmlns", "mat", "http://www.w3.org/2000/xmlns/");
            attrMaterialise.Value = "urn:materialise";
            nodeEntities.Attributes.Append(attrMaterialise);
            xmlDoc.AppendChild(nodeEntities);

            var dataModel = casePreferenceData.ImplantDataModel;
            foreach (var dot in dataModel.DotList)
            {
                var location = RhinoPoint3dConverter.ToPoint3d(dot.Location);
                var direction = RhinoVector3dConverter.ToVector3d(dot.Direction);

                var plane = new Plane(location, direction);
                var dotNode = XmlEntitiesCreator.CreatePlaneNode(xmlDoc, plane, $"{dot.GetType().Name}-Plane", 2.0);
                nodeEntities.AppendChild(dotNode);

                var pointNode = XmlEntitiesCreator.CreatePoint3DNode(xmlDoc, location, $"{dot.GetType().Name}-Point");
                nodeEntities.AppendChild(pointNode);

                var line = new Line(location, Vector3d.Multiply(direction, 5.0));
                var lineNode = XmlEntitiesCreator.CreateLineNode(xmlDoc, line, "TriangleNormal");
                nodeEntities.AppendChild(lineNode);

                if (dot is DotPastille && ((DotPastille)dot).Screw != null)
                {
                    var screwDiameter = casePreferenceData.CasePrefData.PastilleDiameter;
                    var screw = screws.First(s => s.Id == ((DotPastille)dot).Screw.Id);
                    var sphere = new AnalyticSphere
                    {
                        CenterPoint = screw.HeadPoint,
                        Radius = screwDiameter / 2
                    };
                    var sphereNode = XmlEntitiesCreator.CreateSphereNode(xmlDoc, sphere, "Screw");
                    nodeEntities.AppendChild(sphereNode);
                }
            }
            
            foreach (var connection in dataModel.ConnectionList)
            {
                var line = DataModelUtilities.CreateLine(connection.A.Location, connection.B.Location);
                var lineNode = XmlEntitiesCreator.CreateLineNode(xmlDoc, line, connection.GetType().Name);
                nodeEntities.AppendChild(lineNode);
            }

            xmlDoc.Save(xmlPath);
        }

        private void ExportImplant(string exportDir, RhinoObject implant)
        {
            var meshParameters = MeshParameters.IDS();

            var mesh = new Mesh();
            var brep = (Brep)implant.Geometry;
            mesh.Append(brep.GetCollisionMesh(meshParameters));

            var filePath = $"{exportDir}\\Implant_{implant.Id}.stl";
            StlUtilities.RhinoMesh2StlBinary(mesh, filePath);
        }

        private void WriteXMLForTesting(string xmlPath, List<CasePreferenceDataModel> casePreferences)
        {
            var xmlDoc = new XmlDocument();

            var rootNode = xmlDoc.CreateElement("Implants");
            xmlDoc.AppendChild(rootNode);

            foreach (var casePreferenceData in casePreferences)
            {
                AddCasePreferenceNode(xmlDoc, rootNode, casePreferenceData);
            }

            xmlDoc.Save(xmlPath);
        }

        private void AddCasePreferenceNode(XmlDocument xmlDoc, XmlElement rootNode, CasePreferenceDataModel casePreferenceData)
        {
            var implantNode = xmlDoc.CreateElement("implant");

            var nameAttr = xmlDoc.CreateAttribute("name");
            nameAttr.Value = casePreferenceData.CasePrefData.ImplantTypeValue;
            implantNode.Attributes.Append(nameAttr);

            var nodeNumber = xmlDoc.CreateElement("implant_number");
            nodeNumber.InnerText = $"{casePreferenceData.NCase}";
            implantNode.AppendChild(nodeNumber);

            var nodeConnections = xmlDoc.CreateElement("list_of_connections");
            var dataModel = casePreferenceData.ImplantDataModel;
            foreach (var connection in dataModel.ConnectionList)
            {
                var nodeConnection = xmlDoc.CreateElement("connection");

                var nodeConnectionType = xmlDoc.CreateElement("type");
                nodeConnectionType.InnerText = (connection is ConnectionLink) ? "link" : "plate";
                nodeConnection.AppendChild(nodeConnectionType);

                var locationA = RhinoPoint3dConverter.ToPoint3d(connection.A.Location);
                var pointA = xmlDoc.CreateElement("pointA");
                pointA.InnerText = string.Format(CultureInfo.InvariantCulture, "[{0:F8},{1:F8},{2:F8}]", locationA.X, locationA.Y, locationA.Z);

                var pointATypeAttr = xmlDoc.CreateAttribute("type");
                pointATypeAttr.Value = (connection.A is DotControlPoint) ? "controlpoint" : "pastille";
                pointA.Attributes.Append(pointATypeAttr);

                nodeConnection.AppendChild(pointA);

                var locationB = RhinoPoint3dConverter.ToPoint3d(connection.B.Location);
                var pointB = xmlDoc.CreateElement("pointB");
                pointB.InnerText = string.Format(CultureInfo.InvariantCulture, "[{0:F8},{1:F8},{2:F8}]", locationB.X, locationB.Y, locationB.Z);

                var pointBTypeAttr = xmlDoc.CreateAttribute("type");
                pointBTypeAttr.Value = (connection.B is DotControlPoint) ? "controlpoint" : "pastille";
                pointB.Attributes.Append(pointBTypeAttr);

                nodeConnection.AppendChild(pointB);

                var nodeConnectionWidth = xmlDoc.CreateElement("width");
                nodeConnectionWidth.InnerText = string.Format(CultureInfo.InvariantCulture, "{0:F8}", connection.Width);
                nodeConnection.AppendChild(nodeConnectionWidth);

                nodeConnections.AppendChild(nodeConnection);
            }
            implantNode.AppendChild(nodeConnections);

            var nodeScrewType = xmlDoc.CreateElement("screw_type");
            nodeScrewType.InnerText = casePreferenceData.CasePrefData.ScrewTypeValue;
            implantNode.AppendChild(nodeScrewType);

            var nodeScrewLength = xmlDoc.CreateElement("screw_length");
            nodeScrewLength.InnerText = string.Format(CultureInfo.InvariantCulture, "{0:F1}", casePreferenceData.CasePrefData.ScrewLengthMm);
            implantNode.AppendChild(nodeScrewLength);

            var nodePlateWidth = xmlDoc.CreateElement("plate_width");
            nodePlateWidth.InnerText = string.Format(CultureInfo.InvariantCulture, "{0:F8}", casePreferenceData.CasePrefData.PlateWidthMm);
            implantNode.AppendChild(nodePlateWidth);

            var nodeLinkWidth = xmlDoc.CreateElement("link_width");
            nodeLinkWidth.InnerText = string.Format(CultureInfo.InvariantCulture, "{0:F8}", casePreferenceData.CasePrefData.LinkWidthMm);
            implantNode.AppendChild(nodeLinkWidth);

            rootNode.AppendChild(implantNode);
        }
    }

#endif
}
