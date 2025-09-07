using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.FileSystem;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Query;
using Rhino.DocObjects;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace IDS.CMF.Visualization
{
    public class BoneThicknessAnalysisARTScreenshotsCreator
    {
        private readonly CMFImplantDirector _director;
        private readonly CMFObjectManager _objectManager;
        private readonly QcDocBoneThicknessMapQuery _boneThicknessMapQuery;
        private readonly string _dirART;

        public BoneThicknessAnalysisARTScreenshotsCreator(CMFImplantDirector director, 
            QcDocBoneThicknessMapQuery boneThicknessMapQuery,
            string dirART)
        {
            _director = director;
            _objectManager = new CMFObjectManager(director);
            _boneThicknessMapQuery = boneThicknessMapQuery;
            _dirART = dirART;
        }

        public void ExportScreenshotsOnAllImplantCase()
        {
            var groupedBonesScrewsData = new Dictionary<string, Dictionary<RhinoObject, List<Screw>>>();
            // Group Data
            foreach (var casePreferenceDataModel in _director.CasePrefManager.CasePreferences)
            {
                if (string.Equals(casePreferenceDataModel.CasePrefData.ImplantTypeValue,
                        BoneThicknessAnalysisForART.LefortImplantTypeName, 
                        StringComparison.CurrentCultureIgnoreCase))
                {
                    // not necessary implant name, in future it might be multiple implant type group together, ex: Lefort + Zygoma
                    if (!groupedBonesScrewsData.ContainsKey(BoneThicknessAnalysisForART.LefortImplantTypeName))
                    {
                        groupedBonesScrewsData.Add(BoneThicknessAnalysisForART.LefortImplantTypeName, new Dictionary<RhinoObject, List<Screw>>());
                    }

                    var newBonesScrewsData = _boneThicknessMapQuery.GetGroupScrewWithBone(casePreferenceDataModel);
                    var existingBonesScrewsData =
                        groupedBonesScrewsData[BoneThicknessAnalysisForART.LefortImplantTypeName];

                    foreach (var bonesScrewsData in newBonesScrewsData)
                    {
                        var bone = bonesScrewsData.Key;
                        var screws = bonesScrewsData.Value;
                        if (!existingBonesScrewsData.ContainsKey(bone))
                        {
                            existingBonesScrewsData.Add(bone, new List<Screw>());
                        }
                        existingBonesScrewsData[bone].AddRange(screws);
                    }
                }
            }

            // Perform screenshots
            foreach (var singleBonesScrewsData in groupedBonesScrewsData)
            {
                var groupedKey = singleBonesScrewsData.Key;
                var bonesScrewsData = singleBonesScrewsData.Value;
                // Using implant name for now, in future if we have Zygoma + Lefort, we need to think a new name
                if (string.Equals(groupedKey,
                        BoneThicknessAnalysisForART.LefortImplantTypeName,
                        StringComparison.CurrentCultureIgnoreCase))
                {
                    ScreenshotsForLefort(bonesScrewsData);
                }
            }
        }

        private void ExportImageToJpeg(string filePath, Image image)
        {
            // It will thrown exception if direct export from image instead of bitmap
            var bitmap = new Bitmap(image);
            bitmap.Save(filePath, ImageFormat.Jpeg);
        }

        private void ScreenshotsForLefort(Dictionary<RhinoObject, List<Screw>> allBonesScrewsData)
        {
            var skullRemainingScreenshots = GenerateARTBoneThicknessImageString(allBonesScrewsData, 
                BoneThicknessAnalysisForART.SkullRemainingSubLayerName);

            var maxillaScreenshots = GenerateARTBoneThicknessImageString(allBonesScrewsData,
                BoneThicknessAnalysisForART.MaxillaSubLayerName);

            var resource = new CMFResources();
            var scaleFileName = resource.BoneThicknessAnalysisScaleForARTFileName;
            var scaleFilePath = resource.BoneThicknessAnalysisScaleForARTFilePath;
            File.Copy(scaleFilePath, Path.Combine(_dirART, scaleFileName));

            if(skullRemainingScreenshots != null)
            {
                ExportImageToJpeg(Path.Combine(_dirART, BoneThicknessAnalysisForART.SkullRemainLeftViewFileName), skullRemainingScreenshots.LeftView);
                ExportImageToJpeg(Path.Combine(_dirART, BoneThicknessAnalysisForART.SkullRemainRightViewFileName), skullRemainingScreenshots.RightView);
            }
            if(maxillaScreenshots != null)
            {
                ExportImageToJpeg(Path.Combine(_dirART, BoneThicknessAnalysisForART.MaxillaLeftViewFileName), maxillaScreenshots.LeftView);
                ExportImageToJpeg(Path.Combine(_dirART, BoneThicknessAnalysisForART.MaxillaRightViewFileName), maxillaScreenshots.RightView);
            }
        }

        private BoneThicknessAnalysisARTComponentScreenshots GenerateARTBoneThicknessImageString(
            Dictionary<RhinoObject, List<Screw>> allBonesScrewsData, string subLayerName)
        {
            var filteredBonesScrewsData = allBonesScrewsData.Where(kv =>
                    string.Equals(_objectManager.FindLayerNameWithRhinoObject(kv.Key),
                        subLayerName,
                        StringComparison.CurrentCultureIgnoreCase))
                .ToList();

            var boneThicknessMeshes = filteredBonesScrewsData
                .Select(kv => _boneThicknessMapQuery.CreateWallThicknessAnalysisMeshForART(
                    kv.Key, 
                    BoneThicknessAnalysisForART.MinThickness, 
                    BoneThicknessAnalysisForART.MaxThickness))
                .ToList();

            var screws = filteredBonesScrewsData
                .SelectMany(kv => kv.Value)
                .Distinct()
                .ToList();

            return ScreenshotsImplant.GenerateARTBoneThicknessImageString(_director, boneThicknessMeshes, screws);
        }
    }
}
