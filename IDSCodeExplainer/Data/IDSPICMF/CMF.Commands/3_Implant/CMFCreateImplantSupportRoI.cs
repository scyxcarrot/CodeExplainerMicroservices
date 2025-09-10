using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.DataModel;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Query;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.PICMF.Forms;
using IDS.PICMF.Operations;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Style = Rhino.Commands.Style;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("54CC0505-FC68-4B2A-BEA9-7799D5124A1F")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Implant)]
    public class CMFCreateImplantSupportRoI : CmfCommandBase
    {
        public CMFCreateImplantSupportRoI()
        {
            TheCommand = this;
            VisualizationComponent = new CMFCreateImplantSupportRoIVisualizationComponent();
        }
        
        public static CMFCreateImplantSupportRoI TheCommand { get; private set; }
        
        public override string EnglishName => "CMFCreateImplantSupportRoI";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var dataModel = director.ImplantManager.GetImplantSupportRoICreationDataModel();
            var roiCreationDataModel = new ImplantSupportRoICreationDataModel(dataModel);
            SetupDataModel(director, ref roiCreationDataModel);

            var roICreationDialog = new ImplantSupportRoICreationDialog(roiCreationDataModel) { Topmost = true };
            roICreationDialog.Show();

            var getOption = new GetOption();
            getOption.SetCommandPrompt("Choose the operation for define RoI");

            var metalIntegrate = getOption.AddOption("MetalIntegration");
            var trimRemovedMetal = getOption.AddOption("TrimRemovedMetal");
            var teethIntegrate = getOption.AddOption("TeethIntegration");
            var preview = getOption.AddOption("Preview");
            var ok = getOption.AddOption("OK");
            var cancel = getOption.AddOption("Cancel");

            getOption.EnableTransparentCommands(false);
            var res = Result.Cancel;
            Exception exception = null;
            var conduit = new ImplantSupportRoIConduit(roiCreationDataModel) {Enabled = true};
            doc.Views.Redraw();

            while (true)
            {
                getOption.Get();

                var commandResult = getOption.CommandResult();
                if (commandResult != Result.Success)
                {
                    if (roICreationDialog.CancelConfirmation())
                    {
                        res = Result.Cancel;
                        break;
                    }
                }

                var option = getOption.Option();
                if (option == null)
                {
                    continue;
                }

                roICreationDialog.IsEnabled = false;

                var optionSelected = option.Index;
                var needBreak = false;
                try
                {
                    if (optionSelected == metalIntegrate)
                    {
                        ((CMFCreateImplantSupportRoIVisualizationComponent)VisualizationComponent).OnMetalButtonClicked(doc);
                        conduit.Enabled = false;
                        doc.Views.Redraw();
                        MetalIntegration(ref roiCreationDataModel, doc);
                        conduit.Enabled = true;
                        doc.Views.Redraw();
                        ((CMFCreateImplantSupportRoIVisualizationComponent)VisualizationComponent).OnMetalSelected(doc);
                    }
                    else if (optionSelected == trimRemovedMetal)
                    {
                        if (roiCreationDataModel.Metal.IntegratedRemovedMetal != null)
                        {
                            ((CMFCreateImplantSupportRoIVisualizationComponent)VisualizationComponent).OnTrimRemovedMetalBegin(doc);
                            conduit.Enabled = false;
                            doc.Views.Redraw();
                            TrimRemovedMetal(ref roiCreationDataModel, doc);
                            conduit.Enabled = true;
                            doc.Views.Redraw();
                            ((CMFCreateImplantSupportRoIVisualizationComponent)VisualizationComponent).OnTrimRemovedMetalCompleted(doc);
                        }
                        else
                        {
                            MessageBox.Show("No removed metal been preview yet");
                        }
                    }
                    else if (optionSelected == teethIntegrate)
                    {
                        ((CMFCreateImplantSupportRoIVisualizationComponent)VisualizationComponent).OnTeethButtonClicked(doc);
                        conduit.Enabled = false;
                        doc.Views.Redraw();
                        TeethIntegration(ref roiCreationDataModel, doc);
                        conduit.Enabled = true;
                        doc.Views.Redraw();
                    }
                    else if (optionSelected == preview)
                    {
                        conduit.Enabled = false;
                        doc.Views.Redraw();
                        Preview(ref roiCreationDataModel, director);
                        conduit.Enabled = true;
                        doc.Views.Redraw();
                    }
                    else if (optionSelected == ok)
                    {
                        res = OnOK(ref roiCreationDataModel, out needBreak);
                    }
                    else if (optionSelected == cancel)
                    {
                        if (roICreationDialog.CancelConfirmation())
                        {
                            res = Result.Cancel;
                            needBreak = true;
                        }
                    }
                    else
                    {
                        res = Result.Failure;
                        needBreak = true;
                    }
                }
                catch(Exception ex)
                {
                    exception = ex;
                    needBreak = true;
                }

                roICreationDialog.IsEnabled = true;

                if (needBreak)
                {
                    break;
                }
            }

            conduit.Enabled = false;
            conduit.CleanUp();
            roiCreationDataModel.CleanUp();
            roICreationDialog.ForceClose();

            if (exception != null)
            {
                throw exception;
            }

            if (res == Result.Success)
            {
                res = UpdateChanges(director, roiCreationDataModel);
            }

            return res;
        }

        private void MetalIntegration(ref ImplantSupportRoICreationDataModel roiCreationDataModel, RhinoDoc doc)
        {
            var metalInputGetter = new ImplantSupportRoIMetalInputGetter(doc);

            if (metalInputGetter.SelectMetal(out var integratedMetalInfos) != Result.Success)
            {
                return;
            }

            roiCreationDataModel.Metal.SelectedMetalInfos = integratedMetalInfos;
            var remainedMetalsCount = integratedMetalInfos.Count(m => m.State == EMetalIntegrationState.Remain);
            var removedMetalsCount = integratedMetalInfos.Count(m => m.State == EMetalIntegrationState.Remove);

            IDSPluginHelper.WriteLine(LogCategory.Default, $"Selected: {((remainedMetalsCount > 0) ? $"{remainedMetalsCount} Parts Remained, ":"")}" +
                                                           $"{(removedMetalsCount > 0 ? $"{removedMetalsCount} Parts Removed" : "")}");
        }

        private void TrimRemovedMetal(ref ImplantSupportRoICreationDataModel roiCreationDataModel, RhinoDoc doc)
        {
            var removedMetalPreview = roiCreationDataModel.Metal.IntegratedRemovedMetal;
            var meshesGoingToTrim = new Dictionary<Guid, Mesh>()
            {
                {Guid.NewGuid(), removedMetalPreview}
            };

            var trimmerTool = new MultipleMeshesTrimmer(meshesGoingToTrim);
            if (!trimmerTool.Execute(doc, "Select the removed metal shell that needs trimming",
                out var trimmedRemovedMetals))
            {
                return;
            }

            if (trimmedRemovedMetals.Count != 1 ||
                !MultipleMeshesTrimmer.IsSubSetOf(meshesGoingToTrim.Keys, trimmedRemovedMetals.Keys)) 
            {
                return;
            }

            var remainedMetalTemp = roiCreationDataModel.Metal.IntegratedRemainedMetal;
            // It for trigger the "IsDirty" flag
            roiCreationDataModel.Metal.DefaultRemovedMetalOffset = roiCreationDataModel.Metal.DefaultRemovedMetalOffset;
            roiCreationDataModel.Metal.IntegratedRemovedMetal = trimmedRemovedMetals.Values.FirstOrDefault();
            roiCreationDataModel.Metal.IntegratedRemainedMetal = remainedMetalTemp;
            IDSPluginHelper.WriteLine(LogCategory.Default, "Trimmed removed metals");
        }
        
        private void TeethIntegration(ref ImplantSupportRoICreationDataModel roiCreationDataModel, RhinoDoc doc)
        {
            UnlockTeethParts(doc);

            List<Mesh> selection;
            List<Guid> selectionId;
            var commitChanges = SelectParts(doc, "Select teeth part(s) to integrate.", out selection, out selectionId);

            if (commitChanges)
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, $"Selected: {selection.Count} Parts");
                roiCreationDataModel.Teeth.SelectedTeethParts = selection;
            }
        }

        private void Preview(ref ImplantSupportRoICreationDataModel roiCreationDataModel, CMFImplantDirector director)
        {
            if (roiCreationDataModel.Teeth.Enable && roiCreationDataModel.Teeth.IntegratedTeeth == null)
            {
                if (!roiCreationDataModel.Teeth.SelectedTeethParts.Any())
                {
                    MessageBox.Show("No Teeth Part(s) selected!");
                    return;
                }

                var teethIntegrator = new ImplantSupportRoITeethPartsIntegrator();
                teethIntegrator.TeethParts.AddRange(roiCreationDataModel.Teeth.SelectedTeethParts);
                teethIntegrator.ResultingOffset = roiCreationDataModel.Teeth.Offset;

                IDSPluginHelper.WriteLine(LogCategory.Default, "Generating teeth integration mesh...");
                if (!teethIntegrator.Execute())
                {
                    MessageBox.Show("Teeth Integration failed!");
                    return;
                }

                roiCreationDataModel.Teeth.IntegratedTeeth = teethIntegrator.Result;
            }

            if(roiCreationDataModel.Metal.Enable && 
                roiCreationDataModel.Metal.IntegratedRemainedMetal == null && 
                roiCreationDataModel.Metal.IntegratedRemovedMetal == null)
            {
                if (!roiCreationDataModel.Metal.SelectedMetalInfos.Any())
                {
                    MessageBox.Show("No Metal Part(s) selected!");
                    return;
                }

                var removedMetalParts = roiCreationDataModel.Metal.SelectedMetalInfos
                    .Where(m => m.State == EMetalIntegrationState.Remove).Select(m => m.SelectedMesh).ToList();
                if (removedMetalParts.Any())
                {
                    var objectManager = new CMFObjectManager(director);
                    var constraintMeshQuery = new ConstraintMeshQuery(objectManager);
                    var plannedBoneParts = constraintMeshQuery.GetConstraintRhinoObjectForImplant()
                        .Where(b => b.Geometry is Mesh).Select(r => (Mesh)r.Geometry);

                    var removedMetalIntegrator = new ImplantSupportRoIRemovedMetalPartsIntegrator();
                    removedMetalIntegrator.BoneParts.AddRange(plannedBoneParts);
                    removedMetalIntegrator.MetalParts.AddRange(removedMetalParts);
                    removedMetalIntegrator.ResultingOffset = roiCreationDataModel.Metal.DefaultRemovedMetalOffset;

                    IDSPluginHelper.WriteLine(LogCategory.Default, "Generating removed metal integration mesh...");
                    if (!removedMetalIntegrator.Execute())
                    {
                        MessageBox.Show("Removed Metal Integration failed!");
                        return;
                    }

                    roiCreationDataModel.Metal.IntegratedRemovedMetal = removedMetalIntegrator.Result;
                }

                var remainedMetalParts = roiCreationDataModel.Metal.SelectedMetalInfos
                    .Where(m => m.State == EMetalIntegrationState.Remain).Select(m => m.SelectedMesh).ToList();
                if (remainedMetalParts.Any())
                {
                    var remainedMetalIntegrator = new ImplantSupportRoIRemainedMetalPartsIntegrator();
                    remainedMetalIntegrator.MetalParts.AddRange(remainedMetalParts);
                    remainedMetalIntegrator.ResultingOffset = roiCreationDataModel.Metal.DefaultRemainedMetalOffset;

                    IDSPluginHelper.WriteLine(LogCategory.Default, "Generating remained metal integration mesh...");
                    if (!remainedMetalIntegrator.Execute())
                    {
                        MessageBox.Show("Remained Metal Integration failed!");
                        return;
                    }

                    roiCreationDataModel.Metal.IntegratedRemainedMetal = remainedMetalIntegrator.Result;
                }
            }
        }

        private Result OnOK(ref ImplantSupportRoICreationDataModel roiCreationDataModel, out bool canExit)
        {
            canExit = true;

            //currently Preview only include teeth integration
            if ((roiCreationDataModel.Teeth.Enable && roiCreationDataModel.Teeth.IntegratedTeeth == null) ||
                (roiCreationDataModel.Metal.Enable && roiCreationDataModel.Metal.IntegratedRemainedMetal == null && roiCreationDataModel.Metal.IntegratedRemovedMetal == null))
            {
                MessageBox.Show("Please perform Preview!");
                canExit = false;
                return Result.Failure;
            }

            MessageBox.Show($"Metal, Enable: {(roiCreationDataModel.Metal.Enable?"Checked":"Unchecked")}, " +
                            $"Removed Offset: {roiCreationDataModel.Metal.DefaultRemovedMetalOffset:F1}; " +
                            $"Remained Offset: {roiCreationDataModel.Metal.DefaultRemainedMetalOffset:F1}\n" +
                            $"Teeth, Enable: {(roiCreationDataModel.Teeth.Enable ? "Checked" : "Unchecked")}, " +
                            $"Offset: {roiCreationDataModel.Teeth.Offset:F1}");
            
            return Result.Success;
        }

        private void UnlockTeethParts(RhinoDoc doc)
        {
            foreach (var obj in doc.Objects)
            {
                doc.Objects.Lock(obj.Id, true);
            }

            var rhObjs = GetPlannedParts(doc, ProPlanImportPartType.Teeth);
            foreach (var rhinoObject in rhObjs)
            {
                doc.Objects.Unlock(rhinoObject.Id, true);
            }
        }

        private List<RhinoObject> GetPlannedParts(RhinoDoc doc, ProPlanImportPartType partType)
        {
            var parts = new List<RhinoObject>();

            var rhObjs = ProPlanImportUtilities.GetAllPlannedLayerObjects(doc).Where(x => x.Geometry is Mesh && x.Name.Contains(IDS.CMF.Constants.ProPlanImport.ObjectPrefix));
            foreach (var rhinoObject in rhObjs)
            {
                if (ProPlanImportUtilities.IsPartAsRangePartType(new List<ProPlanImportPartType>() { partType }, doc.Layers[rhinoObject.Attributes.LayerIndex].Name))
                {
                    parts.Add(rhinoObject);
                }
            }

            return parts;
        }

        private bool SelectParts(RhinoDoc doc, string message, out List<Mesh> selection, out List<Guid> selectionId)
        {
            var selectParts = new GetObject();
            selectParts.SetCommandPrompt(message);
            selectParts.EnablePreSelect(false, false);
            selectParts.EnablePostSelect(true);
            selectParts.AcceptNothing(true);
            selectParts.EnableTransparentCommands(false);

            var commitChanges = false;
            selection = new List<Mesh>();
            selectionId = new List<Guid>();

            while (true)
            {
                var result = selectParts.GetMultiple(0, 0);

                if (result == GetResult.Cancel)
                {
                    break;
                }

                if (result == GetResult.Object || result == GetResult.Nothing)
                {
                    var selectedParts = doc.Objects.GetSelectedObjects(false, false).ToList();

                    if (!selectedParts.Any())
                    {
                        break;
                    }

                    foreach (var rhobj in selectedParts)
                    {
                        selection.Add((Mesh)rhobj.DuplicateGeometry());
                        selectionId.Add(rhobj.Id); 
                    }
                    commitChanges = true;
                    break;
                }
            }

            doc.Objects.UnselectAll();
            doc.Views.Redraw();

            return commitChanges;
        }

        private Result UpdateChanges(CMFImplantDirector director, ImplantSupportRoICreationDataModel roiCreationDataModel)
        {
            //currently Preview only include teeth integration
            if ((roiCreationDataModel.Teeth.Enable && roiCreationDataModel.Teeth.IntegratedTeeth == null) ||
                (roiCreationDataModel.Metal.Enable && roiCreationDataModel.Metal.IntegratedRemainedMetal == null && roiCreationDataModel.Metal.IntegratedRemovedMetal == null))
            {
                return Result.Failure;
            }

            var objectManager = new CMFObjectManager(director);
            var existingTeethIntegrationRoI = objectManager.GetBuildingBlockId(IBB.ImplantSupportTeethIntegrationRoI);
            if (roiCreationDataModel.Teeth.Enable)
            {
                objectManager.SetBuildingBlock(IBB.ImplantSupportTeethIntegrationRoI, roiCreationDataModel.Teeth.IntegratedTeeth, existingTeethIntegrationRoI);
            }
            else
            {
                if (objectManager.DeleteObject(existingTeethIntegrationRoI))
                {
                    MessageBox.Show("Teeth Integration RoI is removed.");
                }
            }

            var existingRemovedMetalIntegrationRoI = objectManager.GetBuildingBlockId(IBB.ImplantSupportRemovedMetalIntegrationRoI);
            if (roiCreationDataModel.Metal.Enable && roiCreationDataModel.Metal.IntegratedRemovedMetal != null)
            {
                objectManager.SetBuildingBlock(IBB.ImplantSupportRemovedMetalIntegrationRoI, roiCreationDataModel.Metal.IntegratedRemovedMetal, existingRemovedMetalIntegrationRoI);
            }
            else
            {
                if (objectManager.DeleteObject(existingRemovedMetalIntegrationRoI))
                {
                    MessageBox.Show("Removed Metal Integration RoI is removed.");
                }
            }

            var existingRemainedMetalIntegrationRoI = objectManager.GetBuildingBlockId(IBB.ImplantSupportRemainedMetalIntegrationRoI);
            if (roiCreationDataModel.Metal.Enable && roiCreationDataModel.Metal.IntegratedRemainedMetal != null)
            {
                objectManager.SetBuildingBlock(IBB.ImplantSupportRemainedMetalIntegrationRoI, roiCreationDataModel.Metal.IntegratedRemainedMetal, existingRemainedMetalIntegrationRoI);
            }
            else
            {
                if (objectManager.DeleteObject(existingRemainedMetalIntegrationRoI))
                {
                    MessageBox.Show("Remained Metal Integration RoI is removed.");
                }
            }

            director.ImplantManager.SetImplantSupportRoICreationInformation(roiCreationDataModel.GetData());

            if (roiCreationDataModel.IsDirty)
            {
                OutdatedImplantSupportHelper.SetAllImplantSupportsOutdated(director);
            }

            director.Document.ClearUndoRecords(true);
            director.Document.ClearRedoRecords();

            return Result.Success;
        }

        private void SetupDataModel(CMFImplantDirector director, ref ImplantSupportRoICreationDataModel roiCreationDataModel)
        {
            var objectManager = new CMFObjectManager(director);
            if (objectManager.HasBuildingBlock(IBB.ImplantSupportTeethIntegrationRoI))
            {
                var existingTeethIntegrationRoI = objectManager.GetBuildingBlock(IBB.ImplantSupportTeethIntegrationRoI);
                roiCreationDataModel.Teeth.IntegratedTeeth = (Mesh) existingTeethIntegrationRoI.Geometry;
            }

            if (objectManager.HasBuildingBlock(IBB.ImplantSupportRemovedMetalIntegrationRoI))
            {
                var existingRemovedMetalIntegrationRoI = objectManager.GetBuildingBlock(IBB.ImplantSupportRemovedMetalIntegrationRoI);
                roiCreationDataModel.Metal.IntegratedRemovedMetal = (Mesh)existingRemovedMetalIntegrationRoI.Geometry;
            }

            if (objectManager.HasBuildingBlock(IBB.ImplantSupportRemainedMetalIntegrationRoI))
            {
                var existingRemainedMetalIntegrationRoI = objectManager.GetBuildingBlock(IBB.ImplantSupportRemainedMetalIntegrationRoI);
                roiCreationDataModel.Metal.IntegratedRemainedMetal = (Mesh)existingRemainedMetalIntegrationRoI.Geometry;
            }
        }
    }
}