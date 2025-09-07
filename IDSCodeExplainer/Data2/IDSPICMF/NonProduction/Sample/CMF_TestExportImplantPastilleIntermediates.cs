using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.CommandHelpers;
using IDS.CMF.CustomMainObjects;
using IDS.CMF.DataModel;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Preferences;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.Operations;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.RhinoInterfaces.Converter;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

namespace IDS.PICMF.NonProduction
{
#if (INTERNAL)

    [System.Runtime.InteropServices.Guid("264C3E78-DCC7-4280-A2ED-BD3DEF122077")]
    [IDSCMFCommandAttributes(DesignPhase.Any, IBB.Screw)]
    public class CMF_TestExportImplantPastilleIntermediates : CmfCommandBase
    {
        public CMF_TestExportImplantPastilleIntermediates()
        {
            Instance = this;
        }

        public static CMF_TestExportImplantPastilleIntermediates Instance { get; private set; }

        public override string EnglishName => "CMF_TestExportImplantPastilleIntermediates";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var dialog = new FolderBrowserDialog();
            dialog.Description = "Select a folder to export the implant pastille intermediate stls";
            var rc = dialog.ShowDialog();
            if (rc != DialogResult.OK)
            {
                return Result.Cancel;
            }

            var folderPath = Path.GetFullPath(dialog.SelectedPath);
            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"Selected folder: {folderPath}");

            var objectManager = new CMFObjectManager(director);
            var implantSupportManager = new ImplantSupportManager(objectManager);

            var screwManager = new ScrewManager(director);
            var allScrews = screwManager.GetAllScrews(false);

            var parameters = CMFPreferences.GetActualImplantParameters();

            foreach (var casePreference in director.CasePrefManager.CasePreferences)
            {
                var supportMesh = implantSupportManager.GetImplantSupportMesh(casePreference);
                if (supportMesh != null)
                {
                    supportMesh.FaceNormals.ComputeFaceNormals();
                    StlUtilities.RhinoMesh2StlBinary(supportMesh, $"{folderPath}\\ImplantSupport_I{casePreference.NCase}.stl");
                }

                var implantDataModel = casePreference.ImplantDataModel;
                if (implantDataModel != null && implantDataModel.DotList.Any())
                {
                    var pastilleList = implantDataModel.DotList.Where(dot => dot is DotPastille).ToList();

                    for (var i = 0; i < pastilleList.Count; i++)
                    {
                        var pastille = (DotPastille)pastilleList[i];

                        //Export:
                        //Current pastille's Location and Direction
                        //Adjusted pastille's Location and Direction
                        //Meshes that made up the offsetted pastille - top, bottom and side(stitch) surfaces

                        var currentLocation = RhinoPoint3dConverter.ToPoint3d(pastille.Location);
                        var currentDirection = RhinoVector3dConverter.ToVector3d(pastille.Direction);

                        var adjustedPastille = ImplantPastilleCreationUtilities.AdjustPastille(pastille, supportMesh, allScrews, parameters.IndividualImplantParams.PastillePlacementModifier);
                        var adjustedLocation = RhinoPoint3dConverter.ToPoint3d(adjustedPastille.Location);
                        var adjustedDirection = RhinoVector3dConverter.ToVector3d(adjustedPastille.Direction);

                        var currentPlane = new Plane(currentLocation, currentDirection);
                        var adjustedPlane = new Plane(adjustedLocation, adjustedDirection);

                        var currPastille = pastille;
                        var currScrew = allScrews.ToList().First(s => s.Id == currPastille.Screw.Id);

                        ExportPointsAndLines(currentPlane, adjustedPlane, folderPath, casePreference, i);

                        Mesh pastilleMesh;
                        Mesh pastilleCylinders = ImplantPastilleCreationUtilities.GeneratePastilleCylinderIntersectionMesh(parameters.IndividualImplantParams, adjustedPastille);
                        Mesh pastilleExtrudeCylinders = ImplantPastilleCreationUtilities.GeneratePastilleExtrudeCylinderIntersectionMesh(parameters.IndividualImplantParams, adjustedPastille);
                        Mesh top;
                        Mesh bottom;
                        Mesh stitched;
                        if (!ImplantPastilleCreationUtilities.GenerateImplantPastille(adjustedPastille, currScrew, casePreference.NCase,
                            parameters.IndividualImplantParams, supportMesh, supportMesh, i, out pastilleMesh, pastilleCylinders, pastilleExtrudeCylinders,
                            out top, out bottom, out stitched))
                        {
                            IDSPluginHelper.WriteLine(LogCategory.Error, $"Failed to get pastille {i} of {casePreference.CaseName}!");
                        }
                        else
                        {
                            StlUtilities.RhinoMesh2StlBinary(pastilleMesh, $"{folderPath}\\{casePreference.CaseName}_PastilleMesh-{i}.stl");
                            StlUtilities.RhinoMesh2StlBinary(pastilleCylinders, $"{folderPath}\\{casePreference.CaseName}_PastilleCylinder-{i}.stl");
                            StlUtilities.RhinoMesh2StlBinary(top, $"{folderPath}\\{casePreference.CaseName}_PastilleTop-{i}.stl");
                            StlUtilities.RhinoMesh2StlBinary(bottom, $"{folderPath}\\{casePreference.CaseName}_PastilleBottom-{i}.stl");
                            StlUtilities.RhinoMesh2StlBinary(stitched, $"{folderPath}\\{casePreference.CaseName}_PastilleSide-{i}.stl");
                        }
                    }
                }
            }

            return Result.Success;
        }

        private void ExportPointsAndLines(Plane currentPlane, Plane adjustedPlane, string folderPath, CasePreferenceDataModel casePreference, int index)
        {
            var xmlDoc = new XmlDocument();

            var nodeDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", string.Empty);
            xmlDoc.AppendChild(nodeDeclaration);

            var nodeEntities = xmlDoc.CreateElement("Entities");
            var attrMaterialise = xmlDoc.CreateAttribute("xmlns", "mat", "http://www.w3.org/2000/xmlns/");
            attrMaterialise.Value = "urn:materialise";
            nodeEntities.Attributes.Append(attrMaterialise);
            xmlDoc.AppendChild(nodeEntities);

            var currentLocationNode = XmlEntitiesCreator.CreatePoint3DNode(xmlDoc, currentPlane.Origin, $"{casePreference.CaseName}_CurrentLocation-{index}");
            nodeEntities.AppendChild(currentLocationNode);

            var currentLine = new Line(currentPlane.Origin, Vector3d.Multiply(currentPlane.Normal, 5.0));
            var currentLineNode = XmlEntitiesCreator.CreateLineNode(xmlDoc, currentLine, $"{casePreference.CaseName}_CurrentDirection-{index}");
            nodeEntities.AppendChild(currentLineNode);

            var adjustedLocationNode = XmlEntitiesCreator.CreatePoint3DNode(xmlDoc, adjustedPlane.Origin, $"{casePreference.CaseName}_AdjustedLocation-{index}");
            nodeEntities.AppendChild(adjustedLocationNode);

            var adjustedLine = new Line(adjustedPlane.Origin, Vector3d.Multiply(adjustedPlane.Normal, 5.0));
            var adjustedLineNode = XmlEntitiesCreator.CreateLineNode(xmlDoc, adjustedLine, $"{casePreference.CaseName}_AdjustedDirection-{index}");
            nodeEntities.AppendChild(adjustedLineNode);

            xmlDoc.Save($"{folderPath}\\{casePreference.CaseName}_LocationsAndDirections-{index}.xml");
        }
    }

#endif
}
