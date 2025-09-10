using IDS.CMF.CasePreferences;
using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.FileSystem;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Query;
using IDS.CMF.Utilities;
using IDS.CMF.Visualization;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Quality;
using IDS.Core.Utilities;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace IDS.CMF.Quality
{
    public class QCGuideSection
    {
        private readonly CMFImplantDirector _director;
        private readonly CMFObjectManager _objectManager;
        private readonly QCScrewQcSection _screwQcSection;

        public QCGuideSection(CMFImplantDirector director)
        {
            this._director = director;
            _objectManager = new CMFObjectManager(director);
            _screwQcSection = new QCScrewQcSection(director);
        }

        public void FillGuideInfo(ref Dictionary<string, string> valueDictionary, GuidePreferenceDataModel guidePrefData, DocumentType docType)
        {
            FillDictionaryForGuidePreferenceInfo(ref valueDictionary, guidePrefData);

            if (docType == DocumentType.PlanningQC)
            {
                // Visibility Settings
                ////////////////
                valueDictionary.Add("CHECK_GUIDE_EXIST", "none");
                valueDictionary.Add("GUIDE_TEETH_IMPRESSION_DEPTH_DISPLAY", "none");
                valueDictionary.Add("GUIDE_TEETH_BLOCK_THICKNESS_DISPLAY", "none");
                valueDictionary.Add("GUIDE_TEETH_BLOCK_CLEARANCE_DISPLAY", "none");
                valueDictionary.Add("GUIDE_FIXING_DISPLAY", "none");
            }
            else
            {
                FillDictionaryForGuideInfo(ref valueDictionary, guidePrefData, docType);
            }
        }

        private void FillDictionaryForGuidePreferenceInfo(ref Dictionary<string, string> valueDictionary, GuidePreferenceDataModel guidePrefData)
        {
            var tagGuideCutSlot = "VAL_PLANNING_GUIDE_CUT";
            var tagGuideFixScrew = "VAL_PLANNING_GUIDE_FIX";
            var tagGuideFixScrewStyle = "VAL_PLANNING_GUIDE_FIX_STYLE";
            var tagGuideFlange = "VAL_PLANNING_GUIDE_FLANGE";
            var tagGuideConnections = "VAL_PLANNING_GUIDE_CONN";
            var tagRemarks = "VAL_PLANNING_GUIDE_REMARKS";

            valueDictionary.Add(tagGuideCutSlot, guidePrefData.GuidePrefData.GuideCutSlotValue);
            valueDictionary.Add(tagGuideFixScrew, guidePrefData.GuidePrefData.GuideScrewTypeValue);
            valueDictionary.Add(tagGuideFixScrewStyle, guidePrefData.GuidePrefData.GuideScrewStyle);
            valueDictionary.Add(tagGuideFlange, (guidePrefData.GuidePrefData.GuideFlange ? "Yes" : "No") + GetDisplayGuideFlangeHeights(guidePrefData));
            valueDictionary.Add(tagGuideConnections, guidePrefData.GuidePrefData.GuideConnectionsValue);
            valueDictionary.Add(tagRemarks, RichTextBoxUtilities.ConvertRichTextBoxToHtmlString(ByteUtilities.ConvertBytesToRichTextBox(guidePrefData.GuidePrefData.GuideInfoRemarks)));

            valueDictionary.Add("DISPLAY_GUIDE_CUT", guidePrefData.GuidePrefData.GuideCutSlotValue != string.Empty ? "" : "none");
            valueDictionary.Add("DISPLAY_GUIDE_FIX", guidePrefData.GuidePrefData.GuideScrewTypeValue != string.Empty ? "" : "none");
            valueDictionary.Add("DISPLAY_GUIDE_CONN", guidePrefData.GuidePrefData.GuideConnectionsValue != string.Empty ? "" : "none");
        }

        private Mesh _guideBonesRoICacheQca = null;
        private Mesh _guideBonesRoIQca
        {
            get
            {
                if (_guideBonesRoICacheQca == null)
                {
                    var bonesQuery = new QCDocumentBonesQuery(_director);
                    var allBones = bonesQuery.GetGuideBonesForGuideClearance();

                    var actuals = _objectManager.GetAllBuildingBlocks(IBB.GuideSurface).ToList();

                    Mesh actualImplantsAppended;
                    Booleans.PerformBooleanUnion(out actualImplantsAppended, actuals.Select(x => (Mesh) x.Geometry).ToArray());

                    _guideBonesRoICacheQca = GuideDrawingUtilities.CreateRoiMesh(allBones, actualImplantsAppended,1);
                }

                return _guideBonesRoICacheQca;
            }
        }

        private void FillDictionaryForGuideInfo(ref Dictionary<string, string> valueDictionary, GuidePreferenceDataModel guidePrefData, DocumentType docType)
        {
            var timerComponent = new Stopwatch();
            timerComponent.Start();
            var parameterValueTracking = new Dictionary<string, string>();

            var guideScrewInfoTableSession = GuideScrewQcKeys.GuideScrewInfoTableSessionKey;
            valueDictionary.Add(guideScrewInfoTableSession, FillDictionaryForGuideScrewQcTableSession(guidePrefData));
            timerComponent.Stop();
            parameterValueTracking.Add($"QCGuideSection.GenerateGuideScrewInfoTableContent (ScrewQC)", $"{ (timerComponent.ElapsedMilliseconds * 0.001)}");
            timerComponent.Restart();

            var bonesQuery = new QCDocumentBonesQuery(_director);
            var allBonesForGuideClearance = bonesQuery.GetGuideBonesForGuideClearance();

            timerComponent.Stop();
            parameterValueTracking.Add($"QCDocumentBonesQuery.GetGuideBonesForGuideClearance", $"{ (timerComponent.ElapsedMilliseconds * 0.001)}");
            timerComponent.Restart();

            if (docType == DocumentType.MetalQC)
            {
                var documentTypeString = "MetalQC";
                var guideComponent = new GuideCaseComponent();
                var extendedBuildingBlock = guideComponent.GetGuideBuildingBlock(IBB.GuidePreviewSmoothen, guidePrefData);
                var hasGuidePreview = _objectManager.HasBuildingBlock(extendedBuildingBlock);
                var hasTeethImpressionDepth = false;
                var hasTeethBlockThickness = false;
                var hasTeethBlockClearance = false;

                if (hasGuidePreview)
                {
                    valueDictionary.Add("IMG_GUIDE_ONBONE", ScreenshotsGuide.GenerateGuidePreviewSmoothenOnBoneImageString(_director, guidePrefData));

                    var imagesString = ScreenshotsGuide.GenerateGuidePreviewSmoothenWithScrewsImagesString(_director, guidePrefData, new List<CameraView> { CameraView.Right, CameraView.Front, CameraView.Left });
                    valueDictionary.Add("IMG_GUIDE_RIGHT", imagesString[0]);
                    valueDictionary.Add("IMG_GUIDE_FRONT", imagesString[1]);
                    valueDictionary.Add("IMG_GUIDE_LEFT", imagesString[2]);

                    var guidePreview = (Mesh)_objectManager.GetBuildingBlock(extendedBuildingBlock).DuplicateGeometry();
                    FillDictionaryForGuideClearance(ref valueDictionary, guidePrefData, allBonesForGuideClearance, guidePreview);
                    guidePreview.Dispose();

                    hasTeethImpressionDepth = FillDictionaryForGuideTeethImpressionDepth(ref valueDictionary, guidePrefData);

                    hasTeethBlockThickness = FillDictionaryForGuideTeethBlockThickness(ref valueDictionary, guidePrefData);

                    hasTeethBlockClearance = FillDictionaryForGuideTeethBlockClearance(ref valueDictionary, guidePrefData);
                }

                // Visibility Settings
                ////////////////
                valueDictionary.Add("CHECK_GUIDE_EXIST", hasGuidePreview ? "block" : "none");
                valueDictionary.Add("GUIDE_TEETH_IMPRESSION_DEPTH_DISPLAY", hasGuidePreview && hasTeethImpressionDepth ? "block" : "none");
                valueDictionary.Add("GUIDE_TEETH_BLOCK_THICKNESS_DISPLAY", hasGuidePreview && hasTeethBlockThickness ? "block" : "none");
                valueDictionary.Add("GUIDE_TEETH_BLOCK_CLEARANCE_DISPLAY", hasGuidePreview && hasTeethBlockClearance ? "block" : "none");
                valueDictionary.Add("GUIDE_FIXING_DISPLAY", "none");
            }
            else if (docType == DocumentType.ApprovedQC)
            {
                var documentType = "ApprovedQC";
                var guideComponent = new GuideCaseComponent();
                var extendedBuildingBlock = guideComponent.GetGuideBuildingBlock(IBB.ActualGuide, guidePrefData);
                var hasActualGuide = _objectManager.HasBuildingBlock(extendedBuildingBlock);
                var hasTeethImpressionDepth = false;
                var hasTeethBlockThickness = false;
                var hasTeethBlockClearance = false;

                if (hasActualGuide)
                {
                    valueDictionary.Add("IMG_GUIDE_ONBONE", ScreenshotsGuide.GenerateActualGuideOnBoneImageString(_director, guidePrefData));

                    timerComponent.Stop();
                    parameterValueTracking.Add($"GenerateActualGuideOnBoneImageString", $"{ (timerComponent.ElapsedMilliseconds * 0.001)}");
                    timerComponent.Restart();

                    var imagesString = ScreenshotsGuide.GenerateActualGuideWithScrewsImagesString(_director, guidePrefData, new List<CameraView> { CameraView.Right, CameraView.Front, CameraView.Left });
                    
                    valueDictionary.Add("IMG_GUIDE_RIGHT", imagesString[0]);

                    valueDictionary.Add("IMG_GUIDE_FRONT", imagesString[1]);

                    valueDictionary.Add("IMG_GUIDE_LEFT", imagesString[2]);

                    timerComponent.Stop();
                    parameterValueTracking.Add($"GenerateActualGuideString", $"{ (timerComponent.ElapsedMilliseconds * 0.001)}");
                    timerComponent.Restart();

                    var guide = (Mesh)_objectManager.GetBuildingBlock(extendedBuildingBlock).DuplicateGeometry();

                    FillDictionaryForGuideClearance(ref valueDictionary, guidePrefData, allBonesForGuideClearance, guide);

                    timerComponent.Stop();
                    parameterValueTracking.Add($"FillDictionaryForGuideClearance", $"{ (timerComponent.ElapsedMilliseconds * 0.001)}");

                    hasTeethImpressionDepth = FillDictionaryForGuideTeethImpressionDepth(ref valueDictionary, guidePrefData);

                    hasTeethBlockThickness = FillDictionaryForGuideTeethBlockThickness(ref valueDictionary, guidePrefData);

                    hasTeethBlockClearance = FillDictionaryForGuideTeethBlockClearance(ref valueDictionary, guidePrefData);

                    timerComponent.Restart();
                    QcDocumentUtilities.FillDictionaryForMeshFixing(ref valueDictionary, guide, "GUIDE", ref parameterValueTracking);
                    timerComponent.Stop();
                    parameterValueTracking.Add($"FillDictionaryForGuideFixing", $"{ (timerComponent.ElapsedMilliseconds * 0.001)}");

                    Msai.TrackDevEvent($"QCDoc Guide Info Section ({documentType}) {guidePrefData.GuidePrefData.GuideTypeValue}", "CMF", parameterValueTracking);
                    Msai.PublishToAzure();

                    guide.Dispose();
                }

                // Visibility Settings
                ////////////////
                valueDictionary.Add("CHECK_GUIDE_EXIST", hasActualGuide ? "block" : "none");
                valueDictionary.Add("GUIDE_TEETH_IMPRESSION_DEPTH_DISPLAY", hasActualGuide && hasTeethImpressionDepth ? "block" : "none");
                valueDictionary.Add("GUIDE_TEETH_BLOCK_THICKNESS_DISPLAY", hasActualGuide && hasTeethBlockThickness ? "block" : "none");
                valueDictionary.Add("GUIDE_TEETH_BLOCK_CLEARANCE_DISPLAY", hasActualGuide && hasTeethBlockClearance ? "block" : "none");
                valueDictionary.Add("GUIDE_FIXING_DISPLAY", hasActualGuide ? "block" : "none");

                allBonesForGuideClearance.Dispose();
            }
        }

        private void FillDictionaryForGuideClearance(ref Dictionary<string, string> valueDictionary, GuidePreferenceDataModel guidePrefData, Mesh bone, Mesh guide)
        {
            var timer2 = new Stopwatch();
            timer2.Start();
            var timeRecorded = new Dictionary<string, string>();

            var guideForClearance = guide.DuplicateMesh();
            guideForClearance.Compact(); //remove free points

            timer2.Stop();
            timeRecorded.Add($"guide.DuplicateMesh() & Compact", $"{ (timer2.ElapsedMilliseconds * 0.001)}");
            timer2.Restart();

            var minimum = MeshUtilities.Mesh2MeshSignedMinimumDistance(guideForClearance, bone, 
                out var vertexDistances, out _, out var elapsedSecond, true);
            
            timeRecorded.Add("CalculateSignedDistancesForGuideClearance", $"{elapsedSecond}");

            timer2.Stop();
            timeRecorded.Add($"TriangleSurfaceDistance.DistanceBetween", $"{ (timer2.ElapsedMilliseconds * 0.001)}");
            timer2.Restart();

            valueDictionary.Add("GUIDE_MIN_CLEARANCE", minimum.ToString("F4", CultureInfo.InvariantCulture));
            
            var guideClearance = ScreenshotsUtilities.GenerateClearanceMeshForScreenshot(guideForClearance, vertexDistances.ToList());

            timer2.Stop();
            timeRecorded.Add($"GenerateClearanceMeshForScreenshot", $"{ (timer2.ElapsedMilliseconds * 0.001)}");
            timer2.Restart();

            var imagesString = ScreenshotsGuide.GenerateMeshDistanceImagesString(_director, guidePrefData, guideClearance, new List<CameraView> { CameraView.NegateLeft, CameraView.Back, CameraView.NegateRight });

            valueDictionary.Add("IMG_GUIDE_CLEARANCE_LEFT", imagesString[0]);

            valueDictionary.Add("IMG_GUIDE_CLEARANCE_FRONT", imagesString[1]);

            valueDictionary.Add("IMG_GUIDE_CLEARANCE_RIGHT", imagesString[2]);

            timer2.Stop();
            timeRecorded.Add($"GenerateGuideClearanceImageString", $"{ (timer2.ElapsedMilliseconds * 0.001)}");

            Msai.TrackDevEvent($"QCDoc Guide Info Section-FillDictionaryForGuideClearance {guidePrefData.GuidePrefData.GuideTypeValue} ", "CMF", timeRecorded);
            Msai.PublishToAzure();

            guideForClearance.Dispose();
        }

        private string FillDictionaryForGuideScrewQcTableSession(GuidePreferenceDataModel guidePrefData)
        {
            var cmfResources = new CMFResources();
            var dynamicHtml = File.ReadAllText(cmfResources.qcDocumentGuideScrewQcDynamicScriptFile);
            var guideScrewQcDict = new Dictionary<string, string>()
            {
                {GuideScrewQcKeys.GuideScrewInfoTableKey, _screwQcSection.GenerateGuideScrewInfoTableContent(guidePrefData)}
            };

            return QCReportUtilities.FormatFromDictionary(dynamicHtml, guideScrewQcDict);
        }

        private string GetDisplayGuideFlangeHeights(GuidePreferenceDataModel guidePrefData)
        {
            var guideComponent = new GuideCaseComponent();
            var extendedBuildingBlock = guideComponent.GetGuideBuildingBlock(IBB.GuideFlange, guidePrefData);
            var guideFlanges = _objectManager.GetAllBuildingBlocks(extendedBuildingBlock);
            if (!guideFlanges.Any())
            {
                return string.Empty;
            }

            var heightValues = new List<double>();

            var helper = new GuideFlangeObjectHelper(_director);

            foreach (var flangeObj in guideFlanges)
            {
                var flangeHeight = helper.GetFlangeHeight(flangeObj);
                heightValues.Add(flangeHeight);
            }

            return $"<br/>Height(s) (mm): { string.Join(", ", heightValues.OrderBy(w => w).Select(w => string.Format(CultureInfo.InvariantCulture, "{0:F1}", w)).Distinct())}";
        }

        private bool FillDictionaryForGuideTeethImpressionDepth(ref Dictionary<string, string> valueDictionary, GuidePreferenceDataModel guidePrefData)
        {
            var guideComponent = new GuideCaseComponent();
            var extendedBuildingBlock = guideComponent.GetGuideBuildingBlock(IBB.TeethBlock, guidePrefData);
            if (!_objectManager.HasBuildingBlock(extendedBuildingBlock))
            {
                return false;
            }

            var manager = new CastAnalysisManager(_director);

            var teethBlockId = _objectManager.GetBuildingBlockId(extendedBuildingBlock);
            var castType = manager.FindParentCastPartType(teethBlockId);

            if (castType != ProPlanImportPartType.MaxillaCast && castType != ProPlanImportPartType.MandibleCast)
            {
                return false;
            }

            if (!manager.HasInputsForTeethImpressionDepthAnalysis(castType))
            {
                return false;
            }

            manager.PerformTeethImpressionDepthAnalysis(castType, out var triangleCenterDistances);

            var hasAccurateMaxDepth = manager.GetAccurateTeethImpressionMaxDepth(castType, out var actualMaxDepth);
            if (hasAccurateMaxDepth)            
            {
                valueDictionary.Add("GUIDE_MAX_TEETH_IMPRESSION_DEPTH", actualMaxDepth.ToString("F4", CultureInfo.InvariantCulture));
            }
            else
            {
                valueDictionary.Add("GUIDE_MAX_TEETH_IMPRESSION_DEPTH", $"{triangleCenterDistances.Max().ToString("F4", CultureInfo.InvariantCulture)} (Estimated)");
            }            

            var teethImpressionDepthMesh = manager.ApplyTeethImpressionDepthAnalysis(castType, triangleCenterDistances, false);

            var cameraViews = new List<CameraView> { CameraView.Right, castType == ProPlanImportPartType.MaxillaCast ? CameraView.Bottom : CameraView.Top, CameraView.Left };
            var imagesString = ScreenshotsGuide.GenerateMeshDistanceImagesString(_director, guidePrefData, teethImpressionDepthMesh, cameraViews);

            valueDictionary.Add("IMG_GUIDE_TEETH_IMPRESSION_DEPTH_RIGHT", imagesString[0]);

            valueDictionary.Add("IMG_GUIDE_TEETH_IMPRESSION_DEPTH_TOPBOTTOM", imagesString[1]);

            valueDictionary.Add("IMG_GUIDE_TEETH_IMPRESSION_DEPTH_LEFT", imagesString[2]);

            return true;
        }

        private bool FillDictionaryForGuideTeethBlockThickness(ref Dictionary<string, string> valueDictionary, GuidePreferenceDataModel guidePrefData)
        {
            var guideComponent = new GuideCaseComponent();
            var extendedBuildingBlock = guideComponent.GetGuideBuildingBlock(IBB.TeethBlock, guidePrefData);
            if (!_objectManager.HasBuildingBlock(extendedBuildingBlock))
            {
                return false;
            }

            var manager = new TeethBlockAnalysisManager(_director);

            manager.PerformThicknessAnalysis(guidePrefData, out var thicknessData);

            var teethBlockThicknessMesh = manager.ApplyThicknessAnalysis(guidePrefData, thicknessData, false, out var lowerBound, out var upperBound);

            valueDictionary.Add("GUIDE_TEETH_BLOCK_THICKNESS_VALUE_MIN", lowerBound.ToString("F"));
            valueDictionary.Add("GUIDE_TEETH_BLOCK_THICKNESS_VALUE_LOWER_QUARTILE", ((3 * lowerBound + upperBound) / 4).ToString("F"));
            valueDictionary.Add("GUIDE_TEETH_BLOCK_THICKNESS_VALUE_MID_QUARTILE", ((lowerBound + upperBound) / 2).ToString("F"));
            valueDictionary.Add("GUIDE_TEETH_BLOCK_THICKNESS_VALUE_UPPER_QUARTILE", ((lowerBound + 3 * upperBound) / 4).ToString("F"));
            valueDictionary.Add("GUIDE_TEETH_BLOCK_THICKNESS_VALUE_MAX", upperBound.ToString("F"));

            var cameraViews = new List<CameraView> { CameraView.Right, CameraView.Front, CameraView.Left };
            var imagesString = ScreenshotsGuide.GenerateMeshDistanceImagesString(_director, guidePrefData, teethBlockThicknessMesh, cameraViews);

            valueDictionary.Add("IMG_GUIDE_TEETH_BLOCK_THICKNESS_RIGHT", imagesString[0]);

            valueDictionary.Add("IMG_GUIDE_TEETH_BLOCK_THICKNESS_FRONT", imagesString[1]);

            valueDictionary.Add("IMG_GUIDE_TEETH_BLOCK_THICKNESS_LEFT", imagesString[2]);

            return true;
        }

        private bool FillDictionaryForGuideTeethBlockClearance(ref Dictionary<string, string> valueDictionary, GuidePreferenceDataModel guidePrefData)
        {
            var guideComponent = new GuideCaseComponent();
            var extendedBuildingBlock = guideComponent.GetGuideBuildingBlock(IBB.TeethBlock, guidePrefData);
            if (!_objectManager.HasBuildingBlock(extendedBuildingBlock))
            {
                return false;
            }

            var manager = new CastAnalysisManager(_director);

            var teethBlock = _objectManager.GetBuildingBlock(extendedBuildingBlock);
            var castType = manager.FindParentCastPartType(teethBlock.Id);

            if (castType != ProPlanImportPartType.MaxillaCast && castType != ProPlanImportPartType.MandibleCast)
            {
                return false;
            }

            var castRhinoObject = manager.GetCastRhinoObject(castType);
            if (castRhinoObject == null)
            {
                return false;
            }

            var teethBlockMesh = (Mesh)teethBlock.DuplicateGeometry();
            teethBlockMesh.Compact(); //remove free points

            var castMesh = (Mesh)castRhinoObject.DuplicateGeometry();
            castMesh.Compact(); //remove free points

            var minimum = MeshUtilities.Mesh2MeshSignedMinimumDistance(teethBlockMesh, castMesh,
                out var vertexDistances, out _, out _, true);

            valueDictionary.Add("GUIDE_TEETH_BLOCK_MIN_CLEARANCE", minimum.ToString("F4", CultureInfo.InvariantCulture));

            var teethBlockClearance = ScreenshotsUtilities.GenerateClearanceMeshForScreenshot(teethBlockMesh, vertexDistances.ToList());

            var cameraViews = new List<CameraView> { CameraView.Right, castType == ProPlanImportPartType.MaxillaCast ? CameraView.Top : CameraView.Bottom, CameraView.Left };
            var imagesString = ScreenshotsGuide.GenerateMeshDistanceImagesString(_director, guidePrefData, teethBlockClearance, cameraViews);

            valueDictionary.Add("IMG_GUIDE_TEETH_BLOCK_CLEARANCE_RIGHT", imagesString[0]);

            valueDictionary.Add("IMG_GUIDE_TEETH_BLOCK_CLEARANCE_TOPBOTTOM", imagesString[1]);

            valueDictionary.Add("IMG_GUIDE_TEETH_BLOCK_CLEARANCE_LEFT", imagesString[2]);

            teethBlockMesh.Dispose();
            castMesh.Dispose();

            return true;
        }
    }
}
