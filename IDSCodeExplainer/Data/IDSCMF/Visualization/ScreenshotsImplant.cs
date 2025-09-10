using IDS.CMF.CasePreferences;
using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Quality;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Core.V2.Utilities;
using IDS.Interface.Implant;
using IDS.RhinoInterfaces.Converter;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IDS.CMF.Visualization
{
    internal static class ScreenshotsImplant
    {
        public static string GeneratePlanningImplantOnBoneImageString(CMFImplantDirector director, CasePreferenceDataModel casePrefData)
        {
            return GenerateBuildingBlockOnBoneImageString(director, casePrefData, IBB.PlanningImplant);
        }

        public static string GenerateImplantPreviewOnBoneImageString(CMFImplantDirector director, CasePreferenceDataModel casePrefData)
        {
            return GenerateBuildingBlockOnBoneImageString(director, casePrefData, IBB.ImplantPreview);
        }

        public static string GenerateFrontImplantWithScrewsImageString(CMFImplantDirector director, CasePreferenceDataModel casePrefData, bool isPreviewImplant)
        {
            var screwManager = new ScrewManager(director);
            var bubbleConduits = new List<NumberBubbleConduit>();
            var implantCaseComponent = new ImplantCaseComponent();
            var screwBuildingBlock = implantCaseComponent.GetImplantBuildingBlock(IBB.Screw, casePrefData);

            var objectManager = new CMFObjectManager(director);
            var screws = objectManager.GetAllBuildingBlocks(screwBuildingBlock).Select(s => (Screw)s).ToList();
            screws.ForEach(x =>
            {
                var pref = screwManager.GetImplantPreferenceTheScrewBelongsTo(x);
                
                var conduit = new NumberBubbleConduit(x.HeadPoint, x.Index, Color.AliceBlue,  ScrewUtilities.GetScrewTypeColor(x));
                conduit.BubbleRadius = ScrewNumberBubbleConduit.GetScrewNumberBubbleConduitSize(pref);
                conduit.DisplaySize = ScrewNumberBubbleConduit.GetScrewNumberDisplayConduitSize(pref);
                bubbleConduits.Add(conduit);
            });

            foreach (var conduit in bubbleConduits)
            {
                conduit.Enabled = true;
            }

            var res = GenerateBuildingBlockOnBonesInContactImageString(director, casePrefData, 
                isPreviewImplant ? IBB.ImplantPreview : IBB.ActualImplant, IBB.Screw);

            foreach (var conduit in bubbleConduits)
            {
                conduit.Enabled = false;
            }

            bubbleConduits.Clear();

            return res;
        }

        public static string GenerateActualImplantOnBoneImageString(CMFImplantDirector director, CasePreferenceDataModel casePrefData)
        {
            return GenerateBuildingBlockOnBoneImageString(director, casePrefData, IBB.ActualImplant);
        }

        public static List<string> GenerateImplantPreviewWithScrewsImagesString(CMFImplantDirector director, CasePreferenceDataModel casePrefData, List<CameraView> views)
        {
            return GenerateImplantBlockWithScrewsImagesString(director, casePrefData, IBB.ImplantPreview, views);
        }

        public static List<string> GenerateActualImplantWithScrewsImagesString(CMFImplantDirector director, CasePreferenceDataModel casePrefData, List<CameraView> views)
        {
            return GenerateImplantBlockWithScrewsImagesString(director, casePrefData, IBB.ActualImplant, views);
        }

        private static List<string> GenerateImplantBlockWithScrewsImagesString(CMFImplantDirector director, CasePreferenceDataModel casePrefDataModel, IBB implantBlock, List<CameraView> views)
        {
            var implantCaseComponent = new ImplantCaseComponent();
            var implantBuildingBlock = implantCaseComponent.GetImplantBuildingBlock(implantBlock, casePrefDataModel);
            var screwBuildingBlock = implantCaseComponent.GetImplantBuildingBlock(IBB.Screw, casePrefDataModel);

            var screwBubbleRadius = ScrewNumberBubbleConduit.GetScrewNumberBubbleConduitSize(casePrefDataModel.CasePrefData);
            var screwDisplaySize = ScrewNumberBubbleConduit.GetScrewNumberDisplayConduitSize(casePrefDataModel.CasePrefData);

            return ScreenshotsUtilities.GenerateBuildingBlockWithScrewsImagesString(director, implantBuildingBlock, screwBuildingBlock, screwBubbleRadius, screwDisplaySize, views);
        }

        public static List<string> GenerateImplantClearanceImagesString(CMFImplantDirector director, CasePreferenceDataModel casePrefData, Mesh implantClearance, List<CameraView> views)
        {
            if (implantClearance == null)
            {
                return ScreenshotsUtilities.GenerateEmptyStrings(views);
            }

            var doc = director.Document;
            Core.Visualization.Visibility.HideAll(doc);

            var desc = doc.Views.ActiveView.ActiveViewport.DisplayMode;
            var currentShadeVertexColors = desc.DisplayAttributes.ShadeVertexColors;
            if (currentShadeVertexColors)
            {
                desc.DisplayAttributes.ShadeVertexColors = false;
                doc.Views.ActiveView.ActiveViewport.DisplayMode = desc;
            }

            var guid = doc.Objects.AddMesh(implantClearance);

            var bBox = implantClearance.GetBoundingBox(true);

            var imagesString = ScreenshotsUtilities.GenerateImplantGuideImages(doc, bBox, views);           

            if (currentShadeVertexColors)
            {
                desc.DisplayAttributes.ShadeVertexColors = true;
                doc.Views.ActiveView.ActiveViewport.DisplayMode = desc;
            }

            doc.Objects.Delete(guid, true);

            return imagesString;
        }

        public static string GenerateImplantBoneThicknessImageString(CMFImplantDirector director, CasePreferenceDataModel casePrefData, Mesh boneThicknessMesh, 
            List<Screw> screws)
        {
            if (boneThicknessMesh == null)
            {
                return string.Empty;
            }

            Core.Visualization.Visibility.HideAll(director.Document);

            var prevRecordingState = director.Document.UndoRecordingEnabled;
            director.Document.UndoRecordingEnabled = false;

            var bBox = boneThicknessMesh.GetBoundingBox(true);

            var dots = ScrewUtilities.FindDotsTheScrewBelongsTo(screws, casePrefData.ImplantDataModel.DotList);
            var view = GetViewDirection(dots);

            var backgroundColor = Color.FromArgb(128, 128, 128);

            var removeSoonGuid = new List<Guid>();

            removeSoonGuid.Add(director.Document.Objects.AddMesh(boneThicknessMesh));
            foreach (var screw in screws)
            {
                removeSoonGuid.Add(director.Document.Objects.AddBrep(screw.Geometry as Brep));
            }

            var image = ScreenshotsUtilities.GenerateImplantGuideImage(director, bBox, view, backgroundColor);

            foreach (var guid in removeSoonGuid)
            {
                director.Document.Objects.Delete(guid, true);
            }

            director.Document.UndoRecordingEnabled = prevRecordingState;

            return image;
        }

        public static BoneThicknessAnalysisARTComponentScreenshots GenerateARTBoneThicknessImageString(CMFImplantDirector director, 
            List<Mesh> boneThicknessMeshes, List<Screw> screws)
        {
            BoneThicknessAnalysisARTComponentScreenshots screenshots = null;
            if (boneThicknessMeshes == null ||
                !boneThicknessMeshes.Any())
            {
                return null;
            }

            Core.Visualization.Visibility.HideAll(director.Document);

            var prevRecordingState = director.Document.UndoRecordingEnabled;
            director.Document.UndoRecordingEnabled = false;
            
            using (var conduit = new BoneThicknessAnalysisARTConduit())
            {
                var bBox = BoundingBox.Unset;

                var removeSoonGuid = new List<Guid>();

                try
                {
                    foreach (var boneThicknessMesh in boneThicknessMeshes)
                    {
                        removeSoonGuid.Add(director.Document.Objects.AddMesh(boneThicknessMesh));
                    }

                    foreach (var screw in screws)
                    {
                        conduit.ConduitsParam.Add(new BoneThicknessAnalysisARTConduit.ScrewMeshConduitParam(screw));
                        if (!bBox.IsValid)
                        {
                            bBox = screw.Geometry.GetBoundingBox(false);
                        }
                        else
                        {
                            bBox.Union(screw.Geometry.GetBoundingBox(false));
                        }
                    }

                    conduit.Enabled = true;

                    bBox = GeometryUtilities.ScaleBoundingBox(bBox.ToIDSBoundingBox(), 
                        BoneThicknessAnalysisForART.ScreenshotsZoom).ToBoundingBox();
                    var leftViewJpegBase64 =
                        ScreenshotsUtilities.GenerateImplantGuideImage(director, bBox, CameraView.FrontLeft);
                    var rightViewJpegBase64 =
                        ScreenshotsUtilities.GenerateImplantGuideImage(director, bBox, CameraView.FrontRight);

                    screenshots = new BoneThicknessAnalysisARTComponentScreenshots(
                        ImageUtilities.Base64JpegToImage(leftViewJpegBase64),
                        ImageUtilities.Base64JpegToImage(rightViewJpegBase64));

                    conduit.Enabled = false;
                }
                catch (Exception ex)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, $"Exception thrown during screenshots on ART, detail: {ex.Message}");
                }

                foreach (var guid in removeSoonGuid)
                {
                    director.Document.Objects.Delete(guid, true);
                }
            }

            director.Document.UndoRecordingEnabled = prevRecordingState;

            return screenshots;
        }

        private static string GenerateBuildingBlockOnBoneImageString(CMFImplantDirector director,
            CasePreferenceDataModel casePrefData, IBB mainImplantEibb,
            params IBB[] additionalImplantEibbs)
        {
            return GenerateBuildingBlockOnBoneImageString(director, casePrefData, false, mainImplantEibb,
                additionalImplantEibbs);
        }

        private static string GenerateBuildingBlockOnBoneImageString(CMFImplantDirector director, CasePreferenceDataModel casePrefData, bool forceHideBone, IBB mainImplantEibb, params IBB[] additionalImplantEibbs)
        {
            var objectManager = new CMFObjectManager(director);
            var geometry = GetImplantMeshBasedOnCasePreferenceDataModel(
                objectManager, mainImplantEibb, casePrefData, out var buildingBlock);
            if (geometry == null)
            {
                return string.Empty;
            }

            var showPaths = forceHideBone ? new List<string>() : ScreenshotsUtilities.GetBonesPaths(ProPlanImport.PlannedLayer);
            ShowLayers(objectManager, showPaths, buildingBlock, additionalImplantEibbs);

            var bBox = geometry.GetBoundingBox(true);
            return GenerateImageString(director, casePrefData, bBox);
        }

        private static string GenerateBuildingBlockOnBonesInContactImageString(
            CMFImplantDirector director, 
            CasePreferenceDataModel casePrefenceDataModel, 
            IBB mainImplantEibb, 
            params IBB[] additionalImplantEibbs)
        {

            var objectManager = new CMFObjectManager(director);
            var implantMesh = GetImplantMeshBasedOnCasePreferenceDataModel(
                objectManager, mainImplantEibb, casePrefenceDataModel, out var extendedImplantBuildingBlock);
            if (implantMesh == null)
            {
                return string.Empty;
            }

            var showPaths = GetBonesInContact(director, casePrefenceDataModel);
            ShowLayers(objectManager, showPaths, extendedImplantBuildingBlock, additionalImplantEibbs);

            var boundingBox = implantMesh.GetBoundingBox(true);
            return GenerateImageString(director, casePrefenceDataModel, boundingBox);
        }

        private static Mesh GetImplantMeshBasedOnCasePreferenceDataModel(
            CMFObjectManager objectManager, 
            IBB mainImplantEibb, 
            CasePreferenceDataModel casePrefDataModel,
            out ExtendedImplantBuildingBlock extendedImplantBuildingBlock
            )
        {
            var implantCaseComponent = new ImplantCaseComponent();
            extendedImplantBuildingBlock = implantCaseComponent.GetImplantBuildingBlock(mainImplantEibb, casePrefDataModel);

            var hasBuildingBlock = objectManager.HasBuildingBlock(extendedImplantBuildingBlock);
            if (!hasBuildingBlock)
            {
                return null;
            }

            var implantGeometry = objectManager.GetBuildingBlock(extendedImplantBuildingBlock).Geometry;

            return (Mesh)implantGeometry;
        }

        private static void ShowLayers(
            CMFObjectManager objectManager, 
            List<string> showPaths, 
            ExtendedImplantBuildingBlock showExtendedImplantBuildingBlock,
            params IBB[] showAdditionalImplantEibbs)
        {
            showPaths.Add(showExtendedImplantBuildingBlock.Block.Layer);

            showPaths.AddRange(from additionalBlock in showAdditionalImplantEibbs select showExtendedImplantBuildingBlock
                into additionalImplantEibb where objectManager.HasBuildingBlock(additionalImplantEibb) &&
                                                 objectManager.GetBuildingBlock(additionalImplantEibb).Geometry != null select additionalImplantEibb.Block.Layer);
            
            Core.Visualization.Visibility.SetVisible(objectManager.GetDirector().Document, showPaths);
        }

        private static List<string> GetBonesInContact(
            CMFImplantDirector director,
            CasePreferenceDataModel casePreferenceDataModel)
        {
            var screwAnalysis = new CMFScrewAnalysis(director);
            var connectedBoneScrewsMap = screwAnalysis
                .GroupScrewWithBone(casePreferenceDataModel);
            var connectedBones = connectedBoneScrewsMap.Keys.ToList();
            var connectedBonesLayerIndexes = connectedBones
                .Select(boneConnectedToScrew => boneConnectedToScrew.Attributes.LayerIndex);

            var boneLayerPaths = connectedBonesLayerIndexes.Select(
                connectedBonesLayerIndex =>
                    director.Document.Layers[connectedBonesLayerIndex].FullPath).ToList();
            return boneLayerPaths;
        }

        private static string GenerateImageString(CMFImplantDirector director, CasePreferenceDataModel casePrefData, BoundingBox bBox, bool resize = true)
        {
            if (!bBox.IsValid)
            {
                return string.Empty;
            }

            var averageVector = GetViewDirection(casePrefData);
            var image = ScreenshotsUtilities.GenerateImplantGuideImage(director, bBox, averageVector, resize);
            return image;
        }

        private static Vector3d GetViewDirection(CasePreferenceDataModel casePrefData)
        {
            return GetViewDirection(casePrefData.ImplantDataModel.DotList);
        }

        private static Vector3d GetViewDirection(IEnumerable<IDot> dots)
        {
            var sumVector = new Vector3d(0, 0, 0);
            var pastilleNum = 0;

            foreach (var dot in dots)
            {
                sumVector = Vector3d.Add(sumVector, RhinoVector3dConverter.ToVector3d(dot.Direction));
                ++pastilleNum;
            }

            var averageVector = Vector3d.Divide(sumVector, pastilleNum);
            averageVector.Unitize();

            return averageVector;
        }
    }
}
