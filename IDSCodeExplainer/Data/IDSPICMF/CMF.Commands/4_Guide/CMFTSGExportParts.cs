using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.CommandHelpers;
using IDS.CMF.DataModel;
using IDS.CMF.Enumerators;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.CMF.V2.Logics;
using IDS.Core.V2.Geometry;
using IDS.RhinoInterface.Converter;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.CMF
{
    [System.Runtime.InteropServices.Guid("4AD0F166-C9A0-4BA5-AF51-F8B45066A553")]
    [IDSCMFCommandAttributes(DesignPhase.TeethBlock, IBB.TeethBlock)]
    public class CMFTSGExportParts : CmfCommandBase
    {
        public CMFTSGExportParts()
        {
            TheCommand = this;
        }

        public static CMFTSGExportParts TheCommand { get; private set; }
        public override string EnglishName => "CMFTSGExportParts";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var exportTSGGeneralParts = false;

            // create folder and delete all items inside
            var exportTSGFolder = CreateExportTSGFolder(director);
            var filePaths = Directory.GetFiles(exportTSGFolder);
            foreach (var filePath in filePaths)
            {
                File.Delete(filePath);
            }

            foreach (var guidePreferenceDataModel in director.CasePrefManager.GuidePreferences)
            {
                ExportMeshesIfPresent(
                    director,
                    IBB.TeethBlock,
                    guidePreferenceDataModel,
                    "TeethBlock");

                var teethBaseRegionPresent = ExportTSGIntermediatePartsIfPresent(
                    director, guidePreferenceDataModel);

                // if there is one part with teethBase region, then means use IDS workflow
                // so we export the general parts
                exportTSGGeneralParts |= teethBaseRegionPresent;
            }

            if (exportTSGGeneralParts)
            {
                ExportGeneralTSGIntermediatePartsIfPresent(director);
            }

            return Result.Success;
        }

        private static void ExportGeneralTSGIntermediatePartsIfPresent(
            CMFImplantDirector director)
        {
            ExportCasts(director);

            var ibbExportAndNameMap = new Dictionary<IBB, string>()
            {
                {IBB.LimitingSurfaceMandible, "LimitingSurfaceMandible"},
                {IBB.LimitingSurfaceMaxilla, "LimitingSurfaceMaxilla"},
                {IBB.LimitingSurfaceExtrusionMandible, "LimitingSurfaceExtrusionMandible"},
                {IBB.LimitingSurfaceExtrusionMaxilla, "LimitingSurfaceExtrusionMaxilla"},
                {IBB.ReinforcementRegionMandible, "ReinforcementRegionMandible"},
                {IBB.ReinforcementRegionMaxilla, "ReinforcementRegionMaxilla"},
                {IBB.ReinforcementExtrusionMandible, "ReinforcementExtrusionMandible"},
                {IBB.ReinforcementExtrusionMaxilla, "ReinforcementExtrusionMaxilla"},
                {IBB.BracketRegionMandible, "BracketRegionMandible"},
                {IBB.BracketRegionMaxilla, "BracketRegionMaxilla"},
                {IBB.BracketExtrusionMandible, "BracketExtrusionMandible"},
                {IBB.BracketExtrusionMaxilla, "BracketExtrusionMaxilla"},
                {IBB.TeethBlockROIMandible, "TeethBlockROIMandible"},
                {IBB.TeethBlockROIMaxilla, "TeethBlockROIMaxilla"},
                {IBB.FinalSupportMandible, "FinalSupportMandible"},
                {IBB.FinalSupportMaxilla, "FinalSupportMaxilla"},
                {IBB.FinalSupportWrappedMandible, "FinalSupportWrappedMandible"},
                {IBB.FinalSupportWrappedMaxilla, "FinalSupportWrappedMaxilla"},
            };

            foreach (var ibbExportAndName in ibbExportAndNameMap)
            {
                ExportMeshesIfPresent(
                    director,
                    ibbExportAndName.Key,
                    ibbExportAndName.Value);
            }
        }

        private static void ExportCasts(CMFImplantDirector director)
        {
            var proPlanImportComponent = new ProPlanImportComponent();
            var originalCastBlocks =
                proPlanImportComponent.Blocks
                    .Where(x => x.PartType == ProPlanImportPartType.MandibleCast ||
                                x.PartType == ProPlanImportPartType.MaxillaCast)
                    .Where(b => ProPlanPartsUtilitiesV2.IsOriginalPart(b.PartNamePattern));
            var eIbbs =
                originalCastBlocks.Select(x => proPlanImportComponent.GetProPlanImportBuildingBlock(x.PartNamePattern));

            var exportTsgFolder = CreateExportTSGFolder(director);
            foreach (var eIbb in eIbbs)
            {
                var cast = TeethSupportedGuideUtilities.GetCast(director, eIbb);
                if (cast != null)
                {
                    StlUtilitiesV2.IDSMeshToStlBinary(cast, Path.Combine(exportTsgFolder, $"{eIbb.Block.Name}.stl"));
                }
            }
        }

        private static bool ExportTSGIntermediatePartsIfPresent(
            CMFImplantDirector director,
            GuidePreferenceDataModel guidePreferenceDataModel)
        {
            var ibbExportAndNameMap = new Dictionary<IBB, string>()
            {
                {IBB.TeethBaseRegion, "TeethBaseRegion"},
                {IBB.TeethBaseExtrusion, "TeethBaseExtrusion"},
            };

            var ibbExported = false;
            foreach (var ibbExportAndName in ibbExportAndNameMap)
            {
                ibbExported |= ExportMeshesIfPresent(
                    director,
                    ibbExportAndName.Key,
                    guidePreferenceDataModel,
                    ibbExportAndName.Value);
            }

            return ibbExported;
        }

        private static bool ExportMeshesIfPresent(
            CMFImplantDirector director,
            IBB ibbToExport,
            GuidePreferenceDataModel guidePreferenceDataModel,
            string exportName)
        {
            var objectManager = new CMFObjectManager(director);
            var guideCaseComponent = new GuideCaseComponent();
            var eIbbToExport =
                guideCaseComponent.GetGuideBuildingBlock(
                    ibbToExport, guidePreferenceDataModel);

            if (!objectManager.HasBuildingBlock(eIbbToExport))
            {
                return false;
            }

            var idsMeshes = objectManager.GetAllBuildingBlocks(eIbbToExport)
                .Select(rhinoObject => RhinoMeshConverter.ToIDSMesh((Mesh)rhinoObject.Geometry));

            var exportTSGFolder = CreateExportTSGFolder(director);
            TeethSupportedGuideUtilities.ExportMeshes(
                idsMeshes,
                exportTSGFolder,
                $"G{guidePreferenceDataModel.NCase}_{exportName}");

            return true;
        }

        private static void ExportMeshesIfPresent(
            CMFImplantDirector director,
            IBB ibbToExport,
            string exportName)
        {
            var objectManager = new CMFObjectManager(director);
            if (!objectManager.HasBuildingBlock(ibbToExport))
            {
                return;
            }

            var idsMeshes = objectManager.GetAllBuildingBlocks(ibbToExport)
                .Select(rhinoObject => RhinoMeshConverter.ToIDSMesh((Mesh)rhinoObject.Geometry));

            var exportTSGFolder = CreateExportTSGFolder(director);

            TeethSupportedGuideUtilities.ExportMeshes(
                idsMeshes,
                exportTSGFolder,
                exportName);
        }

        private static string CreateExportTSGFolder(CMFImplantDirector director)
        {
            var documentPath = Path.GetDirectoryName(director.Document.Path);
            var exportTSGPath = Path.Combine(documentPath, "ExportTSG");

            if (!Directory.Exists(exportTSGPath))
            {
                Directory.CreateDirectory(exportTSGPath);
            }

            return exportTSGPath;
        }
    }
}