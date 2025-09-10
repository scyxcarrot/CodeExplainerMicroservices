using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Preferences;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace IDS.PICMF.Helper
{
    public class GuideCreationCommandHelper
    {
        private readonly CMFImplantDirector _director;

        public GuideCreationCommandHelper(CMFImplantDirector director)
        {
            this._director = director;
        }

        public class GuideFixationScrewRelevelingParams : IDisposable
        {
            public Screw GuideScrew { get; set; }
            public Screw ReleveledScrew { get; set; }
            public Screw StandardizeHeadPointScrew { get; set; }
            public bool HasContraintSurface { get; set; }
            
            public void Dispose()
            {
                GuideScrew?.Dispose();
                ReleveledScrew?.Dispose();
                StandardizeHeadPointScrew?.Dispose();
            }
        }

        public class GuidePreviewCreationParams : IDisposable
        {
            public GuidePreferenceDataModel CaseData { get; private set; }
            public GuideCreatorHelper.CreateGuideParameters GuideCreationParams { get; private set; }
            public bool RequiresRegeneration { get; set; }
            public List<GuideFixationScrewRelevelingParams> GuideFixationScrewRelevelingParamsList { get; private set; }

            public static GuidePreviewCreationParams Generate(CMFImplantDirector director, GuidePreferenceDataModel guidePreferenceDataModel)
            {
                var guideCreationParams = new GuideCreatorHelper.CreateGuideParameters(director, guidePreferenceDataModel);
                var guidePreviewCreationParams = new GuidePreviewCreationParams
                {
                    CaseData = guidePreferenceDataModel,
                    GuideCreationParams = guideCreationParams,
                    GuideFixationScrewRelevelingParamsList = new List<GuideFixationScrewRelevelingParams>()
                };

                var guideComponent = new GuideCaseComponent();
                var objectManager = new CMFObjectManager(director);

                var guideScrewEibb = guideComponent.GetGuideBuildingBlock(IBB.GuideFixationScrew, guidePreferenceDataModel);
                var guideScrews = objectManager.GetAllBuildingBlocks(guideScrewEibb).Select(x => (Screw)x).ToList();
                foreach (var screw in guideScrews)
                {
                    guidePreviewCreationParams.GuideFixationScrewRelevelingParamsList.Add(new GuideFixationScrewRelevelingParams
                    {
                        GuideScrew = screw,
                        HasContraintSurface = false
                    });
                }

                return guidePreviewCreationParams;
            }

            public void Dispose()
            {
                GuideCreationParams?.Dispose();
                GuideFixationScrewRelevelingParamsList?.ForEach(x => x?.Dispose());
            }
        }

        public List<GuidePreviewCreationParams> GetGuidePreviewCreationParams(ref Dictionary<string, TimeSpan> guidesAndProcessingTimes)
        {
            var guidePreviewCreationParamsList = new List<GuidePreviewCreationParams>();

            foreach (var guidePreferenceDataModel in _director.CasePrefManager.GuidePreferences)
            {
                AddGuidePreviewParams(guidePreferenceDataModel, ref guidePreviewCreationParamsList);
            }

            var sharedGuideFixationScrewsList = new List<List<Guid>>();

            AddSharedScrewParams(ref sharedGuideFixationScrewsList, ref guidePreviewCreationParamsList);

            GenerateGuideBaseAndSetParams(ref guidePreviewCreationParamsList, ref guidesAndProcessingTimes);

            var objectManager = new CMFObjectManager(_director);
            var guideSupport = (Mesh)objectManager.GetBuildingBlock(IBB.GuideSupport).Geometry.Duplicate();

            RelevelGuideFixationScrews(guideSupport, ref guidePreviewCreationParamsList, ref guidesAndProcessingTimes);

            StandardizeSharedScrewsReleveling(guideSupport, sharedGuideFixationScrewsList, ref guidePreviewCreationParamsList);

            ReplaceReleveledGuideScrews(ref guidePreviewCreationParamsList, ref guidesAndProcessingTimes);

            return guidePreviewCreationParamsList;
        }

        private void AddGuidePreviewParams(GuidePreferenceDataModel guidePreferenceDataModel, ref List<GuidePreviewCreationParams> guidePreviewCreationParamsList)
        {
            var objectManager = new CMFObjectManager(_director);

            var guidePreviewEibb = GetGuidePreviewEBlock(guidePreferenceDataModel);
            if (!objectManager.HasBuildingBlock(guidePreviewEibb))
            {
                var guidePreviewCreationParams = GuidePreviewCreationParams.Generate(_director, guidePreferenceDataModel); 
                guidePreviewCreationParamsList.Add(guidePreviewCreationParams);
            }
        }

        private void AddSharedScrewParams(ref List<List<Guid>> sharedGuideFixationScrewsList, ref List<GuidePreviewCreationParams> guidePreviewCreationParamsList)
        {
            var guidePreviewRegenerationParamsList = new List<GuidePreviewCreationParams>();

            foreach (var guidePreviewCreationParams in guidePreviewCreationParamsList)
            {
                foreach (var guideFixationScrewRelevelingParam in guidePreviewCreationParams.GuideFixationScrewRelevelingParamsList)
                {
                    var guideScrew = guideFixationScrewRelevelingParam.GuideScrew;

                    var guideAndScrewItSharedWith = guideScrew.GetGuideAndScrewItSharedWith();
                    foreach (var keyPairValue in guideAndScrewItSharedWith)
                    {
                        var guidePreviewRegenerationParams = guidePreviewCreationParamsList.FirstOrDefault(p => p.CaseData == keyPairValue.Key);
                        if (guidePreviewRegenerationParams == null)
                        {
                            guidePreviewRegenerationParams = guidePreviewRegenerationParamsList.FirstOrDefault(p => p.CaseData == keyPairValue.Key);
                        }

                        if (guidePreviewRegenerationParams == null)
                        {
                            guidePreviewRegenerationParams = GuidePreviewCreationParams.Generate(_director, keyPairValue.Key);
                            guidePreviewRegenerationParamsList.Add(guidePreviewRegenerationParams);
                        }
                    }

                    if (guideAndScrewItSharedWith.Any() && !sharedGuideFixationScrewsList.Any(l => l.Contains(guideScrew.Id)))
                    {
                        var sharedScrewIds = guideAndScrewItSharedWith.Select(s => s.Value.Id).ToList();
                        if (!sharedScrewIds.Contains(guideScrew.Id))
                        {
                            sharedScrewIds.Add(guideScrew.Id);
                        }
                        sharedGuideFixationScrewsList.Add(sharedScrewIds);
                    }
                }
            }

            if (guidePreviewRegenerationParamsList.Any())
            {
                guidePreviewCreationParamsList.AddRange(guidePreviewRegenerationParamsList);
            }
        }

        private void GenerateGuideBaseAndSetParams(ref List<GuidePreviewCreationParams> guidePreviewCreationParamsList, ref Dictionary<string, TimeSpan> guidesAndProcessingTimes)
        {
            var parameter = CMFPreferences.GetActualGuideParameters();

            foreach (var guidePreviewCreationParams in guidePreviewCreationParamsList)
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                var guideCreationParams = guidePreviewCreationParams.GuideCreationParams;

                var guideSurfaces = guideCreationParams.GuideSurfaces;
                if (!guideSurfaces.Any())
                {
                    stopwatch.Stop();
                    continue;
                }

                guideSurfaces = guideCreationParams.GuideSurfacesSmoothed;
                if (guideSurfaces == null || !guideSurfaces.Any())
                {
                    stopwatch.Stop();
                    continue;
                }
                
                var guideBase = guideCreationParams.GenerateGuideBaseSurface(MeshUtilities.AppendMeshes(guideSurfaces), parameter);
                guideCreationParams.GuideBase = guideBase;
                guideCreationParams.GenerateGuideBase = false;

                stopwatch.Stop();
                var timeTaken = stopwatch.Elapsed;
                AddProcessingTime(GetGuideNameForReporting(guidePreviewCreationParams.CaseData), timeTaken, ref guidesAndProcessingTimes);
            }
        }

        private void RelevelGuideFixationScrews(Mesh guideSupport, ref List<GuidePreviewCreationParams> guidePreviewCreationParamsList, ref Dictionary<string, TimeSpan> guidesAndProcessingTimes)
        {
            var calibrator = new GuideFixationScrewCalibrator();

            foreach (var guidePreviewCreationParam in guidePreviewCreationParamsList)
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                var guideBaseSurface = guidePreviewCreationParam.GuideCreationParams.GuideBase;

                var relevelFailed = false;

                foreach (var screwRelevelingParam in guidePreviewCreationParam.GuideFixationScrewRelevelingParamsList)
                {
                    var screw = screwRelevelingParam.GuideScrew;
                    screwRelevelingParam.ReleveledScrew = screw;
                    screwRelevelingParam.StandardizeHeadPointScrew = screw;

                    if (guideBaseSurface == null)
                    {
                        continue;
                    }

                    screwRelevelingParam.HasContraintSurface = true;
                    var relevelScrew = calibrator.RelevelScrewOnSurface(screw, guideBaseSurface, guideSupport, screw);
                    if (relevelScrew == null)
                    {
                        relevelFailed = true;
                    }
                    else if (relevelScrew != null && relevelScrew.HeadPoint != screw.HeadPoint)
                    {
                        screwRelevelingParam.ReleveledScrew = relevelScrew;
                        screwRelevelingParam.StandardizeHeadPointScrew = relevelScrew;
                    }
                }

                stopwatch.Stop();
                var timeTaken = stopwatch.Elapsed;
                var guideName = GetGuideNameForReporting(guidePreviewCreationParam.CaseData);
                AddProcessingTime(guideName, timeTaken, ref guidesAndProcessingTimes);

                if (relevelFailed)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"There are guide fixation screw(s) failed to relevel for {guideName}");
                }
            }
        }

        private void StandardizeSharedScrewsReleveling(Mesh guideSupport, List<List<Guid>> sharedGuideFixationScrewsList, ref List<GuidePreviewCreationParams> guidePreviewCreationParamsList)
        {
            foreach (var sharedGuideFixationScrews in sharedGuideFixationScrewsList)
            {
                var sharedGuideFixationScrewRelevelingParams = guidePreviewCreationParamsList.SelectMany(p => p.GuideFixationScrewRelevelingParamsList).Where(s => sharedGuideFixationScrews.Contains(s.GuideScrew.Id)).ToList();
                if (sharedGuideFixationScrewRelevelingParams.All(p => !p.HasContraintSurface))
                {
                    continue;
                }

                var refScrew = sharedGuideFixationScrewRelevelingParams[0].GuideScrew;
                var pointOnSupport = PointUtilities.GetRayIntersection(guideSupport, refScrew.HeadPoint, refScrew.Direction);
                var newHeadPoint = pointOnSupport;

                var highestLevelingLength = double.MinValue;
                foreach (var sharedGuideFixationScrewReleveling in sharedGuideFixationScrewRelevelingParams)
                {
                    if (!sharedGuideFixationScrewReleveling.HasContraintSurface)
                    {
                        continue;
                    }

                    var length = (sharedGuideFixationScrewReleveling.ReleveledScrew.HeadPoint - pointOnSupport).Length;
                    if (length > highestLevelingLength)
                    {
                        newHeadPoint = sharedGuideFixationScrewReleveling.ReleveledScrew.HeadPoint;
                        highestLevelingLength = length;
                    }
                }

                foreach (var sharedGuideFixationScrewReleveling in sharedGuideFixationScrewRelevelingParams)
                {
                    if (sharedGuideFixationScrewReleveling.StandardizeHeadPointScrew.HeadPoint != newHeadPoint)
                    {
                        var originalScrew = sharedGuideFixationScrewReleveling.GuideScrew;

                        var newTipPoint = newHeadPoint + originalScrew.Direction * originalScrew.Length;

                        var newScrew = new Screw(originalScrew.Director, newHeadPoint, newTipPoint, originalScrew.ScrewAideDictionary, originalScrew.Index, originalScrew.ScrewType);
                        sharedGuideFixationScrewReleveling.StandardizeHeadPointScrew = newScrew;
                    }
                }
            }
        }

        private void ReplaceReleveledGuideScrews(ref List<GuidePreviewCreationParams> guidePreviewCreationParamsList, ref Dictionary<string, TimeSpan> guidesAndProcessingTimes)
        {
            var objectManager = new CMFObjectManager(_director);
            var guideComponent = new GuideCaseComponent();
            var screwManager = new ScrewManager(_director);

            foreach (var guidePreviewCreationParams in guidePreviewCreationParamsList)
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                var guidePreferenceDataModel = guidePreviewCreationParams.CaseData;
                var guideName = GetGuideNameForReporting(guidePreferenceDataModel);

                var guideScrewEibb = guideComponent.GetGuideBuildingBlock(IBB.GuideFixationScrew, guidePreferenceDataModel);
                var guideScrewIds = objectManager.GetAllBuildingBlockIds(guideScrewEibb).ToList();

                var releveled = false;
                var screwRelevelDetails = string.Empty;
                
                var guideFixationScrewRelevelingParams = guidePreviewCreationParams.GuideFixationScrewRelevelingParamsList;

                guideScrewIds.ForEach(id =>
                {
                    var guideFixationScrewRelevelingParam = guideFixationScrewRelevelingParams.First(s => s.GuideScrew.Id == id);
                    var screwInDoc = guideFixationScrewRelevelingParam.GuideScrew;
                    var finalizedScrew = guideFixationScrewRelevelingParam.StandardizeHeadPointScrew;
                    if (finalizedScrew != null && finalizedScrew.HeadPoint != screwInDoc.HeadPoint)
                    {
                        releveled = true;

                        var levelDirection = finalizedScrew.HeadPoint - screwInDoc.HeadPoint;
                        var levelDown = levelDirection.IsParallelTo(screwInDoc.Direction) == 1;
                        var identification = string.Empty;
#if (INTERNAL)
                        identification = $" {screwInDoc.Id}";
#endif
                        screwRelevelDetails += $"\nScrew{identification} being releveled {(levelDown ? "down" : "up")} to {levelDirection.Length}mm.";
                        var releveledScrew = guideFixationScrewRelevelingParam.ReleveledScrew;
                        if (releveledScrew != null && finalizedScrew.HeadPoint != releveledScrew.HeadPoint)
                        {
                            screwRelevelDetails += " Releveling value was standardized.";
#if (INTERNAL)
                            var releveledRef = releveledScrew.GetScrewEyeRef();
                            var finalizedRef = finalizedScrew.GetScrewEyeRef();

                            var screwLabelTagHelper = new ScrewLabelTagHelper(_director);
                            var angle = screwLabelTagHelper.GetLabelTagAngle(screwInDoc);
                            if (!double.IsNaN(angle))
                            {
                                releveledRef = screwLabelTagHelper.GetLabelTagRef(releveledScrew, angle);
                                finalizedRef = screwLabelTagHelper.GetLabelTagRef(finalizedScrew, angle);
                            }

                            var layerName = $"Test-G{guidePreferenceDataModel.NCase}-{screwInDoc.Id}";
                            IDS.Core.NonProduction.InternalUtilities.AddObject(releveledRef, "ReleveledScrew-Ref", layerName);
                            IDS.Core.NonProduction.InternalUtilities.AddObject(releveledScrew.GetScrewContainer(), "ReleveledScrew-Container", layerName);
                            IDS.Core.NonProduction.InternalUtilities.AddObject(finalizedRef, "StandardizeHeadPointScrew-Ref", layerName);
                            IDS.Core.NonProduction.InternalUtilities.AddObject(finalizedScrew.GetScrewContainer(), "StandardizeHeadPointScrew-Container", layerName);
#endif
                        }
                        
                        screwManager.ReplaceExistingScrewInDocument(finalizedScrew, ref screwInDoc, guidePreferenceDataModel, false);
                        finalizedScrew.UpdateAidesInDocument();
                    }
                });

                if (releveled)
                {
                    guidePreviewCreationParams.RequiresRegeneration = true;
                    IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"There are guide fixation screw(s) being releveled for {guideName}:{screwRelevelDetails}");
                }

                stopwatch.Stop();
                var timeTaken = stopwatch.Elapsed;
                AddProcessingTime(guideName, timeTaken, ref guidesAndProcessingTimes);
            }
        }

        public static ExtendedImplantBuildingBlock GetGuidePreviewEBlock(ICaseData data)
        {
            var guideComponent = new GuideCaseComponent();
            var guidePreviewEibb = guideComponent.GetGuideBuildingBlock(IBB.GuidePreviewSmoothen, data);
            return guidePreviewEibb;
        }

        public static string GetGuideNameForReporting(ICaseData data)
        {
            return $"Smooth {data.CaseName}";
        }

        public static TimeSpan AddProcessingTime(string guideName, TimeSpan additionalTime, ref Dictionary<string, TimeSpan> guidesAndProcessingTimes)
        {
            var currentTotalTime = additionalTime;

            if (guidesAndProcessingTimes.ContainsKey(guideName))
            {
                var preprocessTime = guidesAndProcessingTimes[guideName];
                currentTotalTime += preprocessTime;
                guidesAndProcessingTimes[guideName] = currentTotalTime;
            }
            else
            {
                guidesAndProcessingTimes.Add(guideName, currentTotalTime);
            }

            return currentTotalTime;
        }
    }
}
