using IDS.CMF.CasePreferences;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.Core.Utilities;
using Rhino.Geometry;
using System.Drawing;
using System.IO;

namespace IDS.CMF.Quality
{
    public class CMFQCPlasticExporter
    {
        private readonly CMFImplantDirector _director;
        private readonly CMFObjectManager _objectManager;

        public CMFQCPlasticExporter(CMFImplantDirector director)
        {
            _director = director;
            _objectManager = new CMFObjectManager(director);
        }

        private void ExportStl(Mesh mesh, string outputDirectory, string fileName, Color color)
        {
            if (mesh.Faces.Count == 0)
            {
                return;
            }

            var filePath = Path.Combine(outputDirectory, $"{fileName}.stl");
            var stlColor = new int[] { color.R, color.G, color.B };
            StlUtilities.RhinoMesh2StlBinary(mesh, filePath, stlColor);
        }

        private void ExportExtendedBuildingBlock(ExtendedImplantBuildingBlock extendedBuildingBlock, string outputDirectory, string fileName, Color color)
        {
            var partObj = _objectManager.GetBuildingBlock(extendedBuildingBlock);
            if (partObj != null)
            {
                var mesh = (Mesh)partObj.DuplicateGeometry();
                ExportStl(mesh, outputDirectory, fileName, color);
                mesh.Dispose();
            }
        }

        private void ExportImplantPlasticEntity(CasePreferenceDataModel casePreferenceData, string outputDirectory, string caseFileNameTemplate, IBB intermediatePart, string partName)
        {
            var implantComponent = new ImplantCaseComponent();
            var partBuildingBlock = implantComponent.GetImplantBuildingBlock(intermediatePart, casePreferenceData);
            ExportExtendedBuildingBlock(partBuildingBlock, outputDirectory,
                string.Format(caseFileNameTemplate, partName), BuildingBlocks.Blocks[intermediatePart].Color);
        }

        private void ExportGuidePlasticEntity(GuidePreferenceDataModel guidePreferenceData, string outputDirectory, string caseFileNameTemplate, IBB intermediatePart, string partName)
        {
            var implantComponent = new GuideCaseComponent();
            var partBuildingBlock = implantComponent.GetGuideBuildingBlock(intermediatePart, guidePreferenceData);
            ExportExtendedBuildingBlock(partBuildingBlock, outputDirectory,
                string.Format(caseFileNameTemplate, partName), BuildingBlocks.Blocks[intermediatePart].Color);
        }

        public void ExportImplantPlasticBuildingBlocks(string outputDirectory)
        {
            var fileNameTemplate = $"{_director.caseId}_{{0}}_I{{1}}_Implant_{{2}}_v{_director.version:D}_draft{_director.draft:D}";

            foreach (var casePreferenceData in _director.CasePrefManager.CasePreferences)
            {
                var caseFileNameTemplate = string.Format(fileNameTemplate, casePreferenceData.CasePrefData.ImplantTypeValue, casePreferenceData.NCase, "{0}");

                ExportImplantPlasticEntity(casePreferenceData, outputDirectory, caseFileNameTemplate, IBB.ActualImplantImprintSubtractEntity, "Imprint");
                ExportImplantPlasticEntity(casePreferenceData, outputDirectory, caseFileNameTemplate, IBB.ImplantScrewIndentationSubtractEntity, "Screw_Indentation");
            }
        }

        public void ExportGuidePlasticBuildingBlocks(string outputDirectory)
        {
            var fileNameTemplate = $"{_director.caseId}_{{0}}_G{{1}}_Guide_{{2}}_v{_director.version:D}_draft{_director.draft:D}";

            foreach (var guidePref in _director.CasePrefManager.GuidePreferences)
            {
                var guideTypeName = GeneralUtilities.CheckForTSGGuideTypeName(
                    _director, guidePref);
                var caseFileNameTemplate = string.Format(fileNameTemplate, guideTypeName, guidePref.NCase, "{0}");

                ExportGuidePlasticEntity(guidePref, outputDirectory, caseFileNameTemplate, IBB.ActualGuideImprintSubtractEntity, "Imprint");
                ExportGuidePlasticEntity(guidePref, outputDirectory, caseFileNameTemplate, IBB.GuideScrewIndentationSubtractEntity, "Screw_Indentation");
            }
        }
    }
}
