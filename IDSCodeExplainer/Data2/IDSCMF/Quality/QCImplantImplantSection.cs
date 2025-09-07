using IDS.CMF.CasePreferences;
using IDS.CMF.Constants;
using IDS.CMF.FileSystem;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Query;
using IDS.CMF.Utilities;
using IDS.CMF.Visualization;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Quality;
using IDS.Core.Utilities;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace IDS.CMF.Quality
{
    public class QCImplantImplantSection
    {
        public struct ImageData
        {
            public CasePreferenceDataModel CasePref { get; set; }
            public string Base64JpegByteString { get; set; }
            public bool IsActualImplant { get; set; } //else is implant preview
        }

        private readonly CMFImplantDirector _director;
        private readonly CMFObjectManager _objectManager;
        public List<ImageData> ImplantFrontImagesBase64JpegByteString { get; set; } = new List<ImageData>();

        public QCImplantImplantSection(CMFImplantDirector director)
        {
            _director = director;
            _objectManager = new CMFObjectManager(director);
        }

        public void ImplantImplantInfo(ref Dictionary<string, string> valueDictionary, CasePreferenceDataModel casePrefData, DocumentType docType, QcDocBoneThicknessMapQuery boneThicknessMapQuery)
        {
            if (docType == DocumentType.PlanningQC)
            {
                // Visibility Settings
                ////////////////
                valueDictionary.Add("IMPLANT_DISPLAY", "none");
            }
            else
            {
                ImplantFrontImagesBase64JpegByteString = new List<ImageData>();

                var timerComponent = new Stopwatch();
                timerComponent.Start();
                var parameterValueTracking = new Dictionary<string, string>();

                var bonesQuery = new QCDocumentBonesQuery(_director);
                var allBones = bonesQuery.GetImplantBones();

                var documentType = "-";
                if (docType == DocumentType.MetalQC)
                {
                    documentType = "MetalQC";
                    var implantComponent = new ImplantCaseComponent();
                    var extendedBuildingBlock = implantComponent.GetImplantBuildingBlock(IBB.ImplantPreview, casePrefData);
                    var hasImplantPreview = _objectManager.HasBuildingBlock(extendedBuildingBlock);
                    if (hasImplantPreview)
                    {
                        valueDictionary.Add("IMG_IMPLANT_ONBONE", ScreenshotsImplant.GenerateImplantPreviewOnBoneImageString(_director, casePrefData));

                        var imagesString = ScreenshotsImplant.GenerateImplantPreviewWithScrewsImagesString(_director, casePrefData, new List<CameraView> { CameraView.Right, CameraView.Front, CameraView.Left });
                        valueDictionary.Add("IMG_IMPLANT_SCREWNO_RIGHT", imagesString[0]);

                        valueDictionary.Add("IMG_IMPLANT_SCREWNO_FRONT", imagesString[1]);

                        valueDictionary.Add("IMG_IMPLANT_SCREWNO_LEFT", imagesString[2]);

                        var implantPreview = (Mesh)_objectManager.GetBuildingBlock(extendedBuildingBlock).DuplicateGeometry();
                        FillDictionaryForImplantClearance(ref valueDictionary, casePrefData, allBones, implantPreview);

                        timerComponent.Stop();
                        parameterValueTracking.Add($"FillInImplantQC-ImplantImplantInfo-FillDictionaryForImplantClearance (Breakdown-Tracked)", $"{ (timerComponent.ElapsedMilliseconds * 0.001)}");

                        implantPreview.Dispose();
                    }

                    // Visibility Settings
                    ////////////////
                    valueDictionary.Add("IMPLANT_DISPLAY", hasImplantPreview ? "block" : "none");
                    if (hasImplantPreview)
                    {
                        valueDictionary.Add("IMPLANT_FIXING_DISPLAY", "none");
                    }
                }
                else if (docType == DocumentType.ApprovedQC)
                {
                    documentType = "ApprovedQC";
                    var implantComponent = new ImplantCaseComponent();
                    var extendedBuildingBlock = implantComponent.GetImplantBuildingBlock(IBB.ActualImplant, casePrefData);
                    var hasActualImplant = _objectManager.HasBuildingBlock(extendedBuildingBlock);
                    if (hasActualImplant)
                    {

                        valueDictionary.Add("IMG_IMPLANT_ONBONE", ScreenshotsImplant.GenerateActualImplantOnBoneImageString(_director, casePrefData));

                        timerComponent.Stop();
                        parameterValueTracking.Add($"FillInImplantQC-ImplantImplantInfo-GenerateActualImplantOnBoneImageString", $"{ (timerComponent.ElapsedMilliseconds * 0.001)}");
                        timerComponent.Restart();

                        var actualImplantWithScrewNumberFrontView = ScreenshotsImplant.GenerateFrontImplantWithScrewsImageString(_director, casePrefData, false);
                        ImplantFrontImagesBase64JpegByteString.Add(new ImageData() { Base64JpegByteString = actualImplantWithScrewNumberFrontView, CasePref = casePrefData, IsActualImplant = false });

                        var imagesString = ScreenshotsImplant.GenerateActualImplantWithScrewsImagesString(_director, casePrefData, new List<CameraView> { CameraView.Right, CameraView.Front, CameraView.Left });

                        valueDictionary.Add("IMG_IMPLANT_SCREWNO_RIGHT", imagesString[0]);

                        valueDictionary.Add("IMG_IMPLANT_SCREWNO_FRONT", imagesString[1]);


                        valueDictionary.Add("IMG_IMPLANT_SCREWNO_LEFT", imagesString[2]);

                        timerComponent.Stop();
                        parameterValueTracking.Add($"FillInImplantQC-ImplantImplantInfo-GenerateActualImplantWithScrewsImageString", $"{ (timerComponent.ElapsedMilliseconds * 0.001)}");
                        timerComponent.Restart();

                        var actualImplant = (Mesh)_objectManager.GetBuildingBlock(extendedBuildingBlock).DuplicateGeometry();

                        FillDictionaryForImplantClearance(ref valueDictionary, casePrefData, allBones, actualImplant);

                        timerComponent.Stop();
                        parameterValueTracking.Add($"FillInImplantQC-ImplantImplantInfo-FillDictionaryForImplantClearance (Breakdown-Tracked)", $"{ (timerComponent.ElapsedMilliseconds * 0.001)}");

                        timerComponent.Restart();
                        QcDocumentUtilities.FillDictionaryForMeshFixing(ref valueDictionary, actualImplant, "IMPLANT", ref parameterValueTracking);
                        timerComponent.Stop();
                        parameterValueTracking.Add($"FillInImplantQC-ImplantImplantInfo-FillDictionaryForImplantFixing", $"{ (timerComponent.ElapsedMilliseconds * 0.001)}");

                        actualImplant.Dispose();
                    }

                    // Visibility Settings
                    ////////////////
                    valueDictionary.Add("IMPLANT_DISPLAY", hasActualImplant ? "block" : "none");
                    if (hasActualImplant)
                    {
                        valueDictionary.Add("IMPLANT_FIXING_DISPLAY", "block");
                    }
                }

                timerComponent.Restart();
                var tagScrewTable = ImplantScrewQcKeys.ImplantScrewInfoTableSessionKey;
                valueDictionary.Add(tagScrewTable, GenerateImplantScrewInfoTableContent(casePrefData));
                timerComponent.Stop();
                parameterValueTracking.Add($"FillInImplantQC-ImplantImplantInfo-GenerateImplantScrewInfoTableContent (ScrewQC)", $"{ (timerComponent.ElapsedMilliseconds * 0.001)}");

                timerComponent.Restart();
                FillDictionaryForImplantBoneThicknessMapSection(ref valueDictionary, casePrefData, boneThicknessMapQuery);
                timerComponent.Stop();
                parameterValueTracking.Add($"FillInImplantQC-ImplantImplantInfo-FillDictionaryForImplantBoneThicknessMapSection (BoneThickness)", $"{ (timerComponent.ElapsedMilliseconds * 0.001)}");

                Msai.TrackDevEvent($"QCDoc Implant Info Section ({documentType}) {casePrefData.CasePrefData.ImplantTypeValue}", "CMF", parameterValueTracking);
                Msai.PublishToAzure();

                allBones.Dispose();
            }
        }

        private void FillDictionaryForImplantClearance(ref Dictionary<string, string> valueDictionary, CasePreferenceDataModel casePrefData, Mesh bone, Mesh implant)
        {
            var timerComponent = new Stopwatch();
            timerComponent.Start();
            var timeRecorded = new Dictionary<string, string>();

            var implantForClearance = implant.DuplicateMesh();
            implantForClearance.Compact(); //remove free points

            var minimum = MeshUtilities.Mesh2MeshSignedMinimumDistance(implantForClearance, bone,
                out var vertexDistances, out _, out var elapsedSecond);

            timeRecorded.Add("CalculateSignedDistancesForImplantClearance", $"{elapsedSecond}");

            valueDictionary.Add("IMPLANT_MIN_CLEARANCE", minimum.ToString("F4", CultureInfo.InvariantCulture));

            var implantClearance = ScreenshotsUtilities.GenerateClearanceMeshForScreenshot(implantForClearance, vertexDistances.ToList());

            timerComponent.Stop();
            timeRecorded.Add($"Generate ClearanceMeshForScreenshot", $"{ (timerComponent.ElapsedMilliseconds * 0.001)}");
            timerComponent.Restart();

            var imagesString = ScreenshotsImplant.GenerateImplantClearanceImagesString(_director, casePrefData, implantClearance, new List<CameraView> { CameraView.NegateLeft, CameraView.Back, CameraView.NegateRight });

            valueDictionary.Add("IMG_IMPLANT_CLEARANCE_LEFT", imagesString[0]);

            valueDictionary.Add("IMG_IMPLANT_CLEARANCE_FRONT", imagesString[1]);

            valueDictionary.Add("IMG_IMPLANT_CLEARANCE_RIGHT", imagesString[2]);

            timerComponent.Stop();
            timeRecorded.Add($"Generate ImplantClearanceImageString", $"{ (timerComponent.ElapsedMilliseconds * 0.001)}");

            Msai.TrackDevEvent($"QCDoc Implant Info Section-FillInImplantQC-ImplantImplantInfo-FillDictionaryForImplantClearance-Breakdown {casePrefData.CasePrefData.ImplantTypeValue}", "CMF", timeRecorded);

            implantForClearance.Dispose();
        }

        public void FillDictionaryForImplantBoneThicknessMapSection(ref Dictionary<string, string> valueDictionary, CasePreferenceDataModel casePrefData, QcDocBoneThicknessMapQuery boneThicknessMapQuery)
        {
            var cmfResources = new CMFResources();

            var dynamicHtml = File.ReadAllText(cmfResources.qcDocumentImplantBoneThicknessAnalysisDynamicScriptFile);
            var bonesThicknessSectionHTML = new StringBuilder();

            var bonesScrewsData = boneThicknessMapQuery.GetGroupScrewWithBone(casePrefData);
            foreach (var boneScrewData in bonesScrewsData)
            {
                var bone = boneScrewData.Key;
                var screws = boneScrewData.Value;

                double lowerBound, upperBound;
                var thicknessMesh = boneThicknessMapQuery.DoWallThicknessAnalysisForQCDoc(bone, out lowerBound, out upperBound);
                if (thicknessMesh == null)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, $"Failed to do bone thickness analysis for \"{bone.Name}\"!");
                    continue;
                }

                var boneThicknessDict = new Dictionary<string, string>();
                boneThicknessDict.Add("IMG_BONE_THICKNESS_MAP", ScreenshotsImplant.GenerateImplantBoneThicknessImageString(_director, casePrefData, thicknessMesh, screws));
                boneThicknessDict.Add("BONE_THICKNESS_TYPE", BoneNamePreferencesManager.Instance.GetPreferenceBoneName(_director, bone));
                boneThicknessDict.Add("BONE_THICKNESS_VALUE_MIN", lowerBound.ToString("F"));
                boneThicknessDict.Add("BONE_THICKNESS_VALUE_LOWER_QUARTILE", ((3 * lowerBound + upperBound) / 4).ToString("F"));
                boneThicknessDict.Add("BONE_THICKNESS_VALUE_MID_QUARTILE", ((lowerBound + upperBound) / 2).ToString("F"));
                boneThicknessDict.Add("BONE_THICKNESS_VALUE_UPPER_QUARTILE", ((lowerBound + 3 * upperBound) / 4).ToString("F"));
                boneThicknessDict.Add("BONE_THICKNESS_VALUE_MAX", upperBound.ToString("F"));

                var boneThicknessHTML = string.Copy(dynamicHtml);
                bonesThicknessSectionHTML.Append(QCReportUtilities.FormatFromDictionary(boneThicknessHTML, boneThicknessDict));
            }

            valueDictionary.Add("BONE_THICKNESS_MAPS_SECTION", bonesThicknessSectionHTML.ToString());
        }

        private string GenerateImplantScrewInfoTableContent(CasePreferenceDataModel casePrefData)
        {
            var resources = new CMFResources();
            var screwQcHtml = File.ReadAllText(resources.qcDocumentImplantScrewQcDynamicScriptFile);

            var qcScrewQcSection = new QCScrewQcSection(_director);
            var tableHtml = qcScrewQcSection.GenerateImplantScrewInfoTableContent(casePrefData);

            var implantScrewQcDict = new Dictionary<string, string>
            {
                { ImplantScrewQcKeys.ImplantScrewInfoTableKey, tableHtml},
            };
            
            return QCReportUtilities.FormatFromDictionary(screwQcHtml, implantScrewQcDict);
        }
    }
}
