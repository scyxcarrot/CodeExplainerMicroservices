using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Enumerators;
using IDS.Core.Importer;
using IDS.Core.Operations;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Glenius;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

namespace IDSPIGlenius.Commands.Internal
{
#if (INTERNAL)

    [System.Runtime.InteropServices.Guid("5FF5EACC-CA43-4E64-99D4-F01E13A8FBED")]
    [IDSGleniusCommandAttribute(DesignPhase.Any, IBB.Screw)]
    public class GleniusTest_ExportScrewInformation : CommandBase<GleniusImplantDirector>
    {
        public GleniusTest_ExportScrewInformation()
        {
            Instance = this;
        }
        
        public static GleniusTest_ExportScrewInformation Instance { get; private set; }

        public override string EnglishName => "GleniusTest_ExportScrewInformation";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            var dialog = new FolderBrowserDialog
            {
                Description = "Select Destination to Export Screw Information"
            };

            if (dialog.ShowDialog() != DialogResult.OK)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Aborted.");
                return Result.Failure;
            }

            var folderPath = Path.GetFullPath(dialog.SelectedPath);
            var caseId = director.caseId;
            var screwManager = new ScrewManager(director.Document);
            var screwList = screwManager.GetAllScrews().ToList();

            var xmlPath = GenericScrewImportExport.ExportMimicsXml(caseId, screwList, folderPath);

            var suffix = "Exported4Testing";
            var prefix = caseId;
            var xmlTestingPath = $"{folderPath}\\{prefix}_Screws_{suffix}.xml";

            EditXML(xmlPath, xmlTestingPath, screwList, director);
            ExportScrews(folderPath, screwList);

            SystemTools.OpenExplorerInFolder(folderPath);

            RhinoApp.WriteLine("Screws were exported");
            return Result.Success;
        }

        private void EditXML(string originalXmlPath, string newXmlPath, List<Screw> screwList, GleniusImplantDirector director)
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(originalXmlPath);

            var nodeEntities = xmlDoc.SelectSingleNode("Entities");

            foreach (var screw in screwList)
            {
                var additionalNodes = WriteAdditionalInformation(xmlDoc, screw);
                foreach (var node in additionalNodes)
                {
                    nodeEntities.AppendChild(node);
                }
            }

            //Ideal screw head alignment plane
            var placementPlaneGenerator = new ScrewPlacementPlaneGenerator(director);
            var idealPlacementPlane = placementPlaneGenerator.GenerateHeadConstraintPlane();
            var generator = new ImplantFileNameGenerator(director);
            var idealPlacementPlaneNode = XmlEntitiesCreator.CreatePlaneNode(xmlDoc, idealPlacementPlane, generator.GenerateFileName("IdealScrewHeadPlacementPlane"));
            nodeEntities.AppendChild(idealPlacementPlaneNode);

            xmlDoc.Save(newXmlPath);
            File.Delete(originalXmlPath);
        }

        private List<XmlNode> WriteAdditionalInformation(XmlDocument xmlDoc, Screw screw)
        {
            /*
            1.Screw Head Point
            2.Screw Head Center Point
            3.Body origin Point
            4.Tip Point
            5.Point where the screw axis first enters the bone
            6.Last point where the screw axis breaks out of the bone
            */

            var list = new List<XmlNode>();

            var genericName = screw.GenerateNameForMimics();

            var headPointNode = XmlEntitiesCreator.CreatePoint3DNode(xmlDoc, screw.HeadPoint, $"{genericName}_HeadPoint");
            list.Add(headPointNode);

            var headCenterPointNode = XmlEntitiesCreator.CreatePoint3DNode(xmlDoc, screw.headCenter, $"{genericName}_HeadCenterPoint");
            list.Add(headCenterPointNode);

            var bodyOrigin = screw.BodyOrigin;
            var bodyOriginPointNode = XmlEntitiesCreator.CreatePoint3DNode(xmlDoc, bodyOrigin, $"{genericName}_BodyOriginPoint");
            list.Add(bodyOriginPointNode);

            var tipPointNode = XmlEntitiesCreator.CreatePoint3DNode(xmlDoc, screw.TipPoint, $"{genericName}_TipPoint");
            list.Add(tipPointNode);

            var direction = screw.Direction;
            var distanceUntilBone = screw.GetDistanceUntilBone();
            var firstEnterBonePoint = Point3d.Add(bodyOrigin, Vector3d.Multiply(direction, distanceUntilBone));
            var firstEnterBonePointNode = XmlEntitiesCreator.CreatePoint3DNode(xmlDoc, firstEnterBonePoint, $"{genericName}_FirstEnterBonePoint");
            list.Add(firstEnterBonePointNode);

            var distanceInBone = screw.GetDistanceInBone();
            var lastBreakOutBonePoint = Point3d.Add(firstEnterBonePoint, Vector3d.Multiply(direction, distanceInBone));
            var lastBreakOutBonePointNode = XmlEntitiesCreator.CreatePoint3DNode(xmlDoc, lastBreakOutBonePoint, $"{genericName}_LastBreakOutBonePoint");
            list.Add(lastBreakOutBonePointNode);

            return list;
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
