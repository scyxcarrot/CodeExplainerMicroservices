using IDS.CMF.CustomMainObjects;
using IDS.CMF.DataModel;
using IDS.CMF.Factory;
using IDS.CMF.Graph;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Quality;
using IDS.CMF.Query;
using IDS.CMF.ScrewQc;
using IDS.CMF.Utilities;
using IDS.CMF.V2.DataModel;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Interface.Implant;
using IDS.RhinoInterfaces.Converter;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.CasePreferences
{
    public class PropertyHandler
    {
        private readonly double _tolerance = 0.0001;
        private readonly CMFImplantDirector _director;

        public PropertyHandler(CMFImplantDirector director)
        {
            _director = director;
        }

        private bool CheckPlateThicknessChanged(ImplantPreferenceModel data)
        {
            return data.ImplantDataModel.ConnectionList.Any(c => Math.Abs(c.Thickness - data.PlateThickness) > _tolerance) ||
                   data.ImplantDataModel.DotList.Where(d => d is DotPastille).Any(d => Math.Abs(((DotPastille) d).Thickness - data.PlateThickness) > _tolerance);
        }

        private void UpdatePlateThicknessChanged(ImplantPreferenceModel data, bool updateEntities)
        {
            data.ImplantDataModel.ConnectionList.ForEach(c => c.Thickness = data.PlateThickness);
            data.ImplantDataModel.DotList.Where(d => d is DotPastille).ToList().ForEach(d => ((DotPastille)d).Thickness = data.PlateThickness);

            if (updateEntities)
            {
                UpdateImplantPlanning(data, true);
                RecalibrateImplantScrews(data, data.SelectedScrewType, false, true);
                ClearUndoRedo();

                RhinoLayerUtilities.DeleteEmptyLayers(_director.Document);
                _director.Document.Views.ActiveView.Redraw();
            }
        }

        public void HandlePlateThicknessChanged(ImplantPreferenceModel data, bool updateEntities = true)
        {
            if (CheckPlateThicknessChanged(data))
            {
                UpdatePlateThicknessChanged(data, updateEntities);
                RecheckMinMaxDistanceInExistingScrewQc(data);
            }
        }

        private void ShowPlateThicknessOutOfSyncWarning(ImplantPreferenceModel data)
        {
            var diffThickness = data.ImplantDataModel.ConnectionList.Where(c =>
                Math.Abs(c.Thickness - data.PlateThickness) > _tolerance).Select(c => c.Thickness).ToList();
            diffThickness.AddRange(data.ImplantDataModel.DotList.Where(d => d is DotPastille).Where(d =>
                Math.Abs(((DotPastille)d).Thickness - data.PlateThickness) > _tolerance).Select(d => ((DotPastille)d).Thickness));
            diffThickness = diffThickness.Distinct().ToList();
            var diffThicknessMessage = string.Join(",", diffThickness.Select(t => t.ToString("F2")));
            
            IDSPluginHelper.WriteLine(LogCategory.Warning, $"Syncing plate thickness during \"{_director.CurrentDesignPhaseName} Phase\" for implant case {data.CaseNumber} due to the plate thickness out of sync, " +
                                                           $"some thickness({diffThicknessMessage}) is not equal to {data.PlateThickness}");
        }

        private void HandlePlateThicknessOutOfSync(ImplantPreferenceModel data)
        {
            if (CheckPlateThicknessChanged(data))
            {
                ShowPlateThicknessOutOfSyncWarning(data);
                UpdatePlateThicknessChanged(data, true);
            }
        }

        private bool CheckPlateWidthChanged(ImplantPreferenceModel data)
        {
            return data.ImplantDataModel.ConnectionList.Where(c => c is ConnectionPlate && c.IsSynchronizable)
                .Any(c => Math.Abs(c.Width - data.PlateWidth) > _tolerance);
        }

        private void UpdatePlateWidthChanged(ImplantPreferenceModel data, bool updateEntities)
        {
            var connections = data.ImplantDataModel.ConnectionList.Where(c => c is ConnectionPlate).ToList();
            var affectedConnections = UpdateWidthChanged(connections, data.PlateWidth);

            if (updateEntities)
            {
                UpdateImplantWidthEntities(data, false, affectedConnections);
            }

            if (data.ImplantDataModel.ConnectionList.Where(c => c is ConnectionPlate).Any(c => !c.IsSynchronizable))
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "There are some plate's width which are out of sync during " +
                    "\"{_director.CurrentDesignPhaseName} Phase\".");
                Msai.TrackException(new Exception("[INTERNAL] Some plate's width are out of sync."), "CMF");
            }
        }

        private List<IConnection> UpdateWidthChanged(List<IConnection> connectionList, double newWidth)
        {
            var affectedConnections = new List<IConnection>();

            foreach (var connection in connectionList)
            {
                if (connection.Width != newWidth)
                {
                    connection.Width = newWidth;
                    affectedConnections.Add(connection);
                }
            }

            return affectedConnections;
        }

        private void UpdateSynchronizablePlateWidthChanged(ImplantPreferenceModel data, bool updateEntities)
        {
            var connections = data.ImplantDataModel.ConnectionList.Where(c => c is ConnectionPlate && c.IsSynchronizable).ToList();
            var affectedConnections = UpdateWidthChanged(connections, data.PlateWidth);

            if (updateEntities)
            {
                UpdateImplantWidthEntities(data, false, affectedConnections);
            }
        }

        public void HandlePlateWidthChanged(ImplantPreferenceModel data, bool updateEntities = true)
        {
            if (CheckPlateWidthChanged(data))
            {
                UpdatePlateWidthChanged(data, updateEntities);
                RecheckMinMaxDistanceInExistingScrewQc(data);
            }
        }

        private void ShowPlateWidthOutOfSyncWarning(ImplantPreferenceModel data)
        {
            var plateWidth = data.ImplantDataModel.ConnectionList.Where(c => c is ConnectionPlate)
                .Where(c => Math.Abs(c.Width - data.PlateWidth) > _tolerance).Select(c => c.Width).Distinct();
            var plateWidthMessage= string.Join(",", plateWidth.Select(w => w.ToString("F2")));

            IDSPluginHelper.WriteLine(LogCategory.Warning, $"Syncing plate width during \"{_director.CurrentDesignPhaseName} Phase\" for implant case {data.CaseNumber} due to the plate width out of sync, " +
                                                           $"some width({plateWidthMessage}) is not equal to {data.PlateWidth}");
        }

        private void HandlePlateWidthOutOfSync(ImplantPreferenceModel data)
        {
            if (CheckPlateWidthChanged(data))
            {
                ShowPlateWidthOutOfSyncWarning(data);
                UpdateSynchronizablePlateWidthChanged(data, true);
            }
        }

        private bool CheckLinkWidthChanged(ImplantPreferenceModel data)
        {
            return data.ImplantDataModel.ConnectionList.Where(c => c is ConnectionLink && c.IsSynchronizable)
                .Any(c => Math.Abs(c.Width - data.LinkWidth) > _tolerance);
        }

        private void UpdateLinkWidthChanged(ImplantPreferenceModel data, bool updateEntities)
        {
            var connections = data.ImplantDataModel.ConnectionList.Where(c => c is ConnectionLink).ToList();
            var affectedConnections = UpdateWidthChanged(connections, data.LinkWidth);

            if (updateEntities)
            {
                UpdateImplantWidthEntities(data, false, affectedConnections);
            }

            if (data.ImplantDataModel.ConnectionList.Where(c => c is ConnectionLink).Any(c => !c.IsSynchronizable))
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "There are some link's width which are out of sync during " +
                    "\"{_director.CurrentDesignPhaseName} Phase\".");
                Msai.TrackException(new Exception("[INTERNAL] Some link's width are out of sync."), "CMF");
            }
        }

        private void UpdateSynchronizableLinkWidthChanged(ImplantPreferenceModel data, bool updateEntities)
        {
            var connections = data.ImplantDataModel.ConnectionList.Where(c => c is ConnectionLink && c.IsSynchronizable).ToList();
            var affectedConnections = UpdateWidthChanged(connections, data.LinkWidth);

            if (updateEntities)
            {
                UpdateImplantWidthEntities(data, false, affectedConnections);
            }
        }

        private void UpdateImplantWidthEntities(ImplantPreferenceModel data, bool updatePastille, List<IConnection> affectedConnections)
        {
            UpdateImplantPlanning(data, updatePastille, affectedConnections);
            ClearUndoRedo();
            RhinoLayerUtilities.DeleteEmptyLayers(_director.Document);
            _director.Document.Views.ActiveView.Redraw();
        }

        public void HandleLinkWidthChanged(ImplantPreferenceModel data, bool updateEntities = true)
        {
            if (CheckLinkWidthChanged(data))
            {
                UpdateLinkWidthChanged(data, updateEntities);
            }
        }

        private void ShowLinkWidthOutOfSyncWarning(ImplantPreferenceModel data)
        {
            var linkWidth = data.ImplantDataModel.ConnectionList.Where(c => c is ConnectionLink)
                .Where(c => Math.Abs(c.Width - data.LinkWidth) > _tolerance).Select(c => c.Width).Distinct();
            var linkWidthMessage = string.Join(",", linkWidth.Select(w => w.ToString("F2")));

            IDSPluginHelper.WriteLine(LogCategory.Warning, $"Syncing link width during \"{_director.CurrentDesignPhaseName} Phase\" for implant case {data.CaseNumber} due to the link width out of sync, " +
                                                           $"some width({linkWidthMessage}) is not equal to {data.LinkWidth}");
        }

        private void HandleLinkWidthOutOfSync(ImplantPreferenceModel data)
        {
            if (CheckLinkWidthChanged(data))
            {
                ShowLinkWidthOutOfSyncWarning(data);
                UpdateSynchronizableLinkWidthChanged(data, true);
            }
        }

        public void HandleDotPastilleChanged(ImplantPreferenceModel data, bool updateEntities = true)
        {
            var pastilles = data.ImplantDataModel.DotList.Where(d => d is DotPastille).Cast<DotPastille>().ToList();

            pastilles.ForEach(d => d.Diameter = data.CasePrefData.PastilleDiameter);

            if (updateEntities)
            {
                UpdateImplantPlanning(data, true);
            }
        }

        public void HandleImplantScrewTypeChanged(ImplantPreferenceModel data, bool isResetLength = true)
        {
            HandleDotPastilleChanged(data);
            RecalibrateImplantScrews(data, data.SelectedScrewType, isResetLength, false);
            ClearUndoRedo();

            RhinoLayerUtilities.DeleteEmptyLayers(_director.Document);
            _director.Document.Views.ActiveView.Redraw();
        }

        public void HandleBarrelTypeChanged(ImplantPreferenceModel data)
        {
            var implantComponent = new ImplantCaseComponent();
            var screwBuildingBlock = implantComponent.GetImplantBuildingBlock(IBB.Screw, data);
            var objectManager = new CMFObjectManager(_director);
            var screwRhinoObjects = objectManager.GetAllBuildingBlocks(screwBuildingBlock);

            Mesh guideSupportMesh = null;
            if (objectManager.HasBuildingBlock(IBB.GuideSupport))
            {
                guideSupportMesh = (Mesh)objectManager.GetBuildingBlock(IBB.GuideSupport).Geometry;
            }

            var skippedLevelingScrewBarrels = new List<Screw>();
            var succeededLevelingScrewBarrels = new List<Screw>();
            using (var screwBarrelRegistration = new CMFBarrelRegistrator(_director))
            {
                foreach (var screwRhinoObject in screwRhinoObjects)
                {
                    var screw = (Screw)screwRhinoObject;
                    screw.BarrelType = data.SelectedBarrelType;

                    bool isBarrelLevelingSkipped;
                    screwBarrelRegistration.RegisterSingleScrewBarrel(screw, guideSupportMesh, out isBarrelLevelingSkipped);

                    if (isBarrelLevelingSkipped)
                    {
                        skippedLevelingScrewBarrels.Add(screw);
                    }
                    else
                    {
                        succeededLevelingScrewBarrels.Add(screw);
                    }
                }
            }
            
            if (skippedLevelingScrewBarrels.Any())
            {
                BarrelLevelingErrorReporter.ReportGuideBarrelLevelingError(guideSupportMesh,
                    skippedLevelingScrewBarrels);
            }

            RecheckBarrelTypeExistingScrewQc(data);

            RegisteredBarrelUtilities.NotifyBuildingBlockHasChanged(_director, succeededLevelingScrewBarrels.Select(s => s.Id).ToList());
            ClearUndoRedo();

            RhinoLayerUtilities.DeleteEmptyLayers(_director.Document);
            _director.Document.Views.ActiveView?.Redraw();
        }

        public void HandleImplantScrewStyleChanged(ImplantPreferenceModel data, string previousStyle)
        {
            //reset implant screws length to default length
            var implantComponent = new ImplantCaseComponent();
            var screwBuildingBlock = implantComponent.GetImplantBuildingBlock(IBB.Screw, data);
            var objectManager = new CMFObjectManager(_director);
            var screwRhinoObjects = objectManager.GetAllBuildingBlocks(screwBuildingBlock);

            var screwManager = new ScrewManager(_director);
            var previouslyAvailableLengths = Queries.GetAvailableScrewLengths(data.SelectedScrewType, previousStyle);
            var defaultLength = data.CasePrefData.ScrewLengthMm;

            foreach (var screwRhinoObject in screwRhinoObjects)
            {
                var existingScrew = (Screw)screwRhinoObject;
                var index = existingScrew.Index;
                var existingScrewLength = existingScrew.Length;
                var nearestExistingLength = Queries.GetNearestAvailableScrewLength(previouslyAvailableLengths, existingScrewLength);
                if (nearestExistingLength == defaultLength)
                {
                    continue;
                }

                var newTipPoint = existingScrew.HeadPoint + existingScrew.Direction * defaultLength;
                var updatedScrew = ScrewUtilities.AdjustScrewLength(existingScrew, newTipPoint);
                screwManager.ReplaceExistingImplantScrewWithoutAnyInvalidation(updatedScrew, ref existingScrew, data);

                IDSPluginHelper.WriteLine(LogCategory.Warning, $"Implant Screw [{index}.I{data.NCase}] has undergone changes in length (previous: {nearestExistingLength:F1}mm, current: {defaultLength:F1}mm)!");
            }

            ClearUndoRedo();

            RhinoLayerUtilities.DeleteEmptyLayers(_director.Document);
            _director.Document.Views.ActiveView.Redraw();
        }

        public void HandleGuideFixationScrewTypeChanged(GuidePreferenceModel data, out List<ICaseData> unsharedScrewGuidePreferences, bool isResetLength = true)
        {
            RecalibrateGuideFixationScrews(data, data.SelectedGuideScrewType, isResetLength, out unsharedScrewGuidePreferences);

            ClearUndoRedo();

            RhinoLayerUtilities.DeleteEmptyLayers(_director.Document);
            _director.Document.Views.ActiveView.Redraw();
        }

        public void HandleGuideFixationScrewStyleChanged(GuidePreferenceModel data, string previousStyle)
        {
            //reset guide fixation screw length
            var guideComponent = new GuideCaseComponent();
            var buildingBlock = guideComponent.GetGuideBuildingBlock(IBB.GuideFixationScrew, data);
            var objectManager = new CMFObjectManager(_director);
            var screwRhinoObjects = objectManager.GetAllBuildingBlocks(buildingBlock);

            var screwManager = new ScrewManager(_director);
            var previouslyAvailableLengths = Queries.GetAvailableScrewLengths(data.SelectedGuideScrewType, previousStyle);
            var defaultLength = Queries.GetDefaultForGuideFixationScrewScrewLength(data.SelectedGuideScrewType, data.SelectedGuideScrewStyle);

            foreach (var screwRhinoObject in screwRhinoObjects)
            {
                var existingScrew = (Screw)screwRhinoObject;
                var existingLength = existingScrew.Length;
                var nearestExistingLength = Queries.GetNearestAvailableScrewLength(previouslyAvailableLengths, existingLength);
                if (nearestExistingLength == defaultLength)
                {
                    continue;
                }

                var newTipPoint = existingScrew.HeadPoint + existingScrew.Direction * defaultLength;
                var updatedScrew = ScrewUtilities.AdjustScrewLength(existingScrew, newTipPoint);
                screwManager.ReplaceExistingScrewInDocument(updatedScrew, ref existingScrew, data, false);

                var guideAndScrewItShared = updatedScrew.GetGuideAndScrewItSharedWith();
                guideAndScrewItShared.ForEach(cp =>
                {
                    var relatedScrew = cp.Value;
                    var duplicate = new Screw(_director, updatedScrew.HeadPoint,
                        updatedScrew.TipPoint, data.GuideScrewAideData.GenerateScrewAideDictionary(), relatedScrew.Index,
                        data.GuidePrefData.GuideScrewTypeValue);

                    screwManager.ReplaceExistingScrewInDocument(duplicate, ref relatedScrew, data, false);

                    var sharedWithScrews = updatedScrew.GetScrewItSharedWith();
                    duplicate.ShareWithScrews(sharedWithScrews);
                    duplicate.ShareWithScrew(updatedScrew);
                });

                IDSPluginHelper.WriteLine(LogCategory.Warning, $"Guide Fixation Screw for Guide {data.NCase} has undergone changes in length (previous: {nearestExistingLength:F1}mm, current: {defaultLength:F1}mm)!");
            }

            ClearUndoRedo();

            RhinoLayerUtilities.DeleteEmptyLayers(_director.Document);
            _director.Document.Views.ActiveView.Redraw();
        }

        public void UpdateImplantPlanning(ImplantPreferenceModel data, bool updatePastille, List<IConnection> affectedConnections = null)
        {
            var implantComponent = new ImplantCaseComponent();
            var objectManager = new CMFObjectManager(_director);
            var buildingBlock = implantComponent.GetImplantBuildingBlock(IBB.PlanningImplant, data);
            if (objectManager.HasBuildingBlock(buildingBlock))
            {
                var planningImplantBrepFactory = new PlanningImplantBrepFactory();
                var implant = planningImplantBrepFactory.CreateImplant(data.ImplantDataModel);
                var oldImplantGuid = objectManager.GetBuildingBlockId(buildingBlock);
                objectManager.SetBuildingBlock(buildingBlock, implant, oldImplantGuid);

                var ibbsToSkip = new List<IBB>();
                if (!updatePastille)
                {
                    ibbsToSkip.Add(IBB.PastillePreview);
                }

                var targetNodes = new List<TargetNode>();

                if (affectedConnections != null && affectedConnections.Any())
                {
                    var helper = new ConnectionPreviewHelper(_director);
                    var connectionPreviewIds = helper.GetRhinoObjectIdsFromConnections(data, affectedConnections);
                    if (connectionPreviewIds.Any())
                    {
                        targetNodes.Add(new TargetNode
                        {
                            Guids = connectionPreviewIds,
                            IBB = IBB.ConnectionPreview
                        });
                    }
                }

                data.Graph.NotifyBuildingBlockHasChanged(new[] { IBB.PlanningImplant }, targetNodes, ibbsToSkip.ToArray());
            }
        }

        public void RecalibrateImplantScrews(CasePreferenceDataModel data)
        {
            RecalibrateImplantScrews(data, data.CasePrefData.ScrewTypeValue, false, false);
        }

        public void RecalibrateImplantScrews(CasePreferenceDataModel data, string selectedScrewType, bool resetLength, bool maintainBarrelType)
        {
            var objectManager = new CMFObjectManager(_director);
            var implantSupportManager = new ImplantSupportManager(objectManager);
            var implantSupportMesh = implantSupportManager.GetImplantSupportMesh(data);
            if (implantSupportMesh == null)
            {
                return;
            }

            Mesh guideSupportMesh = null;
            if (objectManager.HasBuildingBlock(IBB.GuideSupport))
            {
                guideSupportMesh = (Mesh)objectManager.GetBuildingBlock(IBB.GuideSupport).Geometry;
            }

            var implantComponent = new ImplantCaseComponent();
            var screwBuildingBlock = implantComponent.GetImplantBuildingBlock(IBB.Screw, data);
            var screwRhinoObjects = objectManager.GetAllBuildingBlocks(screwBuildingBlock);
                
            var pastilles = data.ImplantDataModel.DotList.Where(d => d is DotPastille).Cast<DotPastille>().ToList();
            var skippedLevelingScrewBarrels = new List<Screw>();

            var tmpScrewGroups = new List<ScrewManager.ScrewGroup>();
            _director.ScrewGroups.Groups.ForEach(x => tmpScrewGroups.Add(new ScrewManager.ScrewGroup(x.ScrewGuids)));

            using (var screwBarrelRegistration = new CMFBarrelRegistrator(_director))
            {
                foreach (var screwRhinoObject in screwRhinoObjects)
                {
                    var currentScrew = (Screw)screwRhinoObject;
                    var pastille = pastilles.First(p => p.Screw?.Id == currentScrew.Id);

                    var length = resetLength ? data.CasePrefData.ScrewLengthMm : currentScrew.Length;
                    var newHeadPoint = RhinoPoint3dConverter.ToPoint3d(pastille.Location);
                    var newTipPoint = newHeadPoint + currentScrew.Direction * length;
                    var offset = data.CasePrefData.PlateThicknessMm;
                    var barrelType = maintainBarrelType ? currentScrew.BarrelType : data.CasePrefData.BarrelTypeValue;
                    var newScrew = RecalibrateImplantScrew(implantSupportMesh, newHeadPoint, newTipPoint, selectedScrewType,
                        barrelType, data.ScrewAideData.GenerateScrewAideDictionary(), currentScrew.Index, offset,
                        (int)(80 * offset));


                    var id = objectManager.AddNewBuildingBlock(screwBuildingBlock, newScrew);
                    newScrew.UpdateAidesInDocument();

                    ScrewPastilleManager.UpdateScrewDataInPastille(pastille, newScrew);
                    pastille.CreationAlgoMethod = DotPastille.CreationAlgoMethods[0];

                    var screwGroupIndex = _director.ScrewGroups.GetScrewGroupIndex(currentScrew);
                    if (screwGroupIndex != -1 && tmpScrewGroups[screwGroupIndex].ScrewGuids.Contains(currentScrew.Id))
                    {
                        tmpScrewGroups[screwGroupIndex].ScrewGuids.Remove(currentScrew.Id);
                        tmpScrewGroups[screwGroupIndex].ScrewGuids.Add(id);
                    }

                    objectManager.DeleteObject(currentScrew.Id);
                    bool isBarrelLevelingSkipped;
                    screwBarrelRegistration.RegisterSingleScrewBarrel(newScrew, guideSupportMesh, out isBarrelLevelingSkipped);

                    RegisteredBarrelUtilities.ReplaceLinkedImplantScrew(_director, currentScrew.Id, newScrew.Id);

                    if (isBarrelLevelingSkipped)
                    {
                        skippedLevelingScrewBarrels.Add(newScrew);
                    }
                }
            }

            _director.ScrewGroups.Groups = new List<ScrewManager.ScrewGroup>(tmpScrewGroups);

            if (skippedLevelingScrewBarrels.Any())
            {
                BarrelLevelingErrorReporter.ReportGuideBarrelLevelingError(guideSupportMesh,
                    skippedLevelingScrewBarrels);
            }

            data.Graph.NotifyBuildingBlockHasChanged(new[] { IBB.Screw }, IBB.PlanningImplant, IBB.RegisteredBarrel);

            var implantManager = new ImplantManager(objectManager);
            implantManager.InvalidateConnectionBuildingBlock(data);
            implantManager.InvalidateLandmarkBuildingBlock(data);
        }

        public void RecalibrateGuideFixationScrews(GuidePreferenceDataModel data)
        {
            List<ICaseData> unsharedScrewGuidePreferences;
            RecalibrateGuideFixationScrews(data, data.GuidePrefData.GuideScrewTypeValue, false, out unsharedScrewGuidePreferences);
        }

        private void RecalibrateGuideFixationScrews(GuidePreferenceDataModel data, string selectedGuideScrewType, bool resetLength, out List<ICaseData> unsharedScrewGuidePreferences)
        {
            unsharedScrewGuidePreferences = new List<ICaseData>();

            var objectManager = new CMFObjectManager(_director);
            if (objectManager.HasBuildingBlock(IBB.GuideSurfaceWrap))
            {
                var rhinoObj = objectManager.GetBuildingBlock(IBB.GuideSurfaceWrap);

                Mesh lowLoDConstraintMesh;
                objectManager.GetBuildingBlockLoDLow(rhinoObj.Id, out lowLoDConstraintMesh);

                var guideComponent = new GuideCaseComponent();
                var buildingBlock = guideComponent.GetGuideBuildingBlock(IBB.GuideFixationScrew, data);
                var screwRhinoObjects = objectManager.GetAllBuildingBlocks(buildingBlock);

                foreach (var screwRhinoObject in screwRhinoObjects)
                {
                    var currentScrew = (Screw)screwRhinoObject;

                    // will have accurate calibration later when guide preview
                    var newScrew = RecalibrateGuideFixationScrew(lowLoDConstraintMesh, data, currentScrew, selectedGuideScrewType, resetLength);
                    objectManager.AddNewBuildingBlock(buildingBlock, newScrew);

                    var labelTagHelper = new ScrewLabelTagHelper(_director);
                    labelTagHelper.SetNewScrewLabelTagFromOldScrew(currentScrew, newScrew);
                    newScrew.UpdateAidesInDocument();

                    var guideAndScrewItSharedWith = currentScrew.GetGuideAndScrewItSharedWith();
                    foreach (var keyPairValue in guideAndScrewItSharedWith)
                    {
                        if (keyPairValue.Value.Id == currentScrew.Id)
                        {
                            continue;
                        }

                        keyPairValue.Value.UnshareFromScrew(currentScrew);
                        if (!unsharedScrewGuidePreferences.Contains(keyPairValue.Key))
                        {
                            unsharedScrewGuidePreferences.Add(keyPairValue.Key);
                        }
                    }

                    objectManager.DeleteObject(currentScrew.Id);
                }

                data.Graph.NotifyBuildingBlockHasChanged(new[] { IBB.GuideFixationScrew }, IBB.GuideFixationScrewEye, IBB.GuideFixationScrewLabelTag);
            }
        }

        private Screw RecalibrateGuideFixationScrew(Mesh constraintMesh, GuidePreferenceDataModel data, Screw currentScrew, string selectedGuideScrewType, bool resetLength)
        {
            var calibrator = new GuideFixationScrewCalibrator();

            var newHeadPoint = calibrator.GetNewScrewHeadPoint(constraintMesh, currentScrew);

            if (!newHeadPoint.IsValid)
            {
                return null;
            }

            var length = currentScrew.Length;
            if (resetLength)
            {
                length = Queries.GetDefaultForGuideFixationScrewScrewLength(selectedGuideScrewType, data.GuidePrefData.GuideScrewStyle);
            }
            var newTipPoint = newHeadPoint + currentScrew.Direction * length;
            
            var screwAideDictionary = data.GuideScrewAideData.GenerateScrewAideDictionary();
            
            var screw = new Screw(_director, newHeadPoint, newTipPoint, screwAideDictionary, currentScrew.Index, selectedGuideScrewType);
            return calibrator.LevelScrew(screw, constraintMesh, currentScrew);
        }

        private Screw RecalibrateImplantScrew(Mesh constraintMesh, Point3d headPoint, Point3d tipPoint, string screwType, string barrelType, Dictionary<string, GeometryBase> screwAides, int index, double offset, int calibrationSteps)
        {
            var screw = new Screw(_director, headPoint, tipPoint, screwAides, index, screwType, barrelType);
            var calibrator = new ScrewCalibrator(constraintMesh);
            calibrator.LevelHeadOnTopOfMesh(screw, offset, calibrationSteps, true);
            return calibrator.CalibratedScrew;
        }

        private void ClearUndoRedo()
        {
            _director.Document.ClearUndoRecords(true);
            _director.Document.ClearRedoRecords();
            _director.IdsDocument?.ClearUndoRedo();
        }

        public void SyncOutOfSyncProperties()
        {
            foreach (var casePreferenceDataModel in _director.CasePrefManager.CasePreferences)
            {
                var implantPreferenceModel = (ImplantPreferenceModel) casePreferenceDataModel;
                HandlePlateThicknessOutOfSync(implantPreferenceModel);
                HandlePlateWidthOutOfSync(implantPreferenceModel);
                HandleLinkWidthOutOfSync(implantPreferenceModel);
            }
        }

        private void RecheckMinMaxDistanceInExistingScrewQc(CasePreferenceDataModel casePreference)
        {
            if (_director.ImplantScrewQcLiveUpdateHandler == null)
            {
                return;
            }

            var screwQcCheckManager =
                new ScrewQcCheckerManager(_director, new[] { new MinMaxDistancesChecker(_director) });
            var screwManager = new ScrewManager(_director);
            var allScrews = screwManager.GetScrews(casePreference, false);
            _director.ImplantScrewQcLiveUpdateHandler.RecheckCertainResult(screwQcCheckManager, allScrews);
        }

        private void RecheckBarrelTypeExistingScrewQc(CasePreferenceDataModel casePreference)
        {
            if (_director.ImplantScrewQcLiveUpdateHandler == null)
            {
                return;
            }

            var screwManager = new ScrewManager(_director);
            var allScrews = screwManager.GetScrews(casePreference, false);
            RecheckBarrelTypeExistingScrewQc(allScrews);
        }

        public void RecheckBarrelTypeExistingScrewQc(List<Screw> screws)
        {
            if (_director.ImplantScrewQcLiveUpdateHandler == null)
            {
                return;
            }

            var screwQcCheckManager =
                new ScrewQcCheckerManager(_director, new[] { new BarrelTypeChecker() });
            _director.ImplantScrewQcLiveUpdateHandler.RecheckCertainResult(screwQcCheckManager, screws);
        }
    }
}
