using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Query;
using IDS.CMF.Utilities;
using IDS.CMF.V2.Logics;
using IDS.CMF.Visualization;
using IDS.Core.Enumerators;
using IDS.Core.ImplantDirector;
using IDS.Core.PluginHelper;
using IDS.Core.Quality;
using IDS.Core.Utilities;
using Rhino;
using Rhino.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace IDS.CMF.Quality
{
    public class CMFQualityReportExporter : QualityReportExporter
    {
        private readonly bool _isPreview;
        private string _implantSectionHtml;
        private string _guideSectionHtml;
        public List<QCImplantImplantSection.ImageData> ImplantFrontImagesBase64JpegByteString { get; set; } = new List<QCImplantImplantSection.ImageData>();

        public QcDocBoneThicknessMapQuery BoneThicknessMapQuery { get; set; }

        public CMFQualityReportExporter(DocumentType documentType, bool isPreview = false)
        {
            ReportDocumentType = documentType;
            _isPreview = isPreview;
        }

        protected override bool FillReport(IImplantDirector director, string filename, out Dictionary<string, string> reportValues)
        {
#if (INTERNAL)
            var timerMain = new Stopwatch();
            timerMain.Start();
#endif
            var timerComponent = new Stopwatch();
            timerComponent.Start();
            var timeRecorded = new Dictionary<string, string>();

            var implantDirector = (CMFImplantDirector)director;
            reportValues = new Dictionary<string, string>();
            var doc = implantDirector.Document;

            var desc = doc.Views.ActiveView.ActiveViewport.DisplayMode;
            View.SetDisplayModeToIdsCmf(doc);

            // Remove existing thickness map
            ////////////////
            BoneThicknessAnalyzableObjectManager.HandleRemoveAllVertexColor(implantDirector);

            // Header
            ////////////////
            AddHeaderInformation(ref reportValues, implantDirector.caseId, ReportDocumentType.ToString());

            timerComponent.Stop();
            timeRecorded.Add($"AddHeaderInformation", $"{ (timerComponent.ElapsedMilliseconds * 0.001).ToString(CultureInfo.InvariantCulture) }");
            timerComponent.Restart();

            // Overview
            ////////////////
            AddOverviewImages(ref reportValues, implantDirector, doc, Width, Height, ReportDocumentType);

            timerComponent.Stop();
            timeRecorded.Add($"AddOverviewImages", $"{ (timerComponent.ElapsedMilliseconds * 0.001).ToString(CultureInfo.InvariantCulture) }");
            timerComponent.Restart();

            // Surgery Information
            ////////////////
            AddSurgeryInformation(ref reportValues, implantDirector);

            timerComponent.Stop();
            timeRecorded.Add($"AddSurgeryInformation", $"{ (timerComponent.ElapsedMilliseconds * 0.001).ToString(CultureInfo.InvariantCulture) }");
            timerComponent.Restart();

            // Anatomical Obstacles
            ////////////////
            AddAnatomicalObstacleInformation(ref reportValues, implantDirector);

            timerComponent.Stop();
            timeRecorded.Add($"AddAnatomicalObstacleInformation", $"{ (timerComponent.ElapsedMilliseconds * 0.001).ToString(CultureInfo.InvariantCulture) }");
            timerComponent.Restart();

            // Recut
            ////////////////
            AddRecutInformation(ref reportValues, implantDirector, ReportDocumentType);

            timerComponent.Stop();
            timeRecorded.Add($"AddRecutInformation", $"{ (timerComponent.ElapsedMilliseconds * 0.001).ToString(CultureInfo.InvariantCulture) }");
            timerComponent.Restart();

            // Implant Guide Relationship
            ////////////////
            AddImplantGuideRelationshipInformation(ref reportValues, implantDirector);

            timerComponent.Stop();
            timeRecorded.Add($"AddImplantGuideRelationshipInformation", $"{ (timerComponent.ElapsedMilliseconds * 0.001).ToString(CultureInfo.InvariantCulture) }");
            timerComponent.Restart();

            // Implant Information
            ////////////////
            AddImplantInformation(ref reportValues, implantDirector, ReportDocumentType);

            timerComponent.Stop();
            timeRecorded.Add($"AddImplantInformation", $"{ (timerComponent.ElapsedMilliseconds * 0.001).ToString(CultureInfo.InvariantCulture) }");
            timerComponent.Restart();

            // Guide Information
            ////////////////
            AddGuideInformation(ref reportValues, implantDirector, ReportDocumentType);

            timerComponent.Stop();
            timeRecorded.Add($"AddGuideInformation", $"{ (timerComponent.ElapsedMilliseconds * 0.001).ToString(CultureInfo.InvariantCulture) }");
            timerComponent.Restart();

            // Traceability
            ////////////////
            AddTraceability(ref reportValues, implantDirector);

            timerComponent.Stop();
            timeRecorded.Add($"AddTraceability", $"{ (timerComponent.ElapsedMilliseconds * 0.001).ToString(CultureInfo.InvariantCulture) }");

            Msai.TrackDevEvent("QCDoc FillReport (DONE)", "CMF", timeRecorded);
            Msai.PublishToAzure();

            doc.Views.ActiveView.ActiveViewport.DisplayMode = desc;

#if (INTERNAL)
            timerMain.Stop();
            IDSPluginHelper.WriteLine(LogCategory.Default, "QC Doc: " + $"{ (timerMain.ElapsedMilliseconds * 0.001).ToString(CultureInfo.InvariantCulture) } seconds");
#endif

            return true;
        }
        
        private void AddHeaderInformation(ref Dictionary<string, string> valueDictionary, string caseId, string currentDesignPhase)
        {
            valueDictionary.Add("CASE_ID", caseId);
            valueDictionary.Add("PHASE", currentDesignPhase);
            valueDictionary.Add("DOCTYPE", _isPreview ? "Preview" : "Report");
        }

        private void AddOverviewImages(ref Dictionary<string, string> valueDictionary, CMFImplantDirector director, RhinoDoc doc, int width, int height, DocumentType docType)
        {
            AddImplantOverviewImages(ref valueDictionary, director, doc, width, height, docType);

            AddGuideOverviewImages(ref valueDictionary, director, doc, width, height, docType);
        }

        private void AddSurgeryInformation(ref Dictionary<string, string> valueDictionary, CMFImplantDirector director)
        {
            var surgeryInfoData = new QCSurgeryInformation(director);
            surgeryInfoData.AssignQcSurgeryInformation(ref valueDictionary);
        }

        private void AddAnatomicalObstacleInformation(ref Dictionary<string, string> valueDictionary, CMFImplantDirector director)
        {
            var section = new QCAnatomicalObstacleSection(director);
            section.FillAnatomicalObstacleInformation(ref valueDictionary);
        }

        private void AddRecutInformation(ref Dictionary<string, string> valueDictionary, CMFImplantDirector director, DocumentType docType)
        {
            if (docType == DocumentType.PlanningQC)
            {
                valueDictionary.Add("CHANGES_DISPLAY", "none");
                valueDictionary.Add("CHANGES_RECUT_DISPLAY", "none");
            }
            else
            {
                var recutSection = new QCRecutSection(director);
                recutSection.FillRecutInformation(ref valueDictionary);

                valueDictionary.Add("CHANGES_DISPLAY", "block");
                valueDictionary.Add("CHANGES_RECUT_DISPLAY", "block");
            }
        }

        private void AddImplantGuideRelationshipInformation(ref Dictionary<string, string> valueDictionary, CMFImplantDirector director)
        {
            var implantGuideRelationshipSection = new QCImplantGuideRelationshipSection(director);
            implantGuideRelationshipSection.FillRelationshipInformation(ref valueDictionary);

            valueDictionary.Add("IMPLANTGUIDERELATIONSHIP_DISPLAY", "block");
        }

        private void AddImplantInformation(ref Dictionary<string, string> valueDictionary, CMFImplantDirector director, DocumentType docType)
        {
            ImplantFrontImagesBase64JpegByteString = new List<QCImplantImplantSection.ImageData>();
            var qcImplant = new QCImplantInformation(director);
            if (BoneThicknessMapQuery == null)
            {
                BoneThicknessMapQuery = new QcDocBoneThicknessMapQuery(director);
            }
            _implantSectionHtml = qcImplant.FillInImplantQC(ref valueDictionary, docType, BoneThicknessMapQuery);
            ImplantFrontImagesBase64JpegByteString.AddRange(qcImplant.ImplantFrontImagesBase64JpegByteString);
        }

        private void AddGuideInformation(ref Dictionary<string, string> valueDictionary, CMFImplantDirector director, DocumentType docType)
        {
            var qcGuide = new QCGuideInformation(director);
            _guideSectionHtml = qcGuide.FillInGuideQC(ref valueDictionary, docType);
        }

        private void AddImplantOverviewImages(ref Dictionary<string, string> valueDictionary, CMFImplantDirector director, RhinoDoc doc, int width, int height, DocumentType docType)
        {
            var query = new QCDocumentOverviewQuery(director);
            var hasImplantPreview = query.HasImplantPreview();

            if (docType == DocumentType.PlanningQC)
            {
                // Visibility Settings
                ////////////////
                valueDictionary.Add("OVERVIEW_IMPLANT_DISPLAY", "none");
            }
            else if (docType == DocumentType.MetalQC)
            {
                if (hasImplantPreview)
                {
                    // Implant with Implant Previews
                    ////////////////
                    var imagesString = ScreenshotsOverview.GenerateImplantPreviewOverviewImagesString(doc, width, height, new List<CameraView> { CameraView.Right, CameraView.Front, CameraView.Left });
                    valueDictionary.Add("IMG_OVERVIEW_IMPLANT_RIGHT", imagesString[0]);
                    valueDictionary.Add("IMG_OVERVIEW_IMPLANT_FRONT", imagesString[1]);
                    valueDictionary.Add("IMG_OVERVIEW_IMPLANT_LEFT", imagesString[2]);
                }

                // Visibility Settings
                ////////////////
                valueDictionary.Add("OVERVIEW_IMPLANT_DISPLAY", hasImplantPreview ? "block" : "none");
            }
            else if (docType == DocumentType.ApprovedQC)
            {
                var hasActualImplant = query.HasActualImplant();
                if (hasActualImplant)
                {
                    // Implant with Actual Implants
                    ////////////////
                    var imagesString = ScreenshotsOverview.GenerateActualImplantOverviewImagesString(doc, width, height, new List<CameraView> { CameraView.Right, CameraView.Front, CameraView.Left });
                    valueDictionary.Add("IMG_OVERVIEW_IMPLANT_RIGHT", imagesString[0]);
                    valueDictionary.Add("IMG_OVERVIEW_IMPLANT_FRONT", imagesString[1]);
                    valueDictionary.Add("IMG_OVERVIEW_IMPLANT_LEFT", imagesString[2]);
                }

                // Visibility Settings
                ////////////////
                valueDictionary.Add("OVERVIEW_IMPLANT_DISPLAY", hasActualImplant ? "block" : "none");
            }
        }

        private void AddGuideOverviewImages(ref Dictionary<string, string> valueDictionary, CMFImplantDirector director, RhinoDoc doc, int width, int height, DocumentType docType)
        {
            var query = new QCDocumentOverviewQuery(director);
            var hasGuidePreview = query.HasGuidePreviewSmoothen();

            if (docType == DocumentType.PlanningQC)
            {
                // Visibility Settings
                ////////////////
                valueDictionary.Add("OVERVIEW_GUIDE_DISPLAY", "none");
            }
            else if (docType == DocumentType.MetalQC)
            {
                if (hasGuidePreview)
                {
                    // Guide with Guide Previews
                    ////////////////
                    var imagesString = ScreenshotsOverview.GenerateGuidePreviewOverviewImagesString(doc, width, height, new List<CameraView> { CameraView.Right, CameraView.Front, CameraView.Left });
                    valueDictionary.Add("IMG_OVERVIEW_GUIDE_RIGHT", imagesString[0]);
                    valueDictionary.Add("IMG_OVERVIEW_GUIDE_FRONT", imagesString[1]);
                    valueDictionary.Add("IMG_OVERVIEW_GUIDE_LEFT", imagesString[2]);
                }

                // Visibility Settings
                ////////////////
                valueDictionary.Add("OVERVIEW_GUIDE_DISPLAY", hasGuidePreview ? "block" : "none");
            }
            else if (docType == DocumentType.ApprovedQC)
            {
                var hasActualGuide = query.HasActualGuide();
                if (hasActualGuide)
                {
                    // Guide with Actual Guides
                    ////////////////
                    var imagesString = ScreenshotsOverview.GenerateActualGuideOverviewImagesString(doc, width, height, new List<CameraView> { CameraView.Right, CameraView.Front, CameraView.Left });
                    valueDictionary.Add("IMG_OVERVIEW_GUIDE_RIGHT", imagesString[0]);
                    valueDictionary.Add("IMG_OVERVIEW_GUIDE_FRONT", imagesString[1]);
                    valueDictionary.Add("IMG_OVERVIEW_GUIDE_LEFT", imagesString[2]);
                }

                // Visibility Settings
                ////////////////
                valueDictionary.Add("OVERVIEW_GUIDE_DISPLAY", hasActualGuide ? "block" : "none");
            }
        }

        private static void AddTraceability(ref Dictionary<string, string> valueDictionary, CMFImplantDirector director)
        {
            valueDictionary.Add("VERSION", director.version.ToString("D"));
            valueDictionary.Add("DRAFT", director.draft.ToString("D"));           
            valueDictionary.Add("TIMESTAMP", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
            valueDictionary.Add("IDS_BUILD", IDSPluginHelper.PluginVersion);
            valueDictionary.Add("INPUT_FILE_TYPE", 
                InputFileTypeConverter.GetStringValue(director.CurrentInputFileType));
        }

        public void ExportImplantPictures(string fullPathForExport, CMFImplantDirector director)
        {
            var designParamQuery = new DesignParameterQuery(director);
            ImplantFrontImagesBase64JpegByteString.ForEach(x =>
            {
                ImageUtilities.SaveBase64JpegToFile(x.Base64JpegByteString, 
                    fullPathForExport,$"{director.caseId}_{x.CasePref.CasePrefData.ImplantTypeValue}" +
                                      $"{designParamQuery.GetImplantUniqueNumber(x.CasePref)}_numbered", "png");
            });
        }

        public void ExportBoneThicknessAnalysisImageForART(string fullPathForExport, CMFImplantDirector director)
        {
            var creator = new BoneThicknessAnalysisARTScreenshotsCreator(director, BoneThicknessMapQuery, fullPathForExport);
            creator.ExportScreenshotsOnAllImplantCase();
        }

        public override void ExportReport(IImplantDirector director, string fullPathAndFilename, IQCResources resources)
        {
            var isShowing = Panels.IsPanelVisible(PanelIds.Layers);
            if (isShowing)
            {
                Panels.ClosePanel(PanelIds.Layers);
            }

            // Fill the ReportDict
            Dictionary<string, string> reportDict;
            var success = FillReport(director, fullPathAndFilename, out reportDict);
            if (!success)
                throw new IDSException("Could not fill report.");

            // Fill the template
            var template = File.ReadAllText(resources.qcDocumentHtmlFile);

            string css;
            if (director.IsForUserTesting)
            {
                css = File.ReadAllText(resources.qcDocumentCssTestVersionFile);
            }
            else
            {
                css = File.ReadAllText(resources.qcDocumentCssFile);
            }

            template = template.Replace("[CSS_STYLE]", css);

            var javascript = File.ReadAllText(resources.qcDocumentJavaScriptFile);
            template = template.Replace("[JAVASCRIPT]", javascript);

            template = template.Replace("[DYNAMIC_IMPLANT]", _implantSectionHtml);

            template = template.Replace("[DYNAMIC_GUIDE]", _guideSectionHtml);

            var report = QCReportUtilities.FormatFromDictionary(template, reportDict);

            // Export
            File.WriteAllText(fullPathAndFilename, report);

            if (isShowing)
            {
                Panels.OpenPanel(PanelIds.Layers);
            }
        }

        public bool CanExportReport(CMFImplantDirector director)
        {
            if (ReportDocumentType == DocumentType.PlanningQC)
            {
                return true;
            }

            var objectManager = new CMFObjectManager(director);
            var screws = objectManager.GetAllBuildingBlocks(IBB.Screw).Select(screw => screw as Screw).ToList();

            //check all screw indexes are assigned
            if (screws.Exists(x => x.Index == -1))
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "Please assign implant screw numbers!");
                return false;
            }

            var originalOsteotomies = ProPlanImportUtilities.GetAllOriginalOsteotomyParts(director.Document);
            if (screws.Any() && !originalOsteotomies.Any())
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "Missing original osteotomy parts!");
            }

            var guideScrews = objectManager.GetAllBuildingBlocks(IBB.GuideFixationScrew).Select(screw => screw as Screw).ToList();
            if (guideScrews.Exists(x => x.Index == -1))
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "Please assign guide fixation screw numbers!");
                return false;
            }

            return true;
        }
    }
}
