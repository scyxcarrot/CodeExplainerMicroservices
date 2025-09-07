using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Constants;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Query;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.PICMF.Helper;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("59F45925-F3F3-4CD4-9CE7-D4E05CDACE82")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Guide, IBB.RegisteredBarrel)]
    public class CMFOverrideBarrelType : CmfCommandBase
    {
        public CMFOverrideBarrelType()
        {
            TheCommand = this;
        }
        
        public static CMFOverrideBarrelType TheCommand { get; private set; }

        public CMFGuidePrefPanelVisualizationHelper GuidePrefPanelVisualizationHelper { get; } =
            new CMFGuidePrefPanelVisualizationHelper();

        public override string EnglishName => CommandEnglishName.CMFOverrideBarrelType;

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var guideCaseGuid = GuidePreferencesHelper.PromptForPreferenceId();
            if (guideCaseGuid == Guid.Empty)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Guide preference not found!");
                return Result.Failure;
            }

            var objectManager = new CMFObjectManager(director);
            var guidePrefModel = objectManager.GetGuidePreference(guideCaseGuid);

            List<Guid> selectedBarrelGuids;
            selectedBarrelGuids = ScriptedSelectMultipleBarrels(director, guidePrefModel);
            if (!selectedBarrelGuids.Any())
            {
                selectedBarrelGuids = InteractiveSelectMultipleBarrels(director, guidePrefModel);
            }
            
            if (!selectedBarrelGuids.Any())
            {
                return Result.Cancel;
            }

            var selectedScrews = new List<Screw>();
            foreach (var screwGuid in guidePrefModel.LinkedImplantScrews)
            {
                var screw = director.Document.Objects.Find(screwGuid) as Screw;
                var screwbarrelGuid = screw.RegisteredBarrelId;

                if (selectedBarrelGuids.Contains(screwbarrelGuid))
                {
                    selectedScrews.Add(screw);
                }
            }

            var selectableBarrelTypes = GetSelectableBarrelTypes(selectedScrews);
            if (!selectableBarrelTypes.Any())
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, $"Combination of screws have no same barrels to be selected");
                return Result.Failure;
            }

            var defaultBarrel = GetDefaultBarrelType(selectedScrews, selectableBarrelTypes);
            var barrelType = SelectBarrelType(selectableBarrelTypes, defaultBarrel);
            if (string.IsNullOrEmpty(barrelType))
            {
                return Result.Cancel;
            }

            foreach (var screw in selectedScrews)
            {
                if (screw.BarrelType == barrelType)
                {
                    continue;
                }

                //update barrel type
                screw.BarrelType = barrelType;

                // trigger to generate barrel: invalidation of RegisteredBarrel is handled by CMFBarrelRegistrator;
                // if need to be invalidated explicitly, call screw.InvalidateGuideScrewAidesReferencesInDocument();
                Mesh guideSupportMesh = null;
                if (objectManager.HasBuildingBlock(IBB.GuideSupport))
                {
                    guideSupportMesh = (Mesh)objectManager.GetBuildingBlock(IBB.GuideSupport).Geometry;
                }

                var screwBarrelRegistration = new CMFBarrelRegistrator(director);
                bool isBarrelLevelingSkipped;
                var registeredBarrelId = screwBarrelRegistration.RegisterSingleScrewBarrel(screw, guideSupportMesh, out isBarrelLevelingSkipped);
                screwBarrelRegistration.Dispose();
                IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"Registered barrel id: {registeredBarrelId}, BarrelLevelingSkipped: {isBarrelLevelingSkipped}");

                //invalidate dependant parts
                RegisteredBarrelUtilities.NotifyBuildingBlockHasChanged(director, screw.Id);
            }

            var propertyHandler = new PropertyHandler(director);
            propertyHandler.RecheckBarrelTypeExistingScrewQc(selectedScrews);

            doc.Objects.UnselectAll();
            doc.Views.Redraw();

            return Result.Success;
        }

        private List<string> GetSelectableBarrelTypes(List<Screw> selectedScrews)
        {
            var selectedScrewBarrelTypes = selectedScrews.GroupBy(screw => screw.ScrewType)
                .Select(selectedScrewGroup => Queries.GetBarrelTypes(selectedScrewGroup.Key)).ToList();

            if (!selectedScrewBarrelTypes.Any())
            {
                return new List<string>();
            }
            var selectableBarrelTypes = selectedScrewBarrelTypes[0];
            foreach (var selectedScrewBarrelType in selectedScrewBarrelTypes)
            {
                selectableBarrelTypes = selectableBarrelTypes.Intersect(selectedScrewBarrelType).ToList();
            }

            return selectableBarrelTypes;
        }

        private string GetDefaultBarrelType(List<Screw> selectedScrews, List<string> selectableBarrelTypes)
        {
            var barrelTypesDescending = selectedScrews.GroupBy(screw => screw.BarrelType)
                .Select(x => new { x.Key, Count = x.Count() })
                .OrderByDescending(x => x.Count);
            var mostCommonBarrelType = barrelTypesDescending.First().Key;
            var defaultBarrel = mostCommonBarrelType;
            if (!selectableBarrelTypes.Contains(mostCommonBarrelType))
            {
                defaultBarrel = selectableBarrelTypes[0];
            }

            return defaultBarrel;
        }

        private List<Guid> ScriptedSelectMultipleBarrels(CMFImplantDirector director, GuidePreferenceDataModel guidePrefModel)
        {
            var getOptionBarrelIds = new GetOption();
            getOptionBarrelIds.AcceptNothing(false);

            var selectedBarrelIds = new List<Guid>();
            var scriptSelectedBarrelIds = string.Empty;
            getOptionBarrelIds.AddOption("selectedBarrelIds");

            while (true)
            {
                var gres = getOptionBarrelIds.Get();
                if (gres == GetResult.Cancel)
                {
                    break;
                }

                if (gres != GetResult.Option)
                {
                    continue;
                }

                var result = RhinoGet.GetString("selectedBarrelIds", false, ref scriptSelectedBarrelIds);
                if (result == Result.Success && !string.IsNullOrEmpty(scriptSelectedBarrelIds))
                {
                    var selectedBarrelIdsStrArray = scriptSelectedBarrelIds.Split(new string[] { "," }, StringSplitOptions.None);
                    foreach (var selectedBarrelIdsStr in selectedBarrelIdsStrArray)
                    {
                        var success = Guid.TryParse(selectedBarrelIdsStr, out Guid selectedBarrelId);
                        if (success)
                        {
                            selectedBarrelIds.Add(selectedBarrelId);
                        }
                    }
                }
                break;
            }

            if (!selectedBarrelIds.Any())
            {
                RhinoApp.WriteLine($"No barrel Ids Found");
            }

            return selectedBarrelIds;

        }

        private List<Guid> InteractiveSelectMultipleBarrels(CMFImplantDirector director,
            GuidePreferenceDataModel guidePrefModel)
        {
            // Unlock and show barrels
            var doc = director.Document;
            UnlockBarrels(director, guidePrefModel);
            GuidePrefPanelVisualizationHelper.ShowBarrels(director, guidePrefModel);
            // Select barrels
            var selectBarrel = new GetObject();
            selectBarrel.SetCommandPrompt("Select the barrels to override their type");
            selectBarrel.EnablePreSelect(false, false);
            selectBarrel.EnablePostSelect(true);
            selectBarrel.AcceptNothing(true);
            selectBarrel.EnableHighlight(true);
            selectBarrel.EnableTransparentCommands(false);

            while (true)
            {
                var res = selectBarrel.GetMultiple(0, 0);

                if (res == GetResult.Object)
                {
                    var barrels = doc.Objects.GetSelectedObjects(false, false)
                        .Select(o => o.Id).Where(s => s != null).ToList();

                    doc.Objects.UnselectAll();
                    doc.Views.Redraw();

                    return barrels;
                }

                doc.Objects.UnselectAll();
                doc.Views.Redraw();
                return new List<Guid>();
            }
        }

        private string SelectBarrelType(List<string> selectableBarrelTypes, string defaultBarrelType)
        {
            // initialize the default value with an initialbarrelType to avoid passing empty string to BarrelType in screws
            var barrelType = defaultBarrelType.Replace(" ", "_"); ;

            // selectable barrel types must not have underscore otherwise Rhino wont display it
            for (var i=0; i<selectableBarrelTypes.Count(); i++)
            {
                selectableBarrelTypes[i] = selectableBarrelTypes[i].Replace(" ", "_");
            }

            var go = new GetOption();
            go.SetCommandPrompt("Choose Barrel Type.");
            go.AcceptNothing(true);
            go.AddOptionList("BarrelType", selectableBarrelTypes, selectableBarrelTypes.IndexOf(barrelType));

            while (true)
            {
                var res = go.Get();
                if (res == GetResult.Cancel)
                {
                    return string.Empty;
                }

                if (res == GetResult.Option)
                {
                    barrelType = selectableBarrelTypes[go.Option().CurrentListOptionIndex];
                    continue;
                }

                if (res == GetResult.Nothing)
                {
                    break;
                }
            }

            // remove the underscore back
            var spacedBarrelType = barrelType.Replace("_", " ");
            return spacedBarrelType;
        }

        private void UnlockBarrels(CMFImplantDirector director, GuidePreferenceDataModel guidePrefModel)
        {
            var linkedImplantScrewGuids = guidePrefModel.LinkedImplantScrews;
            var linkedScrewBarrelGuids = linkedImplantScrewGuids.Select(id =>
            {
                var implantScrew = director.Document.Objects.Find(id) as Screw;
                
                if (implantScrew != null && implantScrew.RegisteredBarrelId == Guid.Empty)
                {
                    return Guid.Empty;
                }

                var selectedBarrelId = implantScrew.RegisteredBarrelId;
                return selectedBarrelId;
            }).ToList();

            linkedScrewBarrelGuids.ForEach(x => { director.Document.Objects.Unlock(x, true); });
        }
    }
}