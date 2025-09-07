using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.PICMF.Forms;
using IDS.PICMF.Helper;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Display;
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
    [System.Runtime.InteropServices.Guid("4058CF3A-BD63-4D55-81E8-8772E90C6AC4")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Guide)]
    public class CMFCreateGuideSupportRoI : CmfCommandBase
    {
        private enum RoICategory
        {
            RoI,
            Metal,
            Teeth
        }

        public CMFCreateGuideSupportRoI()
        {
            TheCommand = this;
            VisualizationComponent = new CMFCreateGuideSupportRoIVisualizationComponent();
        }
        
        public static CMFCreateGuideSupportRoI TheCommand { get; private set; }
        
        public override string EnglishName => "CMFCreateGuideSupportRoI";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var guideManager = director.GuideManager;
            var generalPreOpParts = ProPlanImportUtilities.GetGeneralPreopParts(doc);

            var dataModel = guideManager.GetGuideSupportRoICreationDataModel();
            var roiCreationDataModel = new GuideSupportRoICreationDataModel(dataModel);

            var guideRoiDrawingManager = new GuideRoiDrawingManager(director);
            var drawnRoIDictionary = guideRoiDrawingManager.GetDrawnRoIs();
            if (drawnRoIDictionary.Any())
            {
                roiCreationDataModel.RoI.DrawnRoI = MeshUtilities.AppendMeshes(drawnRoIDictionary.Values);
            }

            var roICreationDialog = new GuideSupportRoICreationDialog(roiCreationDataModel) { Topmost = true };
            roICreationDialog.Show();

            var getOption = new GetOption();
            getOption.SetCommandPrompt("Choose the operation for define RoI");

            var drawRoi = getOption.AddOption("DrawRoI");
            var metalIntegrate = getOption.AddOption("MetalIntegration");
            var teethIntegrate = getOption.AddOption("TeethIntegration");
            var preview = getOption.AddOption("Preview");
            var ok = getOption.AddOption("OK");
            var cancel = getOption.AddOption("Cancel");

            getOption.EnableTransparentCommands(false);
            var res = Result.Cancel;
            Exception exception = null;
            var conduit = new GuideSupportRoIConduit(roiCreationDataModel) {Enabled = true};
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
                    if (optionSelected == drawRoi)
                    {
                        VisualizationComponent.OnCommandBeginVisualization(doc);
                        DrawRoI(ref roiCreationDataModel, director, ref drawnRoIDictionary);
                        doc.Views.Redraw();
                    }
                    else if (optionSelected == metalIntegrate)
                    {
                        ((CMFCreateGuideSupportRoIVisualizationComponent)VisualizationComponent).OnMetalButtonClicked(doc);
                        MetalIntegration(ref roiCreationDataModel, doc);
                    }
                    else if (optionSelected == teethIntegrate)
                    {
                        ((CMFCreateGuideSupportRoIVisualizationComponent)VisualizationComponent).OnTeethButtonClicked(doc);
                        TeethIntegration(ref roiCreationDataModel, doc);
                    }
                    else if (optionSelected == preview)
                    {
                        conduit.Enabled = false;
                        doc.Views.Redraw();
                        Preview(ref roiCreationDataModel, ref drawnRoIDictionary);
                        conduit.Enabled = true;
                        ((CMFCreateGuideSupportRoIVisualizationComponent)VisualizationComponent).OnPreviewButtonClicked(doc);
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
                guideRoiDrawingManager.UpdateDrawnRoIs(drawnRoIDictionary);
            }

            return res;
        }

        private bool DrawRoI(ref GuideSupportRoICreationDataModel roiCreationDataModel, CMFImplantDirector director, ref Dictionary<Guid, Mesh> drawnRoIDictionary)
        {
            var dictionary = new Dictionary<Guid, Mesh>(drawnRoIDictionary);
            var canContinue = true;

            do
            {
                canContinue = GetSingleHighDefinitionMesh(director, out var constraintMesh, out var iD);
                if (constraintMesh == null)
                {
                    if (canContinue)
                    {
                        break;
                    }
                    else
                    {
                        IDSPluginHelper.WriteLine(LogCategory.Error, "All changes discarded!");
                        return false;
                    }
                }

                var drawGuideSupportRoI = new DrawGuideSupportRoIOnPlane(director.Document, constraintMesh);
                if (!drawGuideSupportRoI.Execute())
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, "Changes discarded!");
                    director.Document.Views.Redraw();
                    continue;
                }

                if (dictionary.ContainsKey(iD))
                {
                    dictionary[iD] = drawGuideSupportRoI.RoIMesh;
                }
                else
                {
                    dictionary.Add(iD, drawGuideSupportRoI.RoIMesh);
                }
            }
            while (canContinue);

            drawnRoIDictionary.Clear();
            drawnRoIDictionary = new Dictionary<Guid, Mesh>(dictionary);
            dictionary.Clear();

            if (drawnRoIDictionary.Any())
            {
                roiCreationDataModel.RoI.DrawnRoI = MeshUtilities.AppendMeshes(drawnRoIDictionary.Values);
            }
            else
            {
                roiCreationDataModel.RoI.DrawnRoI = null;
                IDSPluginHelper.WriteLine(LogCategory.Error, "No roi drawn!");
            }

            return true;
        }

        protected bool GetSingleHighDefinitionMesh(CMFImplantDirector director, out Mesh selectedMesh, out Guid iD)
        {
            var doc = director.Document;

            UnlockBoneParts(doc);

            var selectPart = new GetObject();
            selectPart.SetCommandPrompt("Select a bone part to be included in RoI. Press <Enter> to finalize draw RoI, <Esc> to discard changes.");
            selectPart.EnablePreSelect(false, false);
            selectPart.EnablePostSelect(true);
            selectPart.AcceptNothing(true);
            selectPart.EnableTransparentCommands(false);

            var result = selectPart.Get();

            selectedMesh = null;
            iD = Guid.Empty;

            if (result == GetResult.Nothing)
            {
                return true;
            }
            else if (result == GetResult.Cancel)
            {
                return false;
            }

            var selectedPart = doc.Objects.GetSelectedObjects(false, false).FirstOrDefault();
            if (selectedPart != null)
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, $"Selected: {selectedPart.Name}");
                selectedMesh = (Mesh)selectedPart.DuplicateGeometry();
                iD = selectedPart.Id;
            }

            doc.Objects.UnselectAll();
            doc.Views.Redraw();

            return true;
        }

        private void MetalIntegration(ref GuideSupportRoICreationDataModel roiCreationDataModel, RhinoDoc doc)
        {
            UnlockMetalParts(doc);

            List<Mesh> selection;
            List<Guid> selectionId;
            var commitChanges = SelectParts(doc, "Select metal part(s) to integrate.", out selection, out selectionId);

            if (commitChanges)
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, $"Selected: {selection.Count} Parts");
                roiCreationDataModel.Metal.SelectedMetalParts = selection;
                roiCreationDataModel.Metal.SelectedMetalPartIds = selectionId;
            }
        }

        private void TeethIntegration(ref GuideSupportRoICreationDataModel roiCreationDataModel, RhinoDoc doc)
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

        private void Preview(ref GuideSupportRoICreationDataModel roiCreationDataModel, ref Dictionary<Guid, Mesh> drawnRoIDictionary)
        {
            var inputs = new Dictionary<Guid, Mesh>();
            var relationshipMap = new Dictionary<Guid, RoICategory>();
            var remainMaterialsMap = new Dictionary<Guid, DisplayMaterial>(); 
            var roiRemainMaterial = GuideSupportRoIConduit.CreateRoIMaterial();
            var metalRemainMaterial = GuideSupportRoIConduit.CreateMetalMaterial();
            var teethRemainMaterial = GuideSupportRoIConduit.CreateTeethMaterial();
            var preprocessModes = new Dictionary<Guid, DisjointedShellEditor.PreprocessMode>();

            if (roiCreationDataModel.RoI.DrawnRoI == null)
            {
                MessageBox.Show("No RoI defined!");
                return;
            }

            foreach (var drawnRoIInfo in drawnRoIDictionary)
            {
                inputs.Add(drawnRoIInfo.Key, drawnRoIInfo.Value);
                relationshipMap.Add(drawnRoIInfo.Key, RoICategory.RoI);
                remainMaterialsMap.Add(drawnRoIInfo.Key, roiRemainMaterial);
                preprocessModes.Add(drawnRoIInfo.Key, DisjointedShellEditor.PreprocessMode.Preprocess);
            }

            if (roiCreationDataModel.Metal.Enable)
            {
                if (!roiCreationDataModel.Metal.SelectedMetalParts.Any())
                {
                    MessageBox.Show("No Formal Metal Part(s) selected!");
                    return;
                }

                var metalIntegrator = new GuideSupportRoIMetalPartsIntegrator();
                metalIntegrator.AdditionalGuideRoIBaseParts.Add(roiCreationDataModel.RoI.DrawnRoI);
                metalIntegrator.MetalParts.AddRange(roiCreationDataModel.Metal.SelectedMetalParts);
                metalIntegrator.ResultingOffset = roiCreationDataModel.Metal.Offset;

                IDSPluginHelper.WriteLine(LogCategory.Default, "Generating metal integration mesh...");
                if (!metalIntegrator.Execute())
                {
                    MessageBox.Show("Metal Integration failed!"); 
                    return;
                }

                roiCreationDataModel.Metal.IntegratedMetal = metalIntegrator.Result;
                var id = Guid.NewGuid();
                inputs.Add(id, roiCreationDataModel.Metal.IntegratedMetal);
                relationshipMap.Add(id, RoICategory.Metal);
                remainMaterialsMap.Add(id, metalRemainMaterial);
                preprocessModes.Add(id, DisjointedShellEditor.PreprocessMode.SelectAll);
            }

            if (roiCreationDataModel.Teeth.Enable)
            {
                if (!roiCreationDataModel.Teeth.SelectedTeethParts.Any())
                {
                    MessageBox.Show("No Teeth Part(s) selected!");
                    return;
                }

                var teethIntegrator = new GuideSupportRoITeethPartsIntegrator();
                teethIntegrator.AdditionalGuideRoIBaseParts.Add(roiCreationDataModel.RoI.DrawnRoI);
                teethIntegrator.TeethParts.AddRange(roiCreationDataModel.Teeth.SelectedTeethParts);
                teethIntegrator.ResultingOffset = roiCreationDataModel.Teeth.Offset;

                IDSPluginHelper.WriteLine(LogCategory.Default, "Generating teeth integration mesh...");
                if (!teethIntegrator.Execute())
                {
                    MessageBox.Show("Teeth Integration failed!");
                    return;
                }

                roiCreationDataModel.Teeth.IntegratedTeeth = teethIntegrator.Result;
                var id = Guid.NewGuid();
                inputs.Add(id, roiCreationDataModel.Teeth.IntegratedTeeth);
                relationshipMap.Add(id, RoICategory.Teeth);
                remainMaterialsMap.Add(id, teethRemainMaterial);
                preprocessModes.Add(id, DisjointedShellEditor.PreprocessMode.SelectAll);
            }

            var removeNoiseDisjointedShellEditor = new RemoveNoiseDisjointedShellEditor();
            var outputs = removeNoiseDisjointedShellEditor.Execute(inputs, remainMaterialsMap, null, preprocessModes);
            removeNoiseDisjointedShellEditor.CleanUp();
            
            remainMaterialsMap.Clear();
            roiRemainMaterial.Dispose();
            metalRemainMaterial.Dispose();
            teethRemainMaterial.Dispose();

            var newDrawnRoIDictionary = new Dictionary<Guid, Mesh>();
            var newMetalIntegration = new List<Mesh>();
            var newTeethIntegration = new List<Mesh>();
            
            foreach (var output in outputs.Where(output => relationshipMap.ContainsKey(output.Key)))
            {
                switch (relationshipMap[output.Key])
                {
                    case RoICategory.RoI:
                        newDrawnRoIDictionary.Add(output.Key, output.Value);
                        break;
                    case RoICategory.Metal:
                        newMetalIntegration.Add(output.Value);
                        break;
                    case RoICategory.Teeth:
                        newTeethIntegration.Add(output.Value);
                        break;
                }
            }

            drawnRoIDictionary = newDrawnRoIDictionary;
            roiCreationDataModel.RoI.DrawnRoI = MeshUtilities.AppendMeshes(newDrawnRoIDictionary.Values);
            roiCreationDataModel.Metal.IntegratedMetal = MeshUtilities.AppendMeshes(newMetalIntegration);
            roiCreationDataModel.Teeth.IntegratedTeeth = MeshUtilities.AppendMeshes(newTeethIntegration);

            var finalMesh = MeshUtilities.AppendMeshes(new List<Mesh>()
            {
                roiCreationDataModel.RoI.DrawnRoI, 
                roiCreationDataModel.Metal.IntegratedMetal,
                roiCreationDataModel.Teeth.IntegratedTeeth
            });
            roiCreationDataModel.RoIPreview = finalMesh;
        }

        private Result OnOK(ref GuideSupportRoICreationDataModel roiCreationDataModel, out bool canExit)
        {
            canExit = true;

            if (roiCreationDataModel.RoIPreview == null)
            {
                MessageBox.Show("Please perform Preview!");
                canExit = false;
                return Result.Failure;
            }

            MessageBox.Show($"Metal, Enable: {(roiCreationDataModel.Metal.Enable?"Checked":"Unchecked")} " +
                            $"Offset: {roiCreationDataModel.Metal.Offset:F1}\n" +
                            $"Teeth, Enable: {(roiCreationDataModel.Teeth.Enable ? "Checked" : "Unchecked")} " +
                            $"Offset: {roiCreationDataModel.Teeth.Offset:F1}");
            
            return Result.Success;
        }

        private void UnlockBoneParts(RhinoDoc doc)
        {
            UnlockGeneralPreopParts(doc);
        }

        private void UnlockMetalParts(RhinoDoc doc)
        {
            UnlockGeneralPreopParts(doc);
        }

        private void UnlockTeethParts(RhinoDoc doc)
        {
            UnlockGeneralPreopParts(doc);
        }

        private void UnlockGeneralPreopParts(RhinoDoc doc)
        {
            foreach (var obj in doc.Objects)
            {
                doc.Objects.Lock(obj.Id, true);
            }

            var rhObjs = ProPlanImportUtilities.GetGeneralPreopParts(doc);
            foreach (var rhinoObject in rhObjs)
            {
                doc.Objects.Unlock(rhinoObject.Id, true);
            }
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

        private Result UpdateChanges(CMFImplantDirector director, GuideSupportRoICreationDataModel roiCreationDataModel)
        {
            if (roiCreationDataModel.RoIPreview == null)
            {
                return Result.Failure;
            }

            var objectManager = new CMFObjectManager(director);
            var existingGuideSupportRoI = objectManager.GetBuildingBlockId(IBB.GuideSupportRoI);
            objectManager.SetBuildingBlock(IBB.GuideSupportRoI, roiCreationDataModel.RoIPreview, existingGuideSupportRoI);

            if (roiCreationDataModel.Metal.IntegratedMetal != null)
            {
                var existingRemovedMetalIntegrationId = objectManager.GetBuildingBlockId(IBB.GuideSupportRemovedMetalIntegrationRoI);
                var guid = objectManager.SetBuildingBlock(IBB.GuideSupportRemovedMetalIntegrationRoI, roiCreationDataModel.Metal.IntegratedMetal, existingRemovedMetalIntegrationId);
                objectManager.SetGuideSupportRemovedMetalIntegrationRoISelection(guid, roiCreationDataModel.Metal.SelectedMetalPartIds);
            }
            else 
            {
                var existingRemovedMetalIntegration = objectManager.GetBuildingBlock(IBB.GuideSupportRemovedMetalIntegrationRoI);
                if (existingRemovedMetalIntegration != null)
                {
                    objectManager.DeleteObject(existingRemovedMetalIntegration.Id);
                }
            }

            director.GuideManager.SetGuideSupportRoICreationInformation(roiCreationDataModel.GetData());

            return Result.Success;
        }
    }
}