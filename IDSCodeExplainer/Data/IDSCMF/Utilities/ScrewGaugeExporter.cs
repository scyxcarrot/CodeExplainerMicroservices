using IDS.CMF.CasePreferences;
using IDS.CMF.FileSystem;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.Core.Utilities;
using Rhino.Geometry;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IDS.CMF.Utilities
{
    public class ScrewGaugeExporter
    {
        public bool ExportImplantScrewGauges(CasePreferenceDataModel casePrefData, CMFObjectManager objectManager, string exportDirectory, string prefix, string postfix)
        {
            var implantComponent = new ImplantCaseComponent();
            var screwsBuildingBlock = implantComponent.GetImplantBuildingBlock(IBB.Screw, casePrefData);
            var screws = objectManager.GetAllBuildingBlocks(screwsBuildingBlock).Select(screw => screw as Screw).ToList();
            return ExportScrewGauges(screws, casePrefData.CasePrefData.ScrewTypeValue, exportDirectory, prefix, postfix);
        }

        public bool ExportGuideScrewGauges(GuidePreferenceDataModel guideCasePrefData, CMFObjectManager objectManager, string exportDirectory, string prefix, string postfix)
        {
            var implantComponent = new GuideCaseComponent();
            var screwsBuildingBlock = implantComponent.GetGuideBuildingBlock(IBB.GuideFixationScrew, guideCasePrefData);
            var screws = objectManager.GetAllBuildingBlocks(screwsBuildingBlock).Select(screw => screw as Screw).ToList();
            return ExportScrewGauges(screws, guideCasePrefData.GuidePrefData.GuideScrewTypeValue, exportDirectory, prefix, postfix);
        }

        public bool ExportScrewGauges(List<Screw> screws, string screwType, string exportDirectory, string prefix, string postfix)
        {
            if (!screws.Any())
            {
                return true;
            }

            var resources = new ScrewResources();
            var gaugesFilePath = resources.GetGaugesFilePath(screwType);
            var orderedGaugesFilePath = ScrewGaugeUtilities.OrderGaugePathsByScrewLength(gaugesFilePath);

            for (var i = 0; i < orderedGaugesFilePath.Count; i++)
            {
                var gaugeFilePath = orderedGaugesFilePath[i];
                if (!File.Exists(gaugeFilePath))
                {
                    return false;
                }

                Mesh screwGaugeComponent;
                if (!StlUtilities.StlBinary2RhinoMesh(gaugeFilePath, out screwGaugeComponent))
                {
                    return false;
                }

                var screwGaugeMeshColor = ScrewGaugeUtilities.GetGaugeColorArray(i + 1);

                var screwGaugeMesh = new Mesh();

                screws.ForEach(screw =>
                {
                    var screwGauge = screwGaugeComponent.DuplicateMesh();
                    screwGauge.Transform(screw.AlignmentTransform);
                    screwGaugeMesh.Append(screwGauge);
                });

                var gaugeName = Path.GetFileNameWithoutExtension(gaugeFilePath);
                var screwsGaugePath = Path.Combine(exportDirectory, $"{prefix}_Screws_{gaugeName}{postfix}.stl");
                StlUtilities.RhinoMesh2StlBinary(screwGaugeMesh, screwsGaugePath, screwGaugeMeshColor);
            }
            return true;
        }
    }
}
