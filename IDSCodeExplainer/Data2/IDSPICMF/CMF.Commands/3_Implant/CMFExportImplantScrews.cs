using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.FileSystem;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Quality;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.UI;
using System;
using System.IO;
using System.Linq;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("3365F2CD-E653-484A-9340-9CC56EEEC1CA")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Implant, IBB.Screw)]
    public class CMFExportImplantScrews : CmfCommandBase
    {
        public CMFExportImplantScrews()
        {
            TheCommand = this;
        }

        public static CMFExportImplantScrews TheCommand { get; private set; }

        public override string EnglishName => "CMFExportImplantScrews";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var screwManager = new ScrewManager(director);
            if (!screwManager.IsAllImplantScrewsCalibrated())
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning,
                    "Implant screws not calibrated yet, " +
                    "exported screws might be incorrect!");
            }

            var workingDir = DirectoryStructure.GetWorkingDir(doc);

            ExportScrewsStl(director, workingDir);
            ExportScrewInfo(director, workingDir);

            var gaugesExported = ExportGaugesStl(director, workingDir);

            var screwCapsuleExporter = new ScrewCapsuleExporter();
            var capsulesExported = screwCapsuleExporter.ExportScrewCapsulesStl(director, workingDir);

            var allExported = gaugesExported && capsulesExported;
            if (allExported)
            {
                SystemTools.OpenExplorerInFolder(workingDir);
            }

            return allExported ? Result.Success : Result.Failure;
        }

        private static void ExportScrewsStl(CMFImplantDirector director, string exportPath)
        {
            var objectManager = new CMFObjectManager(director);
            var screwBreps = objectManager.GetAllBuildingBlocks(IBB.Screw).Select(screw => (Brep)screw.Geometry);
            var screwMeshs = screwBreps.Select(screw => MeshUtilities.ConvertBrepToMesh(screw, true));
            var allScrewsInOneMesh = MeshUtilities.AppendMeshes(screwMeshs);
            StlUtilities.RhinoMesh2StlBinary(allScrewsInOneMesh, $@"{exportPath}\{director.caseId}_Screws_Temporary.stl");
        }

        private static void ExportScrewInfo(CMFImplantDirector director, string exportPath)
        {
            var excelCreator = new ScrewTableExcelCreator(director);
            var writeScrewGroupsSuccess = excelCreator.WriteScrewGroups(exportPath);
            if (!writeScrewGroupsSuccess)
            {
                if (!excelCreator.ErrorMessages.Any())
                {
                    return;
                }

                var warningMessage = String.Join("\n-", excelCreator.ErrorMessages);
                Dialogs.ShowMessage(warningMessage, "Screw Table Creation Failed on Export Screws!",
                    ShowMessageButton.OK, ShowMessageIcon.Error);
            }
        }

        private static bool ExportGaugesStl(CMFImplantDirector director, string workingDir)
        {
            var exportDirectory = Path.Combine(workingDir, "Gauge_Temporary");
            if (Directory.Exists(exportDirectory) && !SystemTools.DeleteRecursively(exportDirectory))
            {
                return false;
            }
            Directory.CreateDirectory(exportDirectory);

            var objectManager = new CMFObjectManager(director);
            var gaugeExporter = new ScrewGaugeExporter();
            foreach (var casePreferenceData in director.CasePrefManager.CasePreferences)
            {
                var implantName = $"{director.caseId}_{casePreferenceData.CasePrefData.ImplantTypeValue}_I{casePreferenceData.NCase}";
                var exported = gaugeExporter.ExportImplantScrewGauges(casePreferenceData, objectManager, exportDirectory, implantName, "_Temporary");
                if (!exported)
                {
                    return false;
                }
            }

            return true;
        }
    }
}