using IDS.Core.Enumerators;
using IDS.Core.ImplantDirector;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Core.Visualization;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using IDS.Glenius.Query;
using IDS.Glenius.Visualization;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace IDS.Glenius.Quality
{
    public class QualityReportExporter : Core.Quality.QualityReportExporter
    {
        public QualityReportExporter(DocumentType documentType)
        {
            ReportDocumentType = documentType;
        }

        protected override bool FillReport(IImplantDirector idirector, string filename,
            out Dictionary<string, string> reportValues)
        {
            // Init
            reportValues = new Dictionary<string, string>();

            var director = idirector as GleniusImplantDirector;
            if (director == null)
            {
                return false;
            }

            SetVisibility(ref reportValues, director.CurrentDesignPhase);

            var reportType = ReportDocumentType != DocumentType.ApprovedQC ? director.CurrentDesignPhase.ToString() : "QC Approved Export";
            // Traceability
            ////////////////
            AddTraceability(ref reportValues, director, reportType);

            // Header
            ////////////////
            AddHeaderInformation(ref reportValues, director.caseId, reportType, director.defectIsLeft);

            // Design Overview
            ////////////////
            AddDesignOverviewImages(ref reportValues, director, Width, Height);

            //Conflicting Entities
            ///////////////
            AddConflictingEntitiesTable(ref reportValues, director);
            AddConflictingEntitiesDiagrams(ref reportValues, director);

            // Reconstruction
            ////////////////
            AddReconstructionInformation(ref reportValues, director);
            AddReconstructionImages(ref reportValues, director, Width, Height);

            // Head
            ////////////////
            AddHeadInformation(ref reportValues, director);
            AddHeadImages(ref reportValues, director, Width, Height);

            //Screws
            ////////////////
            AddScrewTableInfo(ref reportValues, director);
            AddScrewDiagrams(ref reportValues, director);

            if (ReportDocumentType == DocumentType.ScrewQC)
            {
                return true;
            }

            // Design Scapula
            ////////////////
            AddDesignScapulaInformation(ref reportValues, director);
            AddDesignScapulaImages(ref reportValues, director, Width, Height);

            //Scaffold
            ///////////////
            AddScaffoldDiagrams(ref reportValues, director);
            AddScaffoldContactDiagrams(ref reportValues, director);
            AddScaffoldReamingDiagrams(ref reportValues, director);
            AddScaffoldTableValues(ref reportValues, director);

            //Plate
            ////////////////
            AddPlateDiagrams(ref reportValues, director);

            //Notching
            ///////////////
            AddNotchingDiagrams(ref reportValues, director);

            return true;
        }

        private static void AddHeaderInformation(ref Dictionary<string, string> valueDictionary, string caseId,
            string reportType, bool defectIsLeft)
        {
            valueDictionary.Add("CASE_ID", caseId);
            valueDictionary.Add("PHASE", reportType);
            valueDictionary.Add("DEFECT_SIDE", defectIsLeft ? "Left" : "Right");
        }

        private static void AddDesignOverviewImages(ref Dictionary<string, string> valueDictionary, GleniusImplantDirector director, int width, int height)
        {
            var screenshots = new ScreenshotsDesignOverview(director);
            valueDictionary.Add("IMG_PREOP_ANT", screenshots.GeneratePreOpOverviewImageString(width, height, CameraView.Anterior));
            valueDictionary.Add("IMG_PREOP_ANTLAT", screenshots.GeneratePreOpOverviewImageString(width, height, CameraView.Anterolateral));
            valueDictionary.Add("IMG_PREOP_LAT", screenshots.GeneratePreOpOverviewImageString(width, height, CameraView.Lateral));
            valueDictionary.Add("IMG_PREOP_POSLAT", screenshots.GeneratePreOpOverviewImageString(width, height, CameraView.Posterolateral));
            valueDictionary.Add("IMG_PREOP_POS", screenshots.GeneratePreOpOverviewImageString(width, height, CameraView.Posterior));

            valueDictionary.Add("IMG_DESIGN_ANT", screenshots.GenerateDesignOverviewImageString(width, height, CameraView.Anterior));
            valueDictionary.Add("IMG_DESIGN_ANTLAT", screenshots.GenerateDesignOverviewImageString(width, height, CameraView.Anterolateral));
            valueDictionary.Add("IMG_DESIGN_LAT", screenshots.GenerateDesignOverviewImageString(width, height, CameraView.Lateral));
            valueDictionary.Add("IMG_DESIGN_POSLAT", screenshots.GenerateDesignOverviewImageString(width, height, CameraView.Posterolateral));
            valueDictionary.Add("IMG_DESIGN_POS", screenshots.GenerateDesignOverviewImageString(width, height, CameraView.Posterior));
        }

        private static void AddReconstructionInformation(ref Dictionary<string, string> valueDictionary, GleniusImplantDirector director)
        {
            valueDictionary.Add("RECONSTRUCTION_VERSION", DoubleToString(director.AnatomyMeasurements.GlenoidVersionValue, "F1"));
            valueDictionary.Add("RECONSTRUCTION_INCLINATION", DoubleToString(director.AnatomyMeasurements.GlenoidInclinationValue, "F1"));
        }

        private static void AddReconstructionImages(ref Dictionary<string, string> valueDictionary, GleniusImplantDirector director, int width, int height)
        {
            var screenshots = new ScreenshotsReconstruction(director);
            screenshots.SetupVisualization();
            valueDictionary.Add("IMG_RECONSTRUCTION_SUP", screenshots.GenerateReconstructionImageString(width, height, CameraView.Superior));
            valueDictionary.Add("IMG_RECONSTRUCTION_ANT", screenshots.GenerateReconstructionImageString(width, height, CameraView.Anterior));
            valueDictionary.Add("IMG_RECONSTRUCTION_ANTLAT", screenshots.GenerateReconstructionImageString(width, height, CameraView.Anterolateral));
            valueDictionary.Add("IMG_RECONSTRUCTION_LAT", screenshots.GenerateReconstructionImageString(width, height, CameraView.Lateral));
            valueDictionary.Add("IMG_RECONSTRUCTION_POSLAT", screenshots.GenerateReconstructionImageString(width, height, CameraView.Posterolateral));
            valueDictionary.Add("IMG_RECONSTRUCTION_POS", screenshots.GenerateReconstructionImageString(width, height, CameraView.Posterior));
            valueDictionary.Add("IMG_RECONSTRUCTION_MED", screenshots.GenerateReconstructionImageString(width, height, CameraView.Medial));
            valueDictionary.Add("IMG_RECONSTRUCTION_INF", screenshots.GenerateReconstructionImageString(width, height, CameraView.Inferior));

            valueDictionary.Add("IMG_DEFECT_SUP", screenshots.GenerateDefectImageString(width, height, CameraView.Superior));
            valueDictionary.Add("IMG_DEFECT_ANT", screenshots.GenerateDefectImageString(width, height, CameraView.Anterior));
            valueDictionary.Add("IMG_DEFECT_ANTLAT", screenshots.GenerateDefectImageString(width, height, CameraView.Anterolateral));
            valueDictionary.Add("IMG_DEFECT_LAT", screenshots.GenerateDefectImageString(width, height, CameraView.Lateral));
            valueDictionary.Add("IMG_DEFECT_POSLAT", screenshots.GenerateDefectImageString(width, height, CameraView.Posterolateral));
            valueDictionary.Add("IMG_DEFECT_POS", screenshots.GenerateDefectImageString(width, height, CameraView.Posterior));
            valueDictionary.Add("IMG_DEFECT_MED", screenshots.GenerateDefectImageString(width, height, CameraView.Medial));
            valueDictionary.Add("IMG_DEFECT_INF", screenshots.GenerateDefectImageString(width, height, CameraView.Inferior));
            screenshots.ResetVisualization();
        }

        private static void AddHeadInformation(ref Dictionary<string, string> valueDictionary,
            GleniusImplantDirector director)
        {
            var objectManager = new GleniusObjectManager(director);
            var headAlignment = new HeadAlignment(director.AnatomyMeasurements, objectManager, director.Document,
                director.defectIsLeft);
            AddHeadOverviewToDictionary(valueDictionary, objectManager);
            AddHeadOrientationToDictionary(valueDictionary, headAlignment);
            AddHeadPositionToDictionary(valueDictionary, headAlignment);
            AddHeadPositionPreopToDictionary(valueDictionary, director);
            AddHeadReamingToDictionary(valueDictionary, director);
        }

        private static void AddHeadOverviewToDictionary(Dictionary<string, string> valueDictionary,
            GleniusObjectManager objectManager)
        {
            var headTypes = new Dictionary<HeadType, string>();
            headTypes.Add(HeadType.TYPE_36_MM, "36");
            headTypes.Add(HeadType.TYPE_38_MM, "38");
            headTypes.Add(HeadType.TYPE_42_MM, "42");
            var head = objectManager.GetBuildingBlock(IBB.Head) as Head;
            valueDictionary.Add("HEAD_TYPE", headTypes[head.HeadType]);

            var headAnalysisHelper = new HeadAnalysisHelper(objectManager);
            headAnalysisHelper.PerformVicinityCheck();
            AddVicinityValue(valueDictionary, "BONE-HEAD_VICINITY", headAnalysisHelper.IsBoneHeadVicinityOK);
            AddVicinityValue(valueDictionary, "BONE-TAPER_VICINITY", headAnalysisHelper.IsBoneTaperVicinityOK);
        }

        private static void AddVicinityValue(Dictionary<string, string> valueDictionary, string key, bool isOK)
        {
            if (isOK)
            {
                valueDictionary.Add(key, "<td>OK</td>");
            }
            else
            {
                valueDictionary.Add(key, "<td style=\"background-color: rgb(255,0,0);\">NOT OK</td>");
            }
        }

        private static void AddHeadOrientationToDictionary(Dictionary<string, string> valueDictionary,
            HeadAlignment headAlignment)
        {
            valueDictionary.Add("HEAD_VERSION", DoubleToString(headAlignment.GetVersionAngle(), "F1"));
            valueDictionary.Add("HEAD_INCLINATION", DoubleToString(headAlignment.GetInclinationAngle(), "F1"));
        }

        private static void AddHeadPositionToDictionary(Dictionary<string, string> valueDictionary,
            HeadAlignment headAlignment)
        {
            valueDictionary.Add("HEAD_INF_SUP", DoubleToString(headAlignment.GetInferiorSuperiorPosition(), "F1"));
            valueDictionary.Add("HEAD_LAT_MED", DoubleToString(headAlignment.GetMedialLateralPosition(), "F1"));
            valueDictionary.Add("HEAD_POS_ANT", DoubleToString(headAlignment.GetAnteriorPosteriorPosition(), "F1"));
        }

        private static void AddHeadPositionPreopToDictionary(Dictionary<string, string> valueDictionary,
            GleniusImplantDirector director)
        {
            var objectManager = new GleniusObjectManager(director);
            if (director.PreopCor != null && objectManager.HasBuildingBlock(IBB.Head))
            {
                var head = objectManager.GetBuildingBlock(IBB.Head) as Head;
                var headPreopMeasurements = new HeadPreopMeasurements(director.AnatomyMeasurements, head.CoordinateSystem.Origin, director.PreopCor.CenterPoint);
                valueDictionary.Add("HEAD_INF_SUP_PREOP", $"{DoubleToString(headPreopMeasurements.GetInferiorSuperiorPosition(), "F1")} mm");
                valueDictionary.Add("HEAD_LAT_MED_PREOP", $"{DoubleToString(headPreopMeasurements.GetMedialLateralPosition(), "F1")} mm");
                valueDictionary.Add("HEAD_POS_ANT_PREOP", $"{DoubleToString(headPreopMeasurements.GetAnteriorPosteriorPosition(), "F1")} mm");
            }
            else
            {
                valueDictionary.Add("HEAD_INF_SUP_PREOP", "/");
                valueDictionary.Add("HEAD_LAT_MED_PREOP", "/");
                valueDictionary.Add("HEAD_POS_ANT_PREOP", "/");
            }
        }

        private static void AddHeadReamingToDictionary(Dictionary<string, string> valueDictionary,
            GleniusImplantDirector director)
        {
            valueDictionary.Add("HEAD_RBV",
                Volume.BuildingBlockVolume(director, BuildingBlocks.Blocks[IBB.RBVHead], true).ToString("F1", CultureInfo.InvariantCulture));
        }

        private static string DoubleToString(double number, string format)
        {
            return number.ToString(format, CultureInfo.InvariantCulture);
        }

        private static void AddHeadImages(ref Dictionary<string, string> valueDictionary,
            GleniusImplantDirector director, int width, int height)
        {
            var screenshots = new ScreenshotsHead(director);
            valueDictionary.Add("IMG_HEAD_SUP",
                screenshots.GenerateHeadImageString(width, height, HeadImageType.Superior));
            valueDictionary.Add("IMG_HEAD_ANT",
                screenshots.GenerateHeadImageString(width, height, HeadImageType.Anterior));
            valueDictionary.Add("IMG_HEAD_LAT",
                screenshots.GenerateHeadImageString(width, height, HeadImageType.Lateral));
            valueDictionary.Add("IMG_HEAD_RBV",
                screenshots.GenerateHeadImageString(width, height, HeadImageType.LateralRBV));
            valueDictionary.Add("IMG_HEAD_REAM",
                screenshots.GenerateHeadImageString(width, height, HeadImageType.LateralReamed));
        }

        private static void AddDesignScapulaInformation(ref Dictionary<string, string> valueDictionary, GleniusImplantDirector director)
        {
            var objectManager = new GleniusObjectManager(director);
            var scapulaDesign = objectManager.GetBuildingBlock(IBB.ScapulaDesign).Geometry as Mesh;
            var scapulaOriginal = objectManager.GetBuildingBlock(IBB.Scapula).Geometry as Mesh;
            var scapulaDesignUsed = false;
            // true if number of vertices differs;
            scapulaDesignUsed |= (scapulaOriginal.Vertices.Count != scapulaDesign.Vertices.Count); 
            if (!scapulaDesignUsed) // if same number of vertices
            {
                var distances = new List<double>();
                MeshAnalysis.MeshToMeshAnalysis(scapulaDesign, scapulaOriginal, out distances);
                scapulaDesignUsed |= (distances.Max() > 0.01);
            }

            var volDiff = VolumeMassProperties.Compute(scapulaDesign).Volume - VolumeMassProperties.Compute(scapulaOriginal).Volume;
            volDiff = Math.Round(volDiff / 1000, 1);

            valueDictionary.Add("DESIGN_SCAPULA_VOL_DIFF", volDiff.ToString("F0"));
            valueDictionary.Add("DESIGN_SCAPULA_DIFF", scapulaDesignUsed ? "YES" : "NO");
        }

        private static void AddDesignScapulaImages(ref Dictionary<string, string> valueDictionary, GleniusImplantDirector director, int width, int height)
        {
            var screenshots = new ScreenshotsDesignScapula(director);
            screenshots.GenerateMeshDifference();
            valueDictionary.Add("IMG_DESIGN_SCAPULA_ANTLAT", screenshots.GenerateDesignScapulaImageString(width, height, CameraView.Anterolateral));
            valueDictionary.Add("IMG_DESIGN_SCAPULA_LAT", screenshots.GenerateDesignScapulaImageString(width, height, CameraView.Lateral));
            valueDictionary.Add("IMG_DESIGN_SCAPULA_POSLAT", screenshots.GenerateDesignScapulaImageString(width, height, CameraView.Posterolateral));
        }

        private static void AddTraceability(ref Dictionary<string, string> valueDictionary,
            GleniusImplantDirector director, string reportType)
        {
            valueDictionary.Add("IDS_BUILD", IDSPluginHelper.PluginVersion);
            valueDictionary.Add("VERSION", director.version.ToString("D"));
            valueDictionary.Add("DRAFT", director.draft.ToString("D"));

            var fileInfo = new FileInfo(director.Document.Path);
            valueDictionary.Add("WORK_FILE", fileInfo.Name);

            var inputFiles = new List<string>();
            foreach (var inputFile in director.InputFiles)
            {
                fileInfo = new FileInfo(inputFile);
                inputFiles.Add($"<div>{fileInfo.Name}</div>");
            }
            valueDictionary.Add("INPUT_FILE", string.Concat(inputFiles));

            valueDictionary.Add("REPORT", reportType);
            valueDictionary.Add("TIMESTAMP", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
        }

        private static void AddScrewTableInfo(ref Dictionary<string, string> valueDictionary,
            GleniusImplantDirector director)
        {
            var provider = new QCDocumentScrewInfoProvider(director);

            var diameters = provider.GetDiameter();
            var screwIndexes = diameters.Select(x => x.Key).ToList();
            var types = provider.GetScrewLockingType();
            var lengths = provider.GetLengths();
            var inBone = provider.GetDistanceInBone();
            var untilBone = provider.GetDistanceUntilBone();
            var bicorticality = provider.GetIsBicortical();
            var offsets = provider.GetScrewOffset();
            var mantles = provider.GetScrewMantleElongationLengths();

            var tableContent = screwIndexes.Aggregate("", (current, screwIndex) =>
                current + GenerateTableInfoRow(screwIndex, diameters[screwIndex], types[screwIndex],
                    lengths[screwIndex],
                    inBone[screwIndex], untilBone[screwIndex],
                    bicorticality[screwIndex], offsets[screwIndex], mantles[screwIndex]));

            valueDictionary.Add("SCREW_TABLE_INFO", tableContent);
        }

        private static string GenerateTableInfoRow(int index, double diameter, string type, double length,
            double inBone, double untilBone, bool isBicortical, double offset, double mantleEnlongement)
        {
            var diameterString = String.Format(CultureInfo.InvariantCulture, "{0:0.0}", diameter);

            return String.Format(
                "<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td><td>{5}</td><td>{6}</td><td>{7}</td><td>{8}</td></tr>",
                index, diameterString, type, DecimaledDouble(length), DecimaledDouble(inBone),
                DecimaledDouble(untilBone), isBicortical ? "BI" : "UNI", DecimaledDouble(offset),
                DecimaledDouble(mantleEnlongement));
        }

        private static string DecimaledDouble(double value)
        {
            return String.Format(CultureInfo.InvariantCulture, "{0:0.0000}", value);
        }

        private static void AddScrewDiagrams(ref Dictionary<string, string> valueDictionary,
            GleniusImplantDirector director)
        {
            var objectManager = new GleniusObjectManager(director);
            var screwGeometries = objectManager.GetAllBuildingBlocks(IBB.Screw).Select(x => x as Screw).ToList();
            var appended = new Brep();
            screwGeometries.ForEach(x => appended.Append(x.BrepGeometry));
            var bboxScrew = appended.GetBoundingBox(false);

            //Screw Index
            var bboxForScrewsIndex = CreateScaledBoundingBox(bboxScrew, 0.8);

            var visualizer = new QCDocumentScrewVisualizerPresets(director);
            visualizer.ScrewsShowIndex();
            var screwIndexDiagram =
                Screenshots.GenerateImage(director.Document, Height / 2, Width / 2, bboxForScrewsIndex, true);
            visualizer.ScrewsShowIndexVisibility(false);
            valueDictionary.Add("SCREW_INDEX_LATERAL", Screenshots.GenerateImageString(screwIndexDiagram));

            //Screw Views
            var bboxForScrews = CreateScaledBoundingBox(bboxScrew, 1.2);

            visualizer.ScrewsAnteroLateral();
            var screwAnteroLateralDiagram =
                Screenshots.GenerateImage(director.Document, Height, Width, bboxForScrews, true);
            valueDictionary.Add("SCREW_ANTEROLATERAL", Screenshots.GenerateImageString(screwAnteroLateralDiagram));

            visualizer.ScrewsPosterior();
            var screwPosteriorDiagram =
                Screenshots.GenerateImage(director.Document, Height, Width, bboxForScrews, true);
            valueDictionary.Add("SCREW_POSTERIOR", Screenshots.GenerateImageString(screwPosteriorDiagram));

            visualizer.ScrewsSuperior();
            var screwSuperiorDiagram = Screenshots.GenerateImage(director.Document, Height, Width, bboxForScrews, true);
            valueDictionary.Add("SCREW_SUPERIOR", Screenshots.GenerateImageString(screwSuperiorDiagram));

            //set bounding box for M4 Connection Screw
            var m4ConnScrewSafety = objectManager.GetBuildingBlock(IBB.M4ConnectionSafetyZone).Geometry as Brep;
            appended.Append(m4ConnScrewSafety);
            var bboxM4 = appended.GetBoundingBox(false);

            var bboxForM4ConnectionScrew = CreateScaledBoundingBox(bboxM4, 1.5);

            //M4 Connection Screw
            visualizer.M4ConnectionScrewAnterior();
            var m4ScrewAnteriorDiagram =
                Screenshots.GenerateImage(director.Document, Height, Width, bboxForM4ConnectionScrew, true);
            valueDictionary.Add("M4_CONNECTION_ANTERIOR", Screenshots.GenerateImageString(m4ScrewAnteriorDiagram));

            visualizer.M4ConnectionScrewNormalToCylinderHat();
            var m4ScrewLateralDiagram = Screenshots.GenerateImage(director.Document, Height, Width,
                CreateScaledBoundingBox(bboxScrew, 1), true);
            valueDictionary.Add("M4_CONNECTION_LATERAL", Screenshots.GenerateImageString(m4ScrewLateralDiagram));

            visualizer.M4ConnectionScrewPosterior();
            var m4ScrewPosteriorDiagram =
                Screenshots.GenerateImage(director.Document, Height, Width, bboxForM4ConnectionScrew, true);
            valueDictionary.Add("M4_CONNECTION_POSTERIOR", Screenshots.GenerateImageString(m4ScrewPosteriorDiagram));

            visualizer.M4ConnectionScrewScrewMantleVisibility(false);

            //M4 Conneciton Screw with Drill
            var screwDrill = objectManager.GetAllBuildingBlocks(IBB.ScrewDrillGuideCylinder)
                .Select(x => x.Geometry as Brep).ToList();
            var appended2 = new Brep();
            screwDrill.ForEach(x => appended2.Append(x.DuplicateBrep()));
            var bboxDrill = appended2.GetBoundingBox(false);

            var bboxForWithDrill = CreateScaledBoundingBox(bboxDrill, 2.8);

            visualizer.M4ConnectionScrewWithDrillGuideAnterior();
            var m4ScrewDrillAnteriorDiagram =
                Screenshots.GenerateImage(director.Document, Height, Width, bboxForWithDrill, true);
            valueDictionary.Add("M4_CONNECTION_DRILL_ANTERIOR",
                Screenshots.GenerateImageString(m4ScrewDrillAnteriorDiagram));

            visualizer.M4ConnectionScrewWithDrillGuidePosterior();
            var m4ScrewDrillPosteriorDiagram =
                Screenshots.GenerateImage(director.Document, Height, Width, bboxForWithDrill, true);
            valueDictionary.Add("M4_CONNECTION_DRILL_POSTERIOR",
                Screenshots.GenerateImageString(m4ScrewDrillPosteriorDiagram));
        }

        private static BoundingBox CreateScaledBoundingBox(BoundingBox bbox, double scale)
        {
            var scaleTransform = Transform.Scale(bbox.Center, scale);
            var bbox2 = bbox;
            bbox2.Transform(scaleTransform);
            return bbox2;
        }

        private static BoundingBox CreateBasePlateBoundingBox(GleniusImplantDirector director)
        {
            var visualizer = new QCDocumentNotchingVisualizerPresets(director);
            var objectManager = new GleniusObjectManager(director);
            var basePlate = objectManager.GetBuildingBlock(IBB.PlateBasePlate).Geometry as Mesh;
            var bbox = basePlate.GetBoundingBox(true);
            return CreateScaledBoundingBox(bbox, 1.4);
        }

        private static void AddPlateDiagrams(ref Dictionary<string, string> valueDictionary,
            GleniusImplantDirector director)
        {
            var visualizer = new QcDocumentPlateVisualizerPresets(director);
            var scaledBbox = CreateBasePlateBoundingBox(director);

            visualizer.SetVisualizationForPlateAnteroLateralView();
            var anteroLateralDiagram = Screenshots.GenerateImage(director.Document, Height, Width, scaledBbox, true);
            valueDictionary.Add("PLATE_ANTERIOR_LATERAL", Screenshots.GenerateImageString(anteroLateralDiagram));

            visualizer.SetVisualizationForPlateLateralView();
            var lateralDiagram = Screenshots.GenerateImage(director.Document, Height, Width, scaledBbox, true);
            valueDictionary.Add("PLATE_LATERAL", Screenshots.GenerateImageString(lateralDiagram));

            visualizer.SetVisualizationForPlatePosteroLateralView();
            var posteroLateralDiagram = Screenshots.GenerateImage(director.Document, Height, Width, scaledBbox, true);
            valueDictionary.Add("PLATE_POSTERIOR_LATERAL", Screenshots.GenerateImageString(posteroLateralDiagram));
        }

        private static void AddNotchingDiagrams(ref Dictionary<string, string> valueDictionary,
            GleniusImplantDirector director)
        {
            var visualizer = new QCDocumentNotchingVisualizerPresets(director);
            var scaledBbox = CreateBasePlateBoundingBox(director);

            visualizer.SetVisualizationForNotchingSuperiorView();
            var superiorView = Screenshots.GenerateImage(director.Document, Height, Width, scaledBbox, true);
            valueDictionary.Add("NOTCH_SUPERIOR", Screenshots.GenerateImageString(superiorView));

            visualizer.SetVisualizationForNotchingAnteriorView();
            var anteriorView = Screenshots.GenerateImage(director.Document, Height, Width, scaledBbox, true);
            valueDictionary.Add("NOTCH_ANTERIOR", Screenshots.GenerateImageString(anteriorView));

            visualizer.SetVisualizationForNotchingAnteroLateralView();
            var anteroLateralView = Screenshots.GenerateImage(director.Document, Height, Width, scaledBbox, true);
            valueDictionary.Add("NOTCH_ANTERIOR_LATERAL", Screenshots.GenerateImageString(anteroLateralView));

            visualizer.SetVisualizationForNotchingLateralView();
            var lateralView = Screenshots.GenerateImage(director.Document, Height, Width, scaledBbox, true);
            valueDictionary.Add("NOTCH_LATERAL", Screenshots.GenerateImageString(lateralView));

            visualizer.SetVisualizationForNotchingPosteroLateralView();
            var posteroLateralView = Screenshots.GenerateImage(director.Document, Height, Width, scaledBbox, true);
            valueDictionary.Add("NOTCH_POSTERIOR_LATERAL", Screenshots.GenerateImageString(posteroLateralView));

            visualizer.SetCameraToPosteriorView();
            var posteriorView = Screenshots.GenerateImage(director.Document, Height, Width, scaledBbox, true);
            valueDictionary.Add("NOTCH_POSTERIOR", Screenshots.GenerateImageString(posteriorView));

            visualizer.SetFullSphereVisible(false);
        }

        private static void AddConflictingEntitiesDiagrams(ref Dictionary<string, string> valueDictionary,
            GleniusImplantDirector director)
        {
            var visualizer = new QcDocumentConflictingEntitiesVisualizer(director);

            var objectManager = new GleniusObjectManager(director);
            var scapula = objectManager.GetBuildingBlock(IBB.Scapula).Geometry as Mesh;
            var conflictingEntities = objectManager.GetAllBuildingBlocks(IBB.ConflictingEntities).Select(x => x.Geometry as Mesh).ToList();
            var nonConflictingEntities = objectManager.GetAllBuildingBlocks(IBB.NonConflictingEntities).Select(x => x.Geometry as Mesh).ToList();

            var bbox = scapula.GetBoundingBox(true);
            conflictingEntities.ForEach(x => bbox.Union(x.GetBoundingBox(true)));
            nonConflictingEntities.ForEach(x => bbox.Union(x.GetBoundingBox(true)));
            var scaledBbox = CreateScaledBoundingBox(bbox, 1.4);

            visualizer.SetForAnteroLateralView();
            var anteroLateralView = Screenshots.GenerateImage(director.Document, Height, Width, scaledBbox, true);
            valueDictionary.Add("CONFLICT_ANTERIOR_LATERAL", Screenshots.GenerateImageString(anteroLateralView));

            visualizer.SetForPosteroLateralView();
            var posteroLateralView = Screenshots.GenerateImage(director.Document, Height, Width, scaledBbox, true);
            valueDictionary.Add("CONFLICT_POSTERO_LATERAL", Screenshots.GenerateImageString(posteroLateralView));

            visualizer.SetForAnteriorView();
            var anteriorView = Screenshots.GenerateImage(director.Document, Height, Width, scaledBbox, true);
            valueDictionary.Add("CONFLICT_ANTERIOR", Screenshots.GenerateImageString(anteriorView));

            visualizer.SetForLateralView();
            var lateralView = Screenshots.GenerateImage(director.Document, Height, Width, scaledBbox, true);
            valueDictionary.Add("CONFLICT_LATERAL", Screenshots.GenerateImageString(lateralView));

            visualizer.SetForPosteriorView();
            var posteriorView = Screenshots.GenerateImage(director.Document, Height, Width, scaledBbox, true);
            valueDictionary.Add("CONFLICT_POSTERIOR", Screenshots.GenerateImageString(posteriorView));
        }

        private static void AddConflictingEntitiesTable(ref Dictionary<string, string> valueDictionary,
            GleniusImplantDirector director)
        {
            var objectManager = new GleniusObjectManager(director);

            var conflictingMesh = objectManager.GetAllBuildingBlocks(IBB.ConflictingEntities).ToList();
            var nonConflictingMesh = objectManager.GetAllBuildingBlocks(IBB.NonConflictingEntities).ToList();

            var conflictingTotal = 0;
            var nonConflictingTotal = 0;

            conflictingMesh.ForEach(x => conflictingTotal += (x.Geometry as Mesh).DisjointMeshCount);
            nonConflictingMesh.ForEach(x => nonConflictingTotal += (x.Geometry as Mesh).DisjointMeshCount);

            valueDictionary.Add("CONFLICT_TOTAL", conflictingTotal.ToString());
            valueDictionary.Add("NONCONFLICT_TOTAL", nonConflictingTotal.ToString());
        }

        private static void AddScaffoldDiagrams(ref Dictionary<string, string> valueDictionary,
            GleniusImplantDirector director)
        {
            var visualizer = new QCDocumentScaffoldVisualizerPresets(director);
            var bbox = CreateBasePlateBoundingBox(director);

            visualizer.SetAnteroLateralForScaffoldView();
            var anteroLateralView = Screenshots.GenerateImage(director.Document, Height, Width, bbox, true);
            valueDictionary.Add("SCAFFOLD_ANTERIOR_LATERAL", Screenshots.GenerateImageString(anteroLateralView));

            visualizer.SetLateralForScaffoldView();
            var lateralView = Screenshots.GenerateImage(director.Document, Height, Width, bbox, true);
            valueDictionary.Add("SCAFFOLD_LATERAL", Screenshots.GenerateImageString(lateralView));

            visualizer.SetPosteroLateralForScaffoldView();
            var posteroLateralView = Screenshots.GenerateImage(director.Document, Height, Width, bbox, true);
            valueDictionary.Add("SCAFFOLD_POSTERIOR_LATERAL", Screenshots.GenerateImageString(posteroLateralView));
        }

        private static void AddScaffoldContactDiagrams(ref Dictionary<string, string> valueDictionary,
            GleniusImplantDirector director)
        {
            var visualizer = new QCDocumentScaffoldVisualizerPresets(director);
            var bbox = CreateBasePlateBoundingBox(director);

            visualizer.SetAnteroLateralForContactView();
            var anteroLateralView = Screenshots.GenerateImage(director.Document, Height, Width, bbox, true);
            valueDictionary.Add("SCAFFOLD_CONTACT_ANTERIOR_LATERAL", Screenshots.GenerateImageString(anteroLateralView));

            visualizer.SetLateralForContactView();
            var lateralView = Screenshots.GenerateImage(director.Document, Height, Width, bbox, true);
            valueDictionary.Add("SCAFFOLD_CONTACT_LATERAL", Screenshots.GenerateImageString(lateralView));

            visualizer.SetPosteroLateralForContactView();
            var posteroLateralView = Screenshots.GenerateImage(director.Document, Height, Width, bbox, true);
            valueDictionary.Add("SCAFFOLD_CONTACT_POSTERIOR_LATERAL", Screenshots.GenerateImageString(posteroLateralView));

            visualizer.SetConduitIsVisible(false);
        }

        private static void AddScaffoldReamingDiagrams(ref Dictionary<string, string> valueDictionary,
            GleniusImplantDirector director)
        {
            var objectManager = new GleniusObjectManager(director);
            var rbvHeads = objectManager.GetAllBuildingBlocks(IBB.RBVHead).Select(x => x.Geometry as Mesh).ToList();
            var rbvScaffold = objectManager.GetAllBuildingBlocks(IBB.RbvScaffold).Select(x => x.Geometry as Mesh).ToList();

            //default to boundingbox of BasePlate because RBVHead and RbvScaffold might not exist
            var bbox = objectManager.GetBuildingBlock(IBB.PlateBasePlate).Geometry.GetBoundingBox(true);
            rbvHeads.ForEach(x => bbox.Union(x.GetBoundingBox(true)));
            rbvScaffold.ForEach(x => bbox.Union(x.GetBoundingBox(true)));
            var scaledBbox = CreateScaledBoundingBox(bbox, 1.4);

            var visualizer = new QCDocumentScaffoldVisualizerPresets(director);

            visualizer.SetForHeadReamingView();
            var reamingHeadView = Screenshots.GenerateImage(director.Document, Height, Width, scaledBbox, true);
            valueDictionary.Add("SCAFFOLD_REAMING_OF_HEAD", Screenshots.GenerateImageString(reamingHeadView));

            visualizer.SetForImplantReamingView();
            var scaffoldReamingView = Screenshots.GenerateImage(director.Document, Height, Width, scaledBbox, true);
            valueDictionary.Add("SCAFFOLD_REAMING_OF_IMPLANT", Screenshots.GenerateImageString(scaffoldReamingView));

            visualizer.SetForReamedScapulaView();
            var scapulaReamedView = Screenshots.GenerateImage(director.Document, Height, Width, scaledBbox, true);
            valueDictionary.Add("SCAFFOLD_SCAPULA_REAMED_BONE", Screenshots.GenerateImageString(scapulaReamedView));

            visualizer.SetConduitIsVisible(false);
        }

        private static void AddScaffoldTableValues(ref Dictionary<string, string> valueDictionary,
            GleniusImplantDirector director)
        {
            var provider = new QCScaffoldInfoProvider(director);

            valueDictionary.Add("SCAFFOLD_VOLUME", provider.GetScaffoldVolumeInCC().ToString(CultureInfo.InvariantCulture));
            valueDictionary.Add("SCAFFOLD_HEAD_RBV_VOLUME", provider.GetHeadRBVVolumeInCC().ToString("F1", CultureInfo.InvariantCulture));
            valueDictionary.Add("SCAFFOLD_SCAFFOLD_RBV_VOLUME", provider.GetScaffoldRBVVolumeInCC().ToString(CultureInfo.InvariantCulture));
            valueDictionary.Add("SCAFFOLD_TOTAL_RBV_VOLUME", provider.GetTotalRBVVolumeInCC().ToString(CultureInfo.InvariantCulture));
        }

        private static void SetVisibility(ref Dictionary<string, string> ValueDict, DesignPhase currentDesignPhase)
        {
            ValueDict.Add("PREOP_DISPLAY", "block");
            ValueDict.Add("RECONSTRUCTION_DISPLAY", "block");
            ValueDict.Add("HEAD_DISPLAY", "block");
            ValueDict.Add("SCREW_DISPLAY", "block");
            ValueDict.Add("DESIGN_SCAPULA_DISPLAY", currentDesignPhase == DesignPhase.ScrewQC ? "none" : "block");
            ValueDict.Add("SCAFFOLD_DISPLAY", currentDesignPhase == DesignPhase.ScrewQC ? "none" : "block");
            ValueDict.Add("PLATE_DISPLAY", currentDesignPhase == DesignPhase.ScrewQC ? "none" : "block");
            ValueDict.Add("NOTCHING_DISPLAY", currentDesignPhase == DesignPhase.ScrewQC ? "none" : "block");
            ValueDict.Add("TRACEABILITY_DISPLAY", "block");
        }
    }
}