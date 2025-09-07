using IDS.CMF.ExternalTools;
using IDS.CMF.Factory;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.Core.ImplantBuildingBlocks;
using IDS.Core.Operations;
using IDS.Core.Utilities;
using Rhino.Geometry;
using System.Collections.Generic;
using System.IO;

namespace IDS.CMF.Operations
{
    public class SourcesExporter
    {
        public const string SubFolderName = "Implant_Support";
        private readonly CMFImplantDirector _director;
        private readonly SupportSourcesExporterHelper _exporterHelper;

        public SourcesExporter(CMFImplantDirector director)
        {
            _director = director;
            _exporterHelper = new SupportSourcesExporterHelper(director);
        }
        
        public void ExportSources(string workingDir, bool exportMxp)
        {
            var directory = SystemTools.HandleCreateDirectory(workingDir, SubFolderName);
            ExportPlanningSources(directory);
            ExportPlanningImplant(directory);
            ExportImplantSupport(directory);
            ExportPreopSources(directory);
            ExportRegeneratedPlanningImplant(directory);
            ExportOriginalSources(directory);

            if(exportMxp)
            {
                ExportMxp(directory);
                return;
            }
            
            ExportManualConvertMxpFolder(workingDir);
        }

        private void ExportOriginalSources(string exportDir)
        {
            ExportOriginal(exportDir);
        }

        private void ExportPlanningSources(string exportDir)
        {
            ExportFinalStages(exportDir);
        }

        private void ExportPlanningImplant(string exportDir)
        {
            ExportPlanningImplants(exportDir);
        }

        private void ExportRegeneratedPlanningImplant(string exportDir)
        {
            ExportRegeneratedPlanningImplants(exportDir);
        }

        public void ExportImplantSupport(string exportDir)
        {
            ExportImplantSupports(exportDir);
        }

        public void ExportPreopSources(string exportDir)
        {
            ExportPreop(exportDir);
        }
        
        private static void ExportMxp(string exportDir)
        {
            var trimaticInteropImplantPhase = new TrimaticInteropImplantPhase();
            trimaticInteropImplantPhase.GenerateMxpFromStl(exportDir);
        }

        private static void ExportManualConvertMxpFolder(string exportDir)
        {
            var trimaticInteropImplantPhase = new TrimaticInteropImplantPhase();
            trimaticInteropImplantPhase.ExportStlToMxpManualConvertFolder(exportDir);
        }

        private void ExportFinalStages(string exportDir)
        {
            ExportMeshOrBrepByLayerName(Constants.ProPlanImport.PlannedLayer, exportDir);
        }

        private void ExportOriginal(string exportDir)
        {
            ExportMeshOrBrepByLayerName(Constants.ProPlanImport.OriginalLayer, exportDir);
        }

        private void ExportPreop(string exportDir)
        {
            ExportMeshOrBrepByLayerName(Constants.ProPlanImport.PreopLayer, exportDir);
        }

        private void ExportMeshOrBrepByLayerName(string layerName, string exportDir)
        {
            var blocks = _exporterHelper.GetBuildingBlocksOfMeshOrBrepByLayerForExport(layerName);
            ExportBuildingBlocks(blocks, exportDir);
        }

        private void ExportRegeneratedPlanningImplants(string exportDir)
        {
            foreach (var casePreferenceData in _director.CasePrefManager.CasePreferences)
            {
                var planningFactory = new PlanningImplantBrepFactory();
                var brep = planningFactory.CreateImplant(casePreferenceData.ImplantDataModel);
                //one of the reason returned brep is invalid: implant design not done yet
                if (!brep.IsValid)
                {
                    continue;
                }

                var fullPath = Path.Combine(exportDir, $"Implant_{casePreferenceData.NCase}_{casePreferenceData.CasePrefData.ImplantTypeValue}_UpdatedPlanning.stl");
                var color = Visualization.Colors.PlateTemporary;
                var stlColor = new int[] { color.R, color.G, color.B };
                StlUtilities.RhinoMesh2StlBinary(MeshUtilities.AppendMeshes(Mesh.CreateFromBrep(brep)), fullPath, stlColor);
            }
        }
        
        private void ExportPlanningImplants(string exportDir)
        {
            var blocks = new List<ImplantBuildingBlock>();
            var implantComponent = new ImplantCaseComponent();
            foreach (var casePreferenceData in _director.CasePrefManager.CasePreferences)
            {
                var extendedBuildingBlock = implantComponent.GetImplantBuildingBlock(IBB.PlanningImplant, casePreferenceData);
                var block = extendedBuildingBlock.Block;
                block.ExportName = casePreferenceData.CaseName;
                blocks.Add(block);
            }

            ExportBuildingBlocks(blocks, exportDir);
        }

        private void ExportImplantSupports(string exportDir)
        {
            var blocks = new List<ImplantBuildingBlock>();
            var implantComponent = new ImplantCaseComponent();
            foreach (var casePreferenceDataModel in _director.CasePrefManager.CasePreferences)
            {
                var extendedBuildingBlock =
                    implantComponent.GetImplantBuildingBlock(IBB.ImplantSupport, casePreferenceDataModel);
                var block = extendedBuildingBlock.Block;
                block.ExportName = $"ImplantSupport_I{casePreferenceDataModel.NCase}";
                blocks.Add(block);
            }

            ExportBuildingBlocks(blocks, exportDir);
        }

        private void ExportBuildingBlocks(List<ImplantBuildingBlock> blocks, string exportDir)
        {
            List<string> exportedFiles;
            BlockExporter.ExportBuildingBlocks(_director, blocks, exportDir, string.Empty, string.Empty, out exportedFiles);
        }
    }
}
