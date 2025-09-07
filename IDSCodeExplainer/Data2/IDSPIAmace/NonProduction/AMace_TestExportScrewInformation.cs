using IDS.Amace;
using IDS.Amace.Enumerators;
using IDS.Amace.FileSystem;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Operations;
using IDS.Common;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace IDS.NonProduction.Commands
{
#if (INTERNAL)

    [System.Runtime.InteropServices.Guid("ED1808C2-A3FD-4AD2-9FC8-CC5FC1D8AA6E")]
    [IDSCommandAttributes(true, DesignPhase.Any, IBB.Screw)]
    public class AMace_TestExportScrewInformation : Command
    {
        public AMace_TestExportScrewInformation()
        {
            Instance = this;
        }

        public static AMace_TestExportScrewInformation Instance { get; private set; }

        public override string EnglishName => "AMace_TestExportScrewInformation";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var director = IDSPluginHelper.GetDirector<ImplantDirector>(doc.DocumentId);
            if (director == null || !director.IsCommandRunnable(this, true))
            {
                return Result.Failure;
            }

            var workingDir = DirectoryStructure.GetWorkingDir(director.Document);
            var suffix = "Exported4Testing";
            var caseId = director.Inspector.CaseId;
            var prefix = caseId;
            var xmlPath = $"{workingDir}\\{prefix}_Screws_{suffix}.xml";

            var screwManager = new ScrewManager(director.Document);
            var screwList = screwManager.GetAllScrews().ToList();
            WriteXML(xmlPath, screwList, caseId);
            ExportScrews(workingDir, screwList);

            var openedFolder = SystemTools.OpenExplorerInFolder(workingDir);
            if (!openedFolder)
            {
                return Result.Failure;
            }

            RhinoApp.WriteLine("Screws were exported to the following file:");
            RhinoApp.WriteLine("{0}", xmlPath);
            return Result.Success;
        }

        private void WriteXML(string xmlPath, List<Screw> screwList, string caseId)
        {
            var xmlDoc = new XmlDocument();

            var nodeDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", string.Empty);
            xmlDoc.AppendChild(nodeDeclaration);

            var nodeEntities = xmlDoc.CreateElement("Entities");
            var attrMaterialise = xmlDoc.CreateAttribute("xmlns", "mat", "http://www.w3.org/2000/xmlns/");
            attrMaterialise.Value = "urn:materialise";
            nodeEntities.Attributes.Append(attrMaterialise);
            xmlDoc.AppendChild(nodeEntities);
            
            foreach (var screw in screwList)
            {
                var screwNode = ScrewExporter.WriteCylinderXml(xmlDoc, screw, caseId);
                nodeEntities.AppendChild(screwNode);

                var additionalNodes = WriteAdditionalInformation(xmlDoc, screw, caseId);
                foreach (var node in additionalNodes)
                {
                    nodeEntities.AppendChild(node);
                }
            }

            xmlDoc.Save(xmlPath);
        }

        private List<XmlNode> WriteAdditionalInformation(XmlDocument xmlDoc, Screw screw, string caseId)
        {
            /*
            1.Calibration Head Point
            2.Screw Head Point
            3.Body origin Point
            4.Tip Point
            5.Point where the screw axis first enters the bone
            6.last point where the screw axis breaks out of the bone
            */

            var list = new List<XmlNode>();

            var calibrationHeadPointNode = WritePoint(xmlDoc, screw.HeadCalibrationPoint, $"{caseId}_{screw.screwBrandType}_{screw.screwAlignment}_{screw.Index:D}_CalibrationHeadPoint");
            list.Add(calibrationHeadPointNode);

            var headPointNode = WritePoint(xmlDoc, screw.HeadPoint, $"{caseId}_{screw.screwBrandType}_{screw.screwAlignment}_{screw.Index:D}_HeadPoint");
            list.Add(headPointNode);

            var bodyOrigin = screw.BodyOrigin;
            var bodyOriginPointNode = WritePoint(xmlDoc, bodyOrigin, $"{caseId}_{screw.screwBrandType}_{screw.screwAlignment}_{screw.Index:D}_BodyOriginPoint");
            list.Add(bodyOriginPointNode);

            var tipPointNode = WritePoint(xmlDoc, screw.TipPoint, $"{caseId}_{screw.screwBrandType}_{screw.screwAlignment}_{screw.Index:D}_TipPoint");
            list.Add(tipPointNode);

            var direction = screw.Direction;
            var distanceUntilBone = screw.GetDistanceUntilBone();
            var firstEnterBonePoint = Point3d.Add(bodyOrigin, Vector3d.Multiply(direction, distanceUntilBone));
            var firstEnterBonePointNode = WritePoint(xmlDoc, firstEnterBonePoint, $"{caseId}_{screw.screwBrandType}_{screw.screwAlignment}_{screw.Index:D}_FirstEnterBonePoint");
            list.Add(firstEnterBonePointNode);

            var distanceInBone = screw.GetDistanceInBone();
            var lastBreakOutBonePoint = Point3d.Add(firstEnterBonePoint, Vector3d.Multiply(direction, distanceInBone));
            var lastBreakOutBonePointNode = WritePoint(xmlDoc, lastBreakOutBonePoint, $"{caseId}_{screw.screwBrandType}_{screw.screwAlignment}_{screw.Index:D}_LastBreakOutBonePoint");
            list.Add(lastBreakOutBonePointNode);

            return list;
        }

        private XmlNode WritePoint(XmlDocument xmlDoc, Point3d point, string name)
        {
            var nodePoint = xmlDoc.CreateElement("Point");

            var nodeName = xmlDoc.CreateElement("Name");
            nodeName.InnerText = name;
            nodePoint.AppendChild(nodeName);

            var nodeCoordinate = xmlDoc.CreateElement("Coordinate");
            nodeCoordinate.InnerText = string.Format(CultureInfo.InvariantCulture, "{0:F8} {1:F8} {2:F8}", point.X, point.Y, point.Z);
            nodePoint.AppendChild(nodeCoordinate);

            return nodePoint;
        }

        private void ExportScrews(string exportDir, List<Screw> screwList)
        {
            var meshParameters = MeshParameters.IDS();

            foreach (var screw in screwList)
            {
                var mesh = new Mesh();
                var brep = (Brep)screw.Geometry;
                mesh.Append(brep.GetCollisionMesh(meshParameters));

                var filePath = $"{exportDir}\\Screw_{screw.Index}.stl";
                StlUtilities.RhinoMesh2StlBinary(mesh, filePath);
            }
        }
    }

#endif
}
