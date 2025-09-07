using IDS.CMF.CasePreferences;
using IDS.CMF.Constants;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Quality;
using IDS.CMF.V2.CasePreferences;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Operations
{
    public class ScrewTypeBackwardsCompatibility
    {
        private readonly CMFImplantDirector _director;
        private readonly CMFObjectManager _objManager;

        private bool _defaultBarrelTypeForObsoleteScrews = false;

        public ScrewTypeBackwardsCompatibility(CMFImplantDirector director)
        {
            _director = director;
            _objManager = new CMFObjectManager(director);

            if (_director.CasePrefManager.SurgeryInformation.ScrewBrand == EScrewBrand.MtlsStandardPlus)
            {
                _director.NeedToUpdateImplantScrewTypeValue = CheckForOutdatedImplantScrews();
                _director.NeedToUpdateGuideScrewTypeValue = CheckForOutdatedGuideScrews();
            }
        }

        
        public void UpdateImplantScrewTypeBackwardCompatibility()
        {
            if (!_director.NeedToUpdateImplantScrewTypeValue)
            {
                return;
            }

            ChangeImplantScrewTypeIfScrewTypeContainsMini();

            foreach (var casePreferenceDataModel in _director.CasePrefManager.CasePreferences)
            {
                if (!casePreferenceDataModel.CasePrefData.NeedsScrewTypeBackwardsCompatibility)
                {
                    continue;
                }

                var implantPreferenceModel = casePreferenceDataModel as ImplantPreferenceModel;
                var currentBarrelType = implantPreferenceModel.SelectedBarrelType;
                implantPreferenceModel.SelectedScrewType = implantPreferenceModel.ScrewTypes.First();

                implantPreferenceModel.SelectedBarrelType = _defaultBarrelTypeForObsoleteScrews ? implantPreferenceModel.BarrelTypes.First() : currentBarrelType;

                var propertyHandler = new PropertyHandler(_director);
                propertyHandler.HandleDotPastilleChanged(implantPreferenceModel);
                propertyHandler.RecalibrateImplantScrews(casePreferenceDataModel,
                    casePreferenceDataModel.CasePrefData.ScrewTypeValue, true, false);

                IDSPluginHelper.WriteLine(LogCategory.Warning, $"{casePreferenceDataModel.CaseName} uses an outdated screw. Screw type, " +
                                                               $"style and pastille diameter are changed to default values");

                casePreferenceDataModel.CasePrefData.NeedsScrewTypeBackwardsCompatibility = false;
            }

            _director.NeedToUpdateImplantScrewTypeValue = false;
        }

        public void UpdateGuideScrewTypeBackwardCompatibility()
        {
            if (!_director.NeedToUpdateGuideScrewTypeValue)
            {
                return;
            }

            foreach (var guidePreferenceDataModel in _director.CasePrefManager.GuidePreferences)
            {
                if (!guidePreferenceDataModel.GuidePrefData.NeedsScrewTypeBackwardsCompatibility)
                {
                    continue;
                }

                var guidePreferenceModel = guidePreferenceDataModel as GuidePreferenceModel;
                guidePreferenceModel.SelectedGuideScrewType = guidePreferenceModel.ScrewGuideTypes.First();

                var propertyHandler = new PropertyHandler(_director);
                propertyHandler.RecalibrateGuideFixationScrews(guidePreferenceModel);
                    
                IDSPluginHelper.WriteLine(LogCategory.Warning, $"Guide Fixation Screw(s) for {guidePreferenceDataModel.CaseName} uses an outdated screw. Guide fixation " +
                                                               $"screw type and style has been changed to default values");
                guidePreferenceDataModel.GuidePrefData.NeedsScrewTypeBackwardsCompatibility = false;
            }

            _director.NeedToUpdateGuideScrewTypeValue = false;
        }

        public void ChangeImplantScrewTypeIfScrewTypeContainsMini()
        {
            if (!_director.NeedToUpdateImplantScrewStyleValue)
            {
                return;
            }

            //case can be using different screw brand
            if (_director.CasePrefManager.SurgeryInformation.ScrewBrand != EScrewBrand.MtlsStandardPlus)
            {
                _director.NeedToUpdateImplantScrewStyleValue = false;
                return;
            }

            var outdatedCasePreferenceDataModels = _director.CasePrefManager.CasePreferences.Where(cp =>
                cp.CasePrefData.ScrewTypeValue.ToLower().Contains("mini")).ToList();

            //case might not be using mini screw type
            if (!outdatedCasePreferenceDataModels.Any())
            {
                _director.NeedToUpdateImplantScrewStyleValue = false;
                return;
            }

            var outdatedCaseWithSelfDrillingScrew =
                outdatedCasePreferenceDataModels.Where(cp => cp.CasePrefData.ScrewStyle == "Self-Drilling").ToList();
            var outdatedCaseWithSelfTappingScrew =
                outdatedCasePreferenceDataModels.Where(cp => cp.CasePrefData.ScrewStyle == "Self-Tapping").ToList();

            var obsoletedSelfDrillingScrewTypesAndNewScrewTypesMap =
                GetObsoletedSelfDrillingScrewTypesAndNewScrewTypesMap();
            var obsoletedSelfTappingScrewTypesAndNewScrewTypesMap =
                GetObsoletedSelfTappingScrewTypesAndNewScrewTypesMap();

            var guideSupport = GetGuideSupport(out var hasGuideSupport);
            var allSkippedLevelingScrewBarrels = new List<Screw>();
            var guidePrefDataModels = _director.CasePrefManager.GuidePreferences;

            var outdatedGuidePrefDataModels = HandleOutdatedCasePreferenceDataModels(outdatedCaseWithSelfDrillingScrew,
                obsoletedSelfDrillingScrewTypesAndNewScrewTypesMap, guideSupport, hasGuideSupport, guidePrefDataModels,
                allSkippedLevelingScrewBarrels);

            outdatedGuidePrefDataModels.AddRange(HandleOutdatedCasePreferenceDataModels(outdatedCaseWithSelfTappingScrew,
                obsoletedSelfTappingScrewTypesAndNewScrewTypesMap, guideSupport, hasGuideSupport, guidePrefDataModels,
                allSkippedLevelingScrewBarrels));

            TriggerRegisteredBarrelsChangedInvalidation(outdatedGuidePrefDataModels);

            if (allSkippedLevelingScrewBarrels.Any())
            {
                BarrelLevelingErrorReporter.ReportGuideBarrelLevelingError(guideSupport,
                    allSkippedLevelingScrewBarrels);
            }

            _director.NeedToUpdateImplantScrewStyleValue = false;
        }

        private List<string> GetObsoletedMicroAndMiniSlottedImplantScrewTypes()
        {
            return new List<string>
            {
                ObsoletedScrewStyle.MicroSlotted,
                ObsoletedScrewStyle.MiniSlottedSelfTapping,
                ObsoletedScrewStyle.MiniSlottedSelfDrilling
            };
        }

        private List<string> GetObsoletedMicroAndMiniSlottedGuideScrewTypes()
        {
            return new List<string>
            {
                ObsoletedScrewStyle.MicroSlotted,
                ObsoletedScrewStyle.MiniSlotted,
            };
        }

        private Dictionary<string, string> GetObsoletedSelfDrillingScrewTypesAndNewScrewTypesMap()
        {
            return new Dictionary<string, string>
            {
                {ObsoletedScrewStyle.MiniCrossed, ReplacementForObsoletedScrewStyle.MiniCrossedSelfDrillingBarrel},
                {ObsoletedScrewStyle.MiniSlotted, ReplacementForObsoletedScrewStyle.MiniSlottedSelfDrillingBarrel},
                {ObsoletedScrewStyle.MiniCrossedHexBarrel, ReplacementForObsoletedScrewStyle.MiniCrossedSelfDrillingHexBarrel},
                {ObsoletedScrewStyle.MiniSlottedHexBarrel, ReplacementForObsoletedScrewStyle.MiniSlottedSelfDrillingHexBarrel}
            };
        }

        private Dictionary<string, string> GetObsoletedSelfTappingScrewTypesAndNewScrewTypesMap()
        {
            return new Dictionary<string, string>
            {
                {ObsoletedScrewStyle.MiniCrossed, ReplacementForObsoletedScrewStyle.MiniCrossedSelfTappingBarrel},
                {ObsoletedScrewStyle.MiniSlotted, ReplacementForObsoletedScrewStyle.MiniSlottedSelfTappingBarrel},
                {ObsoletedScrewStyle.MiniCrossedHexBarrel, ReplacementForObsoletedScrewStyle.MiniCrossedSelfTappingHexBarrel},
                {ObsoletedScrewStyle.MiniSlottedHexBarrel, ReplacementForObsoletedScrewStyle.MiniSlottedSelfTappingHexBarrel}
            };
        }

        private List<GuidePreferenceDataModel> HandleOutdatedCasePreferenceDataModels(
            List<CasePreferenceDataModel> casePreferenceDataModels,
            Dictionary<string, string> screwTypesAndNewScrewTypesMap,
            Mesh guideSupport,
            bool hasGuideSupport,
            IEnumerable<GuidePreferenceDataModel> guidePrefDataModels,
            List<Screw> allSkippedLevelingScrewBarrels)
        {
            var outdatedGuidePrefDataModels = new List<GuidePreferenceDataModel>();
            foreach (var casePreferenceDataModel in casePreferenceDataModels)
            {
                var implantPreferenceModel = casePreferenceDataModel as ImplantPreferenceModel;
                var currentScrewType = implantPreferenceModel.SelectedScrewType;
                var currentBarrelType = implantPreferenceModel.SelectedBarrelType;

                if (!screwTypesAndNewScrewTypesMap.TryGetValue(currentScrewType, out var newScrewType))
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, $"Cannot map outdated {currentScrewType} to new screw type. " +
                                                                 "Please choose a new screw type.");
                    continue;
                }

                implantPreferenceModel.SelectedScrewType = newScrewType;
                implantPreferenceModel.SelectedScrewLength = implantPreferenceModel.PreviouslySelectedScrewLength;
                implantPreferenceModel.SelectedBarrelType = currentBarrelType;
                implantPreferenceModel.CasePrefData.NeedsScrewTypeBackwardsCompatibility = false;

                if (!hasGuideSupport)
                {
                    continue;
                }

                var guidOfUpdatedScrews = UpdateScrewBarrel(implantPreferenceModel, guideSupport,
                    out var skippedLevelingScrewBarrels);
                allSkippedLevelingScrewBarrels.AddRange(skippedLevelingScrewBarrels);
                outdatedGuidePrefDataModels.AddRange(guidePrefDataModels.Where(gp => gp.LinkedImplantScrews.Intersect(guidOfUpdatedScrews).Any()));
            }

            return outdatedGuidePrefDataModels;
        }

        private Mesh GetGuideSupport(out bool hasGuideSupport)
        {
            hasGuideSupport = _objManager.HasBuildingBlock(IBB.GuideSupport);
            Mesh guideSupport = null;
            if (hasGuideSupport)
            {
                guideSupport = (Mesh)_objManager.GetBuildingBlock(IBB.GuideSupport).Geometry;
            }

            return guideSupport;
        }

        private List<Guid> UpdateScrewBarrel(ImplantPreferenceModel caseData, Mesh guideSupport, out List<Screw> skippedLevelingScrewBarrels)
        {
            var implantComponent = new ImplantCaseComponent();
            var screwBuildingBlock = implantComponent.GetImplantBuildingBlock(IBB.Screw, caseData);
            var screwRhinoObjects = _objManager.GetAllBuildingBlocks(screwBuildingBlock);

            skippedLevelingScrewBarrels = new List<Screw>();
            var guidOfUpdatedScrews = new List<Guid>();

            foreach (var screwRhinoObject in screwRhinoObjects)
            {
                var screwBarrelRegistration = new CMFBarrelRegistrator(_director);

                var screw = (Screw)screwRhinoObject;
                var screwType = caseData.SelectedScrewType;
                var barrelType = caseData.SelectedBarrelType;
                screw.Attributes.UserDictionary.Set(screw.KeyScrewType, screwType);
                screw.ScrewType = screwType;
                screw.BarrelType = barrelType;
                screwBarrelRegistration.RegisterSingleScrewBarrel(screw, guideSupport, out var isBarrelLevelingSkipped);
                screwBarrelRegistration.Dispose();

                if (isBarrelLevelingSkipped)
                {
                    skippedLevelingScrewBarrels.Add(screw);
                }

                guidOfUpdatedScrews.Add(screw.Id);
            }

            return guidOfUpdatedScrews;
        }

        private void TriggerRegisteredBarrelsChangedInvalidation(List<GuidePreferenceDataModel> outdatedGuidePrefDataModels)
        {
            if (!outdatedGuidePrefDataModels.Any())
            {
                return;
            }

            outdatedGuidePrefDataModels = outdatedGuidePrefDataModels.Distinct().ToList();
            outdatedGuidePrefDataModels.ForEach(gp =>
            {
                gp.Graph.InvalidateGraph();
                gp.Graph.NotifyBuildingBlockHasChanged(new[] {IBB.RegisteredBarrel });
                IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"Guide {gp.CaseName} - RegisteredBarrel changed");
            });
        }

        private bool CheckForOutdatedImplantScrews()
        {
            var hasObsoleteScrew = false;
            foreach (var casePreferenceDataModel in _director.CasePrefManager.CasePreferences)
            {
                var implantPreferenceModel = casePreferenceDataModel as ImplantPreferenceModel;
                var obsoleteSlottedScrew = GetObsoletedMicroAndMiniSlottedImplantScrewTypes();

                if (obsoleteSlottedScrew.Contains(implantPreferenceModel.SelectedScrewType))
                {
                    _defaultBarrelTypeForObsoleteScrews = true;
                    implantPreferenceModel.CasePrefData.NeedsScrewTypeBackwardsCompatibility = true;
                    hasObsoleteScrew = true;
                }
            }

            return hasObsoleteScrew;
        }

        private bool CheckForOutdatedGuideScrews()
        {
            var hasObsoleteScrew = false;
            foreach (var guidePreferenceDataModel in _director.CasePrefManager.GuidePreferences)
            {
                var guidePreferenceModel = guidePreferenceDataModel as GuidePreferenceModel;
                var obsoleteSlottedScrew = GetObsoletedMicroAndMiniSlottedGuideScrewTypes();

                if (obsoleteSlottedScrew.Contains(guidePreferenceModel.SelectedGuideScrewType))
                {
                    guidePreferenceDataModel.GuidePrefData.NeedsScrewTypeBackwardsCompatibility = true;
                    hasObsoleteScrew = true;
                }
            }

            return hasObsoleteScrew;
        }
    }
}
