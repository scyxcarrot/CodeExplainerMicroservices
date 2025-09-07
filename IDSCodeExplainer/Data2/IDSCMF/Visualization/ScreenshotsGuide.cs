using IDS.CMF.CasePreferences;
using IDS.CMF.Constants;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Visualization
{
    internal static class ScreenshotsGuide
    {
        /// <summary>
        /// Static function to generate string for image
        /// </summary>
        /// <param name="director"></param>
        /// <param name="guidePrefData">guide preference datamodel to know which guide to export</param>
        /// <returns>image string</returns>
        public static string GenerateGuidePreviewSmoothenOnBoneImageString(CMFImplantDirector director, GuidePreferenceDataModel guidePrefData)
        {
            return GenerateBuildingBlockOnBoneImageString(director, guidePrefData, IBB.GuidePreviewSmoothen);
        }

        /// <summary>
        /// Static function to generate image string but with screws
        /// </summary>
        /// <param name="director"></param>
        /// <param name="guidePrefData">guide preference datamodel to know which guide to export</param>
        /// <returns>list of image strings</returns>
        public static List<string> GenerateGuidePreviewSmoothenWithScrewsImagesString(CMFImplantDirector director, GuidePreferenceDataModel guidePrefData, List<CameraView> views)
        {
            return GenerateBuildingBlockWithScrewsImagesString(director, guidePrefData, views, IBB.GuidePreviewSmoothen);
        }

        public static string GenerateActualGuideOnBoneImageString(CMFImplantDirector director, GuidePreferenceDataModel guidePrefData)
        {
            return GenerateBuildingBlockOnBoneImageString(director, guidePrefData, IBB.ActualGuide);
        }

        public static List<string> GenerateActualGuideWithScrewsImagesString(CMFImplantDirector director, GuidePreferenceDataModel guidePrefData, List<CameraView> views)
        {
            return GenerateBuildingBlockWithScrewsImagesString(director, guidePrefData, views, IBB.ActualGuide);
        }

        public static List<string> GenerateMeshDistanceImagesString(CMFImplantDirector director, GuidePreferenceDataModel guidePrefData, Mesh coloredMesh, List<CameraView> views)
        {
            if (coloredMesh == null)
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

            var guid = doc.Objects.AddMesh(coloredMesh);

            var bBox = coloredMesh.GetBoundingBox(true);

            var imagesString = ScreenshotsUtilities.GenerateImplantGuideImages(doc, bBox, views);

            if (currentShadeVertexColors)
            {
                desc.DisplayAttributes.ShadeVertexColors = true;
                doc.Views.ActiveView.ActiveViewport.DisplayMode = desc;
            }

            doc.Objects.Delete(guid, true);

            return imagesString;
        }

        private static string GenerateBuildingBlockOnBoneImageString(CMFImplantDirector director, GuidePreferenceDataModel guidePrefData, IBB block)
        {
            var guideCaseComponent = new GuideCaseComponent();
            var buildingBlock = guideCaseComponent.GetGuideBuildingBlock(block, guidePrefData);

            var objectManager = new CMFObjectManager(director);
            var hasBuildingBlock = objectManager.HasBuildingBlock(buildingBlock);
            if (!hasBuildingBlock)
            {
                return string.Empty;
            }

            var geometry = objectManager.GetBuildingBlock(buildingBlock).Geometry;
            if (geometry == null)
            {
                return string.Empty;
            }

            var showPaths = ScreenshotsUtilities.GetBonesPaths(ProPlanImport.OriginalLayer);
            showPaths.Add(buildingBlock.Block.Layer);
            Core.Visualization.Visibility.SetVisible(director.Document, showPaths);

            var bBox = geometry.GetBoundingBox(true);
            return GenerateImageString(director, guidePrefData, bBox);
        }

        private static List<string> GenerateBuildingBlockImagesString(CMFImplantDirector director, GuidePreferenceDataModel guidePrefData, List<CameraView> views, IBB block)
        {
            var guideCaseComponent = new GuideCaseComponent();
            var buildingBlock = guideCaseComponent.GetGuideBuildingBlock(block, guidePrefData);

            var objectManager = new CMFObjectManager(director);
            var hasBuildingBlock = objectManager.HasBuildingBlock(buildingBlock);
            if (!hasBuildingBlock)
            {
                return ScreenshotsUtilities.GenerateEmptyStrings(views);
            }

            var geometry = objectManager.GetBuildingBlock(buildingBlock).Geometry;
            if (geometry == null)
            {
                return ScreenshotsUtilities.GenerateEmptyStrings(views);
            }

            var doc = director.Document;
            Core.Visualization.Visibility.SetVisible(doc, new List<string>
            {
                buildingBlock.Block.Layer
            });

            var bBox = geometry.GetBoundingBox(true);

            return ScreenshotsUtilities.GenerateImplantGuideImages(doc, bBox, views);
        }

        private static List<string> GenerateBuildingBlockWithScrewsImagesString(CMFImplantDirector director, GuidePreferenceDataModel guidePrefData, List<CameraView> views, IBB block)
        {
            var guideCaseComponent = new GuideCaseComponent();
            var buildingBlock = guideCaseComponent.GetGuideBuildingBlock(block, guidePrefData);
            var screwBuildingBlock = guideCaseComponent.GetGuideBuildingBlock(IBB.GuideFixationScrew, guidePrefData);

            var screwBubbleRadius = 2.0;
            var screwDisplaySize = 2.0;

            return ScreenshotsUtilities.GenerateBuildingBlockWithScrewsImagesString(director, buildingBlock, screwBuildingBlock, screwBubbleRadius, screwDisplaySize, views);
        }

        private static string GenerateImageString(CMFImplantDirector director, GuidePreferenceDataModel guidePrefData, BoundingBox bBox)
        {
            if (!bBox.IsValid)
            {
                return string.Empty;
            }

            var averageVector = GetViewDirection(director, guidePrefData);
            var image = ScreenshotsUtilities.GenerateImplantGuideImage(director, bBox, averageVector);
            return image;
        }

        private static Vector3d GetViewDirection(CMFImplantDirector director, GuidePreferenceDataModel guidePrefData)
        {
            var objectManager = new CMFObjectManager(director);

            var sumVector = new Vector3d(0, 0, 0);
            var faceNormalNum = 0;

            var guideComponent = new GuideCaseComponent();
            var surfaceBuildingBlock = guideComponent.GetGuideBuildingBlock(IBB.GuideSurface, guidePrefData);
            var surfaces = objectManager.GetAllBuildingBlocks(surfaceBuildingBlock.Block).Select(surface => (Mesh)surface.Geometry);

            foreach (var surface in surfaces)
            {
                if (!surface.FaceNormals.Any())
                {
                    surface.FaceNormals.ComputeFaceNormals();
                }

                foreach (var normal in surface.FaceNormals)
                {
                    sumVector = Vector3d.Add(sumVector, normal);
                    ++faceNormalNum;
                }
            }

            var averageVector = Vector3d.Divide(sumVector, faceNormalNum);
            averageVector.Unitize();

            return averageVector;
        }
    }
}