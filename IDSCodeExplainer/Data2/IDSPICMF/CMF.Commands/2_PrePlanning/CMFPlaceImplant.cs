using IDS.CMF;
using IDS.CMF.AttentionPointer;
using IDS.CMF.CasePreferences;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.Enumerators;
using IDS.CMF.Graph;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Query;
using IDS.CMF.Utilities;
using IDS.CMF.V2.DataModel;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Interface.Implant;
using IDS.PICMF.Drawing;
using IDS.PICMF.Forms;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Linq;
using DrawMode = IDS.Core.Drawing.DrawMode;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("601C4404-9AC4-4638-8B1C-1AA64903DF42")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Planning | DesignPhase.Implant)]
    public class CMFPlaceImplant : CmfCommandBase
    {
        public CMFPlaceImplant()
        {
            TheCommand = this;
            VisualizationComponent = new CMFDesignImplantVisualization();
        }

        /// The one and only instance of this command
        public static CMFPlaceImplant TheCommand { get; private set; }

        /// The command name as it appears on the Rhino command line
        public override string EnglishName => CommandEnglishName.CMFPlaceImplant;

        private Mesh roiVolume = null;

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            roiVolume = null;

            var gm = new GetOption();
            gm.SetCommandPrompt("Select implant changes mode");
            gm.AcceptNothing(false);
            var modeIndicate = gm.AddOption("Indicate");
            var modeStartNewBranch = gm.AddOption("StartNewBranch");
            var modeDelete = gm.AddOption("Delete");
            var modeMove = gm.AddOption("Move");
            var modeCasePreference = gm.AddOption("CasePreference");

            var editImplantId = Guid.Empty;
            var casePreferenceId = Guid.Empty;
            var editingDrawMode = DrawMode.Indicate;

            while (true)
            {
                var gres = gm.Get();
                if (gres == GetResult.Cancel)
                {
                    return Result.Failure;
                }

                if (gres != GetResult.Option)
                {
                    continue;
                }

                if (gm.OptionIndex() == modeIndicate)
                {
                    break;
                }

                if (gm.OptionIndex() == modeStartNewBranch)
                {
                    editImplantId = SelectImplantToEdit(doc);
                    if (editImplantId == Guid.Empty)
                    {
                        return Result.Failure;
                    }
                    break;
                }

                if (gm.OptionIndex() == modeDelete)
                {
                    if (HandleExistingImplantToAdvanceEdit(ref casePreferenceId, ref editImplantId, director) != Result.Success)
                    {
                        return Result.Failure;
                    }

                    editingDrawMode = DrawMode.Delete;
                    break;
                }

                if (gm.OptionIndex() == modeMove)
                {
                    if (HandleExistingImplantToAdvanceEdit(ref casePreferenceId, ref editImplantId, director) != Result.Success)
                    {
                        return Result.Failure;
                    }

                    editingDrawMode = DrawMode.Move;
                    break;
                }

                if (gm.OptionIndex() != modeCasePreference)
                {
                    continue;
                }

                casePreferenceId = GetCasePreferenceId();
                if (casePreferenceId == Guid.Empty)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, $"Invalid case preference id: {casePreferenceId}");
                    return Result.Failure;
                }
                editImplantId = GetImplantToEdit(director, casePreferenceId);
                break;
            }

            var tmpCasePrefData = director.CasePrefManager.GetCase(casePreferenceId);
            if (tmpCasePrefData != null)
            {
                TrackingParameters.Add("CaseName", tmpCasePrefData.CasePrefData.ImplantTypeValue);
                TrackingParameters.Add("CaseIndex", tmpCasePrefData.NCase.ToString());
            }
            TrackingParameters.Add("Mode", editingDrawMode.ToString());

            var objectManager = new CMFObjectManager(director);
            var targetLowLoDMeshes = new List<Mesh>();

            var implantSupportBb = GetImplantSupportBb(tmpCasePrefData);
            if (objectManager.HasBuildingBlock(implantSupportBb))
            {
                var rhObj = objectManager.GetBuildingBlock(implantSupportBb);

                //automatically show and set a very high transparency
                var transparencyValue = 0.75;

                var material = rhObj.GetMaterial(true);
                material.Transparency = transparencyValue;
                material.CommitChanges();

                var l = rhObj.Attributes.LayerIndex;
                var layer = doc.Layers[l];
                layer.CommitChanges();

                if (!layer.IsVisible && layer.IsValid)
                {
                    doc.Layers.ForceLayerVisible(layer.Id);
                }

                Mesh lowLoD;
                objectManager.GetBuildingBlockLoDLow(rhObj.Id, out lowLoD);
                targetLowLoDMeshes.Add(lowLoD);

            }
            else
            {
                var constraintMeshQuery = new ConstraintMeshQuery(objectManager);
                targetLowLoDMeshes = constraintMeshQuery.GetVisibleConstraintMeshesForImplant(true).ToList();
            }

            if (targetLowLoDMeshes == null || !targetLowLoDMeshes.ToList().Any())
            {
                var layer = "Planned layer and its sublayer";
                if (objectManager.HasBuildingBlock(implantSupportBb))
                {
                    layer = implantSupportBb.Block.Name;
                }

                IDSPluginHelper.WriteLine(LogCategory.Warning, $"No target mesh is visible, please ensure {layer} layer is toggled to visible.");

                return Result.Failure;
            }

            var existingPlanningImplant = objectManager.GetAllBuildingBlocks(IBB.PlanningImplant).FirstOrDefault(obj => obj.Id == editImplantId);

            ImplantSurfaceRoIVisualizer RoIVisualizer = null;
            if (objectManager.HasBuildingBlock(implantSupportBb))
            {
                var implantSupport = objectManager.GetBuildingBlock(implantSupportBb);
                RoIVisualizer = new ImplantSurfaceRoIVisualizer(tmpCasePrefData, implantSupport);
                RoIVisualizer.Enabled = true;
            }

            ImplantDataModel exisitingDataModel = null;
            ImplantDataModel implantDataModel = null;

            try
            {
                if (existingPlanningImplant != null)
                {
                    exisitingDataModel = (ImplantDataModel) objectManager.GetImplantDataModel(existingPlanningImplant).Clone();
                }
                
                implantDataModel = existingPlanningImplant == null ? PlaceScrew(director, targetLowLoDMeshes, casePreferenceId, out _) :
                    PlaceScrew(director, targetLowLoDMeshes, casePreferenceId, objectManager.GetImplantDataModel(existingPlanningImplant), editingDrawMode, out _);
            }
            catch (Exception e)
            {
                Msai.TrackException(e, "CMF");
                if (RoIVisualizer != null)
                {
                    RoIVisualizer.Enabled = false;
                }
            }

            if (RoIVisualizer != null)
            {
                RoIVisualizer.Enabled = false;
            }

            RoIVisualizer?.Dispose();

            if (implantDataModel == null)
            {
                return Result.Failure;
            }

            if (!implantDataModel.IsHasConstruction())
            {
                var casePreferenceData = objectManager.GetCasePreference(existingPlanningImplant);

                if (existingPlanningImplant != null)
                {
                    director.ImplantManager.HandleDeleteAllPlanningImplantRelatedItems(casePreferenceData);
                    director.ImplantManager.DeleteAllConnectionsBuildingBlock(casePreferenceData);
                    director.ImplantManager.DeleteLandmarksBuildingBlock(casePreferenceData);
                    casePreferenceData.ImplantDataModel = implantDataModel;
                }

                ClearHistory(doc);
                casePreferenceData.InvalidateEvents(director);
                CasePreferencePanel.GetView().InvalidateUI();
                return Result.Success;
            }

            if (existingPlanningImplant == null)
            {
                var casePreferenceData = director.CasePrefManager.CasePreferences.First(c => c.CaseGuid == casePreferenceId);
                casePreferenceData.ImplantDataModel = implantDataModel;

                casePreferenceData.InvalidateEvents(director);
                director.ImplantManager.HandleAddNewImplant(casePreferenceData, false);
                director.ImplantManager.HandleAllPlanningImplantRelatedItemsInvalidation(casePreferenceData);
            }
            else
            {
                //Strangely the datamodel in director is updated from PlaceScrew for this case...
                var casePreferenceData = objectManager.GetCasePreference(existingPlanningImplant);
                director.ImplantManager.HandleAllPlanningImplantRelatedItemsInvalidation(casePreferenceData, exisitingDataModel);

                // Create newly added screw
                var screwCreator = new ScrewCreator(director);
                if (!screwCreator.CreateAllScrewBuildingBlock(true, casePreferenceData, roiVolume ?? MeshUtilities.AppendMeshes(targetLowLoDMeshes)))
                {
                    return Result.Failure;
                }

                if (roiVolume == null)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Warning, $"Uncalibrated screws created on CaseID: {casePreferenceData.CaseGuid}");
                }

                casePreferenceData.InvalidateEvents(director);
                roiVolume = null;
            }

            ClearHistory(doc);
            PastilleAttentionPointer.Instance.RefreshHighlightedPastillePosition(director);
            CasePreferencePanel.GetView().InvalidateUI();
            return Result.Success;
        }

        private Guid SelectImplantToEdit(RhinoDoc doc)
        {
            //temporary for testing
            VisualizationComponent.OnCommandSuccessVisualization(doc);

            Locking.UnlockImplants(doc);

            var selectImplant = new GetObject();
            selectImplant.SetCommandPrompt("Select implant to edit.");
            selectImplant.EnablePreSelect(false, false);
            selectImplant.EnablePostSelect(true);
            selectImplant.AcceptNothing(true);

            var guid = Guid.Empty;

            var res = selectImplant.Get();

            switch (res)
            {
                case GetResult.Cancel:
                case GetResult.Nothing:
                    break;
                case GetResult.Object:
                    var rhinoObj = selectImplant.Object(0).Object();
                    guid = rhinoObj.Id;
                    break;
            }

            //temporary for testing
            VisualizationComponent.OnCommandBeginVisualization(doc);

            return guid;
        }

        private Guid GetCasePreferenceId()
        {
            var casePreferenceId = Guid.Empty;
            var casePreferenceIdStr = string.Empty;
            var result = RhinoGet.GetString("CasePreferenceId", false, ref casePreferenceIdStr);
            if (result != Result.Success)
            {
                return casePreferenceId;
            }
            if (!Guid.TryParse(casePreferenceIdStr, out casePreferenceId))
            {
                casePreferenceId = Guid.Empty;
            }
            return casePreferenceId;
        }

        private Guid GetImplantToEdit(CMFImplantDirector director, Guid casePreferenceId)
        {
            var casePreferenceData = director.CasePrefManager.GetCase(casePreferenceId);
            var implantComponent = new ImplantCaseComponent();
            var buildingBlock = implantComponent.GetImplantBuildingBlock(IBB.PlanningImplant, casePreferenceData);
            var objectManager = new CMFObjectManager(director);
            var implantId = Guid.Empty;

            var implantObj = objectManager.GetBuildingBlock(buildingBlock);
            if (implantObj != null)
            {
                implantId = implantObj.Id;
                Locking.UnlockImplant(director.Document, implantId);
                implantObj.Select(true);
            }

            return implantId;
        }

        private ImplantDataModel PlaceScrew(CMFImplantDirector director, IEnumerable<Mesh> targetLowLoDMeshes, Guid casePreferenceId, out List<IDot> changedDots)
        {
            return PlaceScrew(director, targetLowLoDMeshes, casePreferenceId, null, DrawMode.Indicate, out changedDots);
        }

        private ImplantDataModel PlaceScrew(CMFImplantDirector director, IEnumerable<Mesh> targetLowLoDMeshes,
            Guid casePreferenceId, ImplantDataModel dataModel, DrawMode editExistingImplantDrawMode, out List<IDot> changedDots)
        {
            roiVolume = null;
            changedDots = new List<IDot>();

            var gp = new DrawImplant(director);
            var handleOnPlannedLayerChanged = new EventHandler<Rhino.DocObjects.Tables.LayerTableEventArgs>((s, e) =>
            {
                var implantSupportBb = GetImplantSupportBb(director, casePreferenceId);
                var objManager = new CMFObjectManager(director);

                if (!objManager.HasBuildingBlock(implantSupportBb))
                {
                    var query = new ConstraintMeshQuery(objManager);
                    gp.LowLoDConstraintMesh = MergeMeshes(query.GetVisibleConstraintMeshesForImplant(true));
                }
            });

            RhinoDoc.LayerTableEvent += handleOnPlannedLayerChanged;

            SetDefaultValues(casePreferenceId, director, ref gp);
            gp.LowLoDConstraintMesh = MergeMeshes(targetLowLoDMeshes);

            if (dataModel != null)
            {
                var clonedDataModel = (ImplantDataModel)dataModel.Clone();
                gp.SetExistingImplant(clonedDataModel, editExistingImplantDrawMode);
            }

            var success = gp.Execute();
            RhinoDoc.LayerTableEvent -= handleOnPlannedLayerChanged;
            if (!success)
            {
                return null;
            }

            var oldConnections = (dataModel == null) ? new List<IConnection>() : dataModel.ConnectionList;
            var newConnections = gp.DataModelBase.ConnectionList;

            UpdateImplantRoIVolumeOnImplantPhase(director, casePreferenceId, oldConnections, newConnections, ref roiVolume);

            GetDotLocationOnConstraintMesh(director, oldConnections, newConnections, roiVolume, gp.LowLoDConstraintMesh, casePreferenceId);

            // Newly Added Pastilles
            if (dataModel == null || !gp.DataModelBase.ConnectionList.Any())
            {
                var newDataModel = new ImplantDataModel(newConnections);
                return newDataModel;
            }

            var casePreference = director.CasePrefManager.GetCase(casePreferenceId);
            changedDots =
                DataModelUtilities.FindDotDifferenceInNewConnection<IDot>(
                    oldConnections, newConnections);

            var differencePastille = changedDots.OfType<DotPastille>().ToList();

            // Create a dictionary mapping DotPastille Guid to DotPastille and Screw info for data access
            // Screw Index and Group info can no longer be accessed as Screw Objects are removed after "Update"
            var differencePastilleToScrewInfoMap = new Dictionary<Guid, (DotPastille Pastille, int ScrewIndex, int ScrewGroup)>();
            foreach (var pastille in differencePastille)
            {
                if (pastille.Screw?.Id != null)
                {
                    var screw = director.Document.Objects.Find(pastille.Screw.Id) as Screw;
                    if (screw != null)
                    {
                        var screwIndex = screw.Index;
                        // Capture screw group information before screws are removed
                        var screwGroupIndex = director.ScrewGroups.GetScrewGroupIndex(screw);
                        differencePastilleToScrewInfoMap[pastille.Id] = (pastille, screwIndex, screwGroupIndex);
                    }
                }
            }

            // Order matters. "Update" invokes the event to add/update the IDot and IConnections in the IdsDocument
            // Only then add the pastilles and connection curve as child for the IConections
            dataModel.Update(newConnections);

            // Move/Remove Pastille
            if (DataModelUtilities.AnyDotLocationChanged<IDot>(oldConnections, newConnections))
            {
                var obsoleteIDots =
                   DataModelUtilities.FindDotDifferenceInNewConnection<IDot>(
                        newConnections, oldConnections).ToList();
                var obsoletePastilles =
                    obsoleteIDots.OfType<DotPastille>().ToList();

                var helper = new PastillePreviewHelper(director);
                
                //will use LoD here if things are still during planning before import implant support.
                ScrewPastilleManager.UpdateScrewsAfterMovePastilles(director, newConnections,
                    differencePastilleToScrewInfoMap, casePreference, roiVolume ?? gp.LowLoDConstraintMesh);

                var pastillePreviewIds = helper.GetPastillePreviewBuildingBlockIds(casePreference, obsoletePastilles);
                var connectionPreviewIds = GetAffectedConnectionPreviewIds(director, casePreference, changedDots, obsoleteIDots, oldConnections, newConnections);

                casePreference.Graph.NotifyBuildingBlockHasChanged(new[] { IBB.Screw }, new List<TargetNode>
                {
                    new TargetNode
                    {
                        Guids = pastillePreviewIds,
                        IBB = IBB.PastillePreview
                    },
                    new TargetNode
                    {
                        Guids = connectionPreviewIds,
                        IBB = IBB.ConnectionPreview
                    }
                }, IBB.Connection, IBB.Landmark, IBB.RegisteredBarrel);
            }
            // Add Pastille
            else if (DataModelUtilities.IsDifferent<IDot>(oldConnections, newConnections))
            {
                var pastillePreviewIds = new List<Guid>();

                var obsoletePastilles = DataModelUtilities.FindDotDifferenceInNewConnection<IDot>
                    (newConnections, oldConnections).OfType<DotPastille>().ToList();

                if (obsoletePastilles.Any())
                {
                    var helper = new PastillePreviewHelper(director);
                    pastillePreviewIds.AddRange(helper.GetPastillePreviewBuildingBlockIds(casePreference, obsoletePastilles));
                }

                var obsoleteDots = DataModelUtilities.FindDotDifferenceInNewConnection<IDot>(newConnections, oldConnections);
                var connectionPreviewIds = GetAffectedConnectionPreviewIds(director, casePreference, changedDots, obsoleteDots, oldConnections, newConnections);

                casePreference.Graph.NotifyBuildingBlockHasChanged(new[] { IBB.Connection }, new List<TargetNode>
                {
                    new TargetNode
                    {
                        Guids = pastillePreviewIds,
                        IBB = IBB.PastillePreview
                    },
                    new TargetNode
                    {
                        Guids = connectionPreviewIds,
                        IBB = IBB.ConnectionPreview
                    }
                });
            }
            else if (DataModelUtilities.IsAnythingChanged(dataModel.ConnectionList, newConnections))
            {
                casePreference.Graph.NotifyBuildingBlockHasChanged(new[] { IBB.Connection }, IBB.Connection, IBB.Landmark);
            }

            return dataModel;
        }

        private List<Guid> GetAffectedConnectionPreviewIds(CMFImplantDirector director, CasePreferenceDataModel casePreference, List<IDot> newChanges, List<IDot> oldChanges, List<IConnection> oldConnections, List<IConnection> newConnections)
        {
            var connectionPreviewHelper = new ConnectionPreviewHelper(director);
            var connectionPreviewIds = connectionPreviewHelper.GetRhinoObjectIdsFromDots(casePreference, oldChanges);

            var differencePastilleIds = newChanges.Select(p => p.Id);
            var obsolateConnections = new List<IConnection>();
            var affectedDotIds = new List<Guid>();
            var affectedDotControlPointIds = new List<Guid>();

            foreach (var connection in newConnections)
            {
                if (differencePastilleIds.Contains(connection.A.Id))
                {
                    affectedDotIds.Add(connection.B.Id);

                    if (connection.B is DotControlPoint)
                    {
                        affectedDotControlPointIds.Add(connection.B.Id);
                    }
                }
                else if (differencePastilleIds.Contains(connection.B.Id))
                {
                    affectedDotIds.Add(connection.A.Id);

                    if (connection.A is DotControlPoint)
                    {
                        affectedDotControlPointIds.Add(connection.A.Id);
                    }
                }
            }

            foreach (var connection in oldConnections)
            {
                if (affectedDotIds.Contains(connection.A.Id) && affectedDotIds.Contains(connection.B.Id) && !newConnections.Any(c => c.A.Id == connection.A.Id && c.B.Id == connection.B.Id))
                {
                    obsolateConnections.Add(connection);
                }
                else if ((affectedDotControlPointIds.Contains(connection.A.Id) || affectedDotControlPointIds.Contains(connection.B.Id)) && newConnections.Any(c => c.A.Id == connection.A.Id && c.B.Id == connection.B.Id))
                {
                    //invalidate any connection that branch out from affected control point
                    obsolateConnections.Add(connection);
                }
            }

            connectionPreviewIds.AddRange(connectionPreviewHelper.GetRhinoObjectIdsFromConnections(casePreference, obsolateConnections));

            return connectionPreviewIds;
        }

        private bool UpdateImplantRoIVolumeOnImplantPhase(CMFImplantDirector director, Guid casePreferenceId,
            List<IConnection> oldConnections, List<IConnection> newConnections, ref Mesh actualSupportRoiVolume)
        {
            var casePreference = director.CasePrefManager.GetCase(casePreferenceId);
            var implantSupportBb = GetImplantSupportBb(casePreference);

            var objectManager = new CMFObjectManager(director);
            if (!objectManager.HasBuildingBlock(implantSupportBb) ||
                director.CurrentDesignPhase != DesignPhase.Implant ||
                !oldConnections.Any())
            {
                return false;
            }

            var actualSupportRhObject = objectManager.GetBuildingBlock(implantSupportBb);

            var changedDots = DataModelUtilities.FindDotDifferenceInNewConnection<IDot>(oldConnections, newConnections);
            //GOT BUG THAT IT UPDATE IN PLANNING PHASE, no need becoz no need to calibrate the screw
            if (!changedDots.Any())
            {
                actualSupportRoiVolume = ImplantCreationUtilities.GetImplantRoIVolumeWithoutCheck(objectManager, casePreference,
                    ref actualSupportRhObject);
            }
            else
            {
                var changedConnections =
                    DataModelUtilities.FindConnectionDifferenceInNewConnection(oldConnections, newConnections);
                actualSupportRoiVolume = ImplantCreationUtilities.GetImplantRoIVolume(objectManager, casePreference,
                    ref actualSupportRhObject, changedDots, changedConnections);
            }

            return true;
        }

        private void GetDotLocationOnConstraintMesh(CMFImplantDirector director, List<IConnection> oldConnections,
            List<IConnection> newConnections, Mesh actualSupportRoiVolume, Mesh lowLoDConstraintMesh, Guid casePrefId)
        {
            var dotsNeedFinalize = DataModelUtilities.FindDotDifferenceInNewConnection<IDot>(oldConnections, newConnections);
            if (!dotsNeedFinalize.Any())
            {
                return;
            }

            var maximumDistanceAllowed = DotUtilities.MaximumDistanceAllowed;

            var implantSupportBb = GetImplantSupportBb(director, casePrefId);

            var objectManager = new CMFObjectManager(director);
            var constraintMesh = objectManager.HasBuildingBlock(implantSupportBb) ?
                actualSupportRoiVolume ?? (Mesh)objectManager.GetBuildingBlock(implantSupportBb).DuplicateGeometry() :
                lowLoDConstraintMesh;

            foreach (var dot in dotsNeedFinalize)
            {
                var finalizedDot = DotUtilities.FindDotOnDifferentMesh(dot, constraintMesh, maximumDistanceAllowed);

                if (finalizedDot == null)
                {
                    continue;
                }

                dot.Location = finalizedDot.Location;
                dot.Direction = finalizedDot.Direction;
            }
        }

        private void SetDefaultValues(Guid casePreferenceId, CMFImplantDirector director, ref DrawImplant gp)
        {
            if (casePreferenceId == Guid.Empty)
            {
                return;
            }

            var casePreference = director.CasePrefManager.CasePreferences.FirstOrDefault(cp => cp.CaseGuid == casePreferenceId);
            if (casePreference == null)
            {
                return;
            }

            gp.SetDefaultValues(casePreference.CasePrefData.PlateThicknessMm, casePreference.CasePrefData.PlateWidthMm, casePreference.CasePrefData.LinkWidthMm, casePreference.CasePrefData.PastilleDiameter);
        }

        private Mesh MergeMeshes(IEnumerable<Mesh> targetMeshes)
        {
            var duplicated = targetMeshes.Select(mesh => mesh.DuplicateMesh());
            var merged = MeshUtilities.AppendMeshes(duplicated);
            if (merged != null && merged.FaceNormals.Count == 0)
            {
                merged.FaceNormals.ComputeFaceNormals();
            }
            return merged;
        }

        private ExtendedImplantBuildingBlock GetImplantSupportBb(CMFImplantDirector director, Guid casePrefId)
        {
            return GetImplantSupportBb(director.CasePrefManager.GetCase(casePrefId));
        }

        private ExtendedImplantBuildingBlock GetImplantSupportBb(CasePreferenceDataModel casePreferenceDataModel)
        {
            var implantCaseComponent = new ImplantCaseComponent();
            return implantCaseComponent.GetImplantBuildingBlock(IBB.ImplantSupport, casePreferenceDataModel);
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, CMFImplantDirector director)
        {
            doc.Objects.UnselectAll();
            doc.Views.Redraw();
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, CMFImplantDirector director)
        {
            doc.Objects.UnselectAll();
            doc.Views.Redraw();
        }

        private Result HandleExistingImplantToAdvanceEdit(ref Guid casePreferenceId, ref Guid editImplantId, CMFImplantDirector director)
        {
            casePreferenceId = GetCasePreferenceId();
            if (casePreferenceId == Guid.Empty)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, $"Invalid case preference id: {casePreferenceId}");
                return Result.Failure;
            }
            editImplantId = GetImplantToEdit(director, casePreferenceId);

            if (editImplantId == Guid.Empty)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "No implant detected!");
                return Result.Failure;
            }

            return Result.Success;
        }

        private void ClearHistory(RhinoDoc doc)
        {
            doc.ClearUndoRecords(true);
            doc.ClearRedoRecords();
        }
    }
}