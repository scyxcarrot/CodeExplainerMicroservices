using IDS.CMF.FileSystem;
using IDS.CMF.V2.Constants;
using IDS.CMF.V2.Logics;
using IDS.Core.Enumerators;
using IDS.Core.Operations;
using IDS.Core.PluginHelper;
using IDS.Interface.Logic;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Linq;

namespace IDS.CMF.Visualization
{
    public class BoneThicknessAnalysisIntegrateHelper: IBoneThicknessAnalysisIntegrateHelper
    {
        private readonly CMFImplantDirector _director;
        private RhinoObject _selectBone;

        public BoneThicknessAnalysisIntegrateHelper(CMFImplantDirector director, double minWallThickness, double maxWallThickness)
        {
            _director = director;
            CurrentMinWallThickness = minWallThickness;
            CurrentMaxWallThickness = maxWallThickness;
        }

        public LogicStatus PrepareLogicParameters(out BoneThicknessAnalysisIntegrateParameters parameters)
        {
            parameters = null;
            var doc = _director.Document;

            var handleUnlocking = new Action(() =>
            {
                Locking.LockAll(doc);

                var rhObjects = BoneThicknessAnalyzableObjectManager.GetBoneThicknessAnalyzableRhinoObjects(doc);
                rhObjects.ForEach(x => doc.Objects.Unlock(x.Id, true));
            });

            var handleOnPlannedLayerChanged = new EventHandler<Rhino.DocObjects.Tables.LayerTableEventArgs>((s, e) =>
            {
                handleUnlocking();
            });

            RhinoDoc.LayerTableEvent += handleOnPlannedLayerChanged;

            var selectEntities = new GetObject();
            selectEntities.EnablePreSelect(false, false);
            selectEntities.EnablePostSelect(true);
            selectEntities.AcceptNothing(true);
            selectEntities.EnableTransparentCommands(false);
            selectEntities.EnableHighlight(false);
            
            while (true)
            {
                selectEntities.ClearCommandOptions();
                var minMaxWallThicknessFinal = (CurrentMaxWallThickness - BoneThicknessAnalysisConstants.MinGap < BoneThicknessAnalysisConstants.MaxMinWallThickness)
                    ? CurrentMaxWallThickness - BoneThicknessAnalysisConstants.MinGap
                    : BoneThicknessAnalysisConstants.MaxMinWallThickness;
                var minWallThickness = new OptionDouble(CurrentMinWallThickness, BoneThicknessAnalysisConstants.MinMinWallThickness, minMaxWallThicknessFinal);
                var minOptionIndex = selectEntities.AddOptionDouble("MinimumWallThickness", ref minWallThickness,
                    $"Set parameters, minimum: {BoneThicknessAnalysisConstants.MinMinWallThickness}, maximum: {minMaxWallThicknessFinal}");

                var maxMinWallThicknessFinal = (CurrentMinWallThickness + BoneThicknessAnalysisConstants.MinGap > BoneThicknessAnalysisConstants.MinMaxWallThickness)
                    ? CurrentMinWallThickness + BoneThicknessAnalysisConstants.MinGap
                    : BoneThicknessAnalysisConstants.MinMaxWallThickness;
                var maxWallThickness = new OptionDouble(CurrentMaxWallThickness, maxMinWallThicknessFinal, BoneThicknessAnalysisConstants.MaxMaxWallThickness);
                var maxOptionIndex = selectEntities.AddOptionDouble("MaximumWallThickness", ref maxWallThickness,
                    $"Set parameters, minimum: {maxMinWallThicknessFinal}, maximum: {BoneThicknessAnalysisConstants.MaxMaxWallThickness}");

                selectEntities.SetCommandPrompt($"Select Anatomy Part to perform analysis, minimum bone thickness = {CurrentMinWallThickness}, maximum bone thickness = {CurrentMaxWallThickness}");

                handleUnlocking();
                var res = selectEntities.Get();

                if (res == GetResult.Object)
                {
                    var selectedObj = doc.Objects.GetSelectedObjects(false, false).FirstOrDefault();
                    if (selectedObj != null)
                    {

                        if (selectedObj.Geometry is Mesh)
                        {
                            _selectBone = selectedObj;
                            _director.Document.Views.Redraw();
                            break;
                        }

                        IDSPluginHelper.WriteLine(LogCategory.Error, "Unsupported object type! Please select a Mesh type object.");
                        continue;
                    }
                }

                //To exit from changing the lower and upper boundary
                if (res == GetResult.Nothing)
                {
                    _director.Document.Views.Redraw();
                    RhinoDoc.LayerTableEvent -= handleOnPlannedLayerChanged;
                    return LogicStatus.Success;
                }

                if (res == GetResult.Cancel)
                {
                    RhinoDoc.LayerTableEvent -= handleOnPlannedLayerChanged;
                    return LogicStatus.Cancel;
                }

                if (res == GetResult.Option)
                {
                    if (selectEntities.OptionIndex() == minOptionIndex)
                    {
                        CurrentMinWallThickness = Math.Round(minWallThickness.CurrentValue, 2, MidpointRounding.AwayFromZero);
                    }
                    else if (selectEntities.OptionIndex() == maxOptionIndex)
                    {
                        CurrentMaxWallThickness = Math.Round(maxWallThickness.CurrentValue, 2, MidpointRounding.AwayFromZero);
                    }
                }
            }

            RhinoDoc.LayerTableEvent -= handleOnPlannedLayerChanged;
            return LogicStatus.Success;
        }

        public LogicStatus ProcessLogicResult(BoneThicknessAnalysisIntegrateResult result)
        {
            var resources = new CMFResources();
            var displayModeSettingsFile = resources.IdsCmfSettingsFile;
            RhinoApp.RunScript($"-_OptionsImport \"{displayModeSettingsFile}\" AdvDisplay=Yes Display=Yes _Enter", false);

            return LogicStatus.Success;
        }

        public double CurrentMinWallThickness { get; private set; }

        public double CurrentMaxWallThickness { get; private set; }

        public IBoneThicknessAnalysisGenerationHelper GetBoneAnalysisGenerationHelper()
        {
            return new BoneThicknessAnalysisGenerationHelper(_director, _selectBone);
        }

        public IBoneThicknessAnalysisReportingHelper GetBoneThicknessAnalysisReportingHelper(double[] thicknessData)
        {
            return new BoneThicknessAnalysisReportingHelper(_director, _selectBone, thicknessData, CurrentMinWallThickness, CurrentMaxWallThickness);
        }
    }
}
