using IDS.CMF.CasePreferences;
using IDS.CMF.CustomMainObjects;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Utilities;
using IDS.CMF.Visualization;
using IDS.Core.Utilities;
using Rhino.Geometry;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;

namespace IDS.CMF.Quality
{
    public class CMFQCImplantExporter
    {
        private readonly CMFImplantDirector _director;
        private readonly CMFObjectManager _objectManager;
        private readonly ImplantCaseComponent _implantComponent;

        public CMFQCImplantExporter(CMFImplantDirector director)
        {
            this._director = director;
            _objectManager = new CMFObjectManager(director);
            _implantComponent = new ImplantCaseComponent();
        }
        
        public void ExportAdditionalImplantBuildingBlocks(string outputDirectory)
        {
            var fileNameTemplate = $"{_director.caseId}_{{0}}_I{{1}}_{{2}}_v{_director.version:D}_draft{_director.draft:D}";

            var screwComponentsAreValid = true;

            foreach (var casePreferenceData in _director.CasePrefManager.CasePreferences)
            {
                var caseFileNameTemplate = string.Format(fileNameTemplate, casePreferenceData.CasePrefData.ImplantTypeValue, casePreferenceData.NCase, "{0}");

                var color = CasePreferencesHelper.GetColor(casePreferenceData.NCase);

                ExportActualImplantIntermediateParts(casePreferenceData, outputDirectory, caseFileNameTemplate, color);

                var export = ExportImplantScrewComponents(casePreferenceData, outputDirectory, caseFileNameTemplate);
                if (!export)
                {
                    screwComponentsAreValid = false;
                }
            }

            if (!screwComponentsAreValid)
            {
                MessageBox.Show("Invalid screw entity during export", "ImplantScrewComponents", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public bool ExportImplantScrewComponents(string outputDirectory)
        {
            var res = true;
            var fileNameTemplate = $"{_director.caseId}_{{0}}_I{{1}}_{{2}}_v{_director.version:D}_draft{_director.draft:D}";
            foreach (var casePreferenceData in _director.CasePrefManager.CasePreferences)
            {
                var caseFileNameTemplate = string.Format(fileNameTemplate, casePreferenceData.CasePrefData.ImplantTypeValue, casePreferenceData.NCase, "{0}");
                res &= ExportImplantScrewComponents(casePreferenceData, outputDirectory, caseFileNameTemplate);
            }

            return res;
        }

        private bool ExportImplantScrewComponents(CasePreferenceDataModel casePreferenceData, string outputDirectory, string caseFileNameTemplate)
        {
            if (casePreferenceData.ScrewAideData.ScrewStamp == null)
            {
                return false;
            }

            var screwBuildingBlock = _implantComponent.GetImplantBuildingBlock(IBB.Screw, casePreferenceData);
            var screws = _objectManager.GetAllBuildingBlocks(screwBuildingBlock.Block).Select(screw => (Screw) screw);

            var screwStamp = new Mesh();

            foreach (var screw in screws)
            {
                screwStamp.Append(GetMeshScrewComponents(casePreferenceData.ScrewAideData.ScrewStamp, screw.AlignmentTransform));               
            }

            ExportStl(screwStamp, outputDirectory, string.Format(caseFileNameTemplate, "Stamp"), Colors.Screw);

            return ExportPlannedBarrelEntities(casePreferenceData, outputDirectory, caseFileNameTemplate);
        }

        public bool ExportPlannedBarrelEntities(CasePreferenceDataModel casePreferenceData, string outputDirectory, string caseFileNameTemplate)
        {
            if (casePreferenceData.BarrelAideData.ScrewBarrel == null ||
                    casePreferenceData.BarrelAideData.ScrewBarrelShape == null || casePreferenceData.BarrelAideData.ScrewBarrelSubtractor == null)
            {
                return false;
            }

            var calibrator = new PlannedPositionedBarrelCalibrator(_director);
            var screwAndLeveledScrewBarrels = calibrator.CalibrateScrewsBarrelOnPlannedPosition(casePreferenceData);
                
            var screwBarrel = new Mesh();
            var screwBarrelShape = new Mesh();
            var screwBarrelSubtractor = new Mesh();
            Mesh screwBarrelDidntMeetSpecs = null;

            var barrelCenterline = new Mesh();
            var barrelHelper = new BarrelHelper(_director);

            var implantSupportManager = new ImplantSupportManager(_objectManager);
            var implantSupport = implantSupportManager.GetImplantSupportMesh(casePreferenceData);
            implantSupportManager.ImplantSupportNullCheck(implantSupport, casePreferenceData);

            foreach (var screwAndLeveledScrewBarrel in screwAndLeveledScrewBarrels)
            {
                var barrel = screwAndLeveledScrewBarrel.Value;
                var screw = screwAndLeveledScrewBarrel.Key;
                var alignTransform = barrel.Transform;
                var barrelAideData = new BarrelAideDataModel(screw.ScrewType, screw.BarrelType);

                screwBarrel.Append(GetMeshScrewComponents(barrelAideData.ScrewBarrel, alignTransform));
                screwBarrelShape.Append(GetMeshScrewComponents(barrelAideData.ScrewBarrelShape, alignTransform));
                screwBarrelSubtractor.Append(GetMeshScrewComponents(barrelAideData.ScrewBarrelSubtractor, alignTransform));

                var centerlineCurve = barrelHelper.GetBarrelCenterline(casePreferenceData, alignTransform, implantSupport);
                barrelCenterline.Append(barrelHelper.ConvertCurveToMesh(centerlineCurve));

                if (barrel.Color == Colors.BarrelLevelingNotMeetingSpecs)
                {
                    if (screwBarrelDidntMeetSpecs == null)
                    {
                        screwBarrelDidntMeetSpecs = new Mesh();
                    }

                    screwBarrelDidntMeetSpecs.Append(GetMeshScrewComponents(barrelAideData.ScrewBarrel, alignTransform));
                }
            }

            ExportStl(screwBarrel, outputDirectory, string.Format(caseFileNameTemplate, "PlannedBarrels"), Colors.GeneralGrey);
            ExportStl(screwBarrelShape, outputDirectory, string.Format(caseFileNameTemplate, "PlannedBarrelsShape"), Colors.Screw);
            ExportStl(screwBarrelSubtractor, outputDirectory, string.Format(caseFileNameTemplate, "PlannedBarrelSubtractor"), Colors.Screw);
            ExportStl(barrelCenterline, outputDirectory, string.Format(caseFileNameTemplate, "PlannedBarrelCenterline"), Colors.GeneralGrey);

            if (screwBarrelDidntMeetSpecs != null)
            {
                ExportStl(screwBarrelDidntMeetSpecs, outputDirectory, string.Format(caseFileNameTemplate, "PlannedBarrels_red"), Colors.BarrelLevelingNotMeetingSpecs);
            }

            return true;
        }

        private Mesh GetMeshScrewComponents(Brep screwAide, Transform alignmentTransform)
        {
            var screwComponent = new Brep();
            screwComponent.Append(screwAide);
            screwComponent.Transform(alignmentTransform);
            return MeshUtilities.ConvertBrepToMesh(screwComponent, true);
        }

        private void ExportStl(Mesh mesh, string outputDirectory, string fileName, Color color)
        {
            if (mesh.Faces.Count == 0)
            {
                return;
            }

            var filePath = Path.Combine(outputDirectory, $"{fileName}.stl");
            var stlColor = new int[] {color.R, color.G, color.B};
            StlUtilities.RhinoMesh2StlBinary(mesh, filePath, stlColor);
        }

        private void ExportActualImplantIntermediateParts(CasePreferenceDataModel casePreferenceData, string outputDirectory, string caseFileNameTemplate, Color color)
        {            
            ExportActualImplantIntermediatePart(casePreferenceData, outputDirectory, caseFileNameTemplate, color, IBB.ActualImplantWithoutStampSubtraction, "WithoutSubtraction");
            ExportActualImplantIntermediatePart(casePreferenceData, outputDirectory, caseFileNameTemplate, color, IBB.ActualImplantSurfaces, "PlateAndPastille");
        }

        private void ExportActualImplantIntermediatePart(CasePreferenceDataModel casePreferenceData, string outputDirectory, string caseFileNameTemplate, Color color, IBB intermediatePart, string partName)
        {
            var partBuildingBlock = _implantComponent.GetImplantBuildingBlock(intermediatePart, casePreferenceData);
            var partObj = _objectManager.GetBuildingBlock(partBuildingBlock);
            if (partObj != null)
            {
                var mesh = (Mesh)partObj.DuplicateGeometry();
                ExportStl(mesh, outputDirectory, string.Format(caseFileNameTemplate, partName), color);
                mesh.Dispose();
            }
        }
    }
}
