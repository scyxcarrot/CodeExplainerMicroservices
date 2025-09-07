using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.ScrewQc;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.PICMF.Forms;
using IDS.PICMF.Helper;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("748D92EB-6628-455C-ABA2-54517481BBF3")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Guide, IBB.RegisteredBarrel)]
    public class CMFSelectGuideBarrel : CmfCommandBase
    {
        internal class RegisteredBarrelInfo
        {
            public RhinoObject RegisteredBarrel;
            public Color OriginalColor;
            public Color CurrentColor;
            public bool IsSelected;
        }

        private readonly List<RegisteredBarrelInfo> _registeredBarrelInfos;
        private readonly Color _unassignedColor = Color.Yellow;

        public CMFSelectGuideBarrel()
        {
            TheCommand = this;
            VisualizationComponent = new CMFSelectGuideBarrelVisualization();
            _registeredBarrelInfos = new List<RegisteredBarrelInfo>();
        }
        
        public static CMFSelectGuideBarrel TheCommand { get; private set; }

        public CMFGuidePrefPanelVisualizationHelper GuidePrefPanelVisualizationHelper { get; } =
            new CMFGuidePrefPanelVisualizationHelper();
        
        public override string EnglishName => "CMFSelectGuideBarrel";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            CMFGuideScrewQcBubbleConduitProxy.Instance.TurnOff();
            var guideCaseGuid = GuidePreferencesHelper.PromptForPreferenceId();

            if (guideCaseGuid == Guid.Empty)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Guide preference not found!");
                return Result.Failure;
            }

            doc.UndoRecordingEnabled = false;

            _registeredBarrelInfos.Clear();

            var objectManager = new CMFObjectManager(director);
            var guidePrefModel = objectManager.GetGuidePreference(guideCaseGuid);
            
            GuidePrefPanelVisualizationHelper.GuidePrefPanelOpVisualization(guidePrefModel, doc, false, true);

            PrepareRegisteredBarrelsForSelection(director, guideCaseGuid);

            var selectRegisteredBarrels = new GetObject();
            selectRegisteredBarrels.SetCommandPrompt("Select/deselect registered barrel(s).");
            selectRegisteredBarrels.EnablePreSelect(false, false);
            selectRegisteredBarrels.EnablePostSelect(true);
            selectRegisteredBarrels.AcceptNothing(true);
            selectRegisteredBarrels.EnableTransparentCommands(false);
            selectRegisteredBarrels.EnableHighlight(false);

            var result = Result.Failure;

            while (true)
            {
                var res = selectRegisteredBarrels.GetMultiple(0, -1);

                if (res == GetResult.Cancel)
                {
                    result = Result.Cancel;
                    break;
                }
                else if (res == GetResult.Nothing)
                {
                    LinkUnlinkRegisteredBarrels(director, guidePrefModel);
                    result = Result.Success;
                    break;
                }
                else if (res == GetResult.Object)
                {
                    var selectedRegisteredBarrels = doc.Objects.GetSelectedObjects(false, false).ToList();

                    foreach (var registeredBarrel in selectedRegisteredBarrels)
                    {
                        if (_registeredBarrelInfos.Any(r => r.RegisteredBarrel.Id == registeredBarrel.Id))
                        {
                            var info = _registeredBarrelInfos.First(r => r.RegisteredBarrel.Id == registeredBarrel.Id);
                            info.IsSelected = !info.IsSelected;

                            var color = CasePreferencesHelper.GetColor(guidePrefModel.NCase);
                            if (!info.IsSelected)
                            {
                                color = _unassignedColor;
                            }

                            info.CurrentColor = color;
                            UpdateColor(info, color);
                        }
                    }
                }
            }

            return result;
        }

        private void PrepareRegisteredBarrelsForSelection(CMFImplantDirector director, Guid guideCaseGuid)
        {
            Locking.UnlockRegisteredBarrels(director.Document);

            var objectManager = new CMFObjectManager(director);
            var registeredBarrels = objectManager.GetAllBuildingBlocks(IBB.RegisteredBarrel);

            foreach (var registeredBarrel in registeredBarrels)
            {
                _registeredBarrelInfos.Add(new RegisteredBarrelInfo
                {
                    RegisteredBarrel = registeredBarrel,
                    OriginalColor = registeredBarrel.GetMaterial(true).AmbientColor,
                    CurrentColor = _unassignedColor,
                    IsSelected = false
                });
            }

            foreach (var guidePref in director.CasePrefManager.GuidePreferences)
            {
                var linkedRegisteredBarrels = RegisteredBarrelUtilities.GetLinkedRegisteredBarrels(director, guidePref);
                foreach (var registeredBarrelID in linkedRegisteredBarrels)
                {
                    if (!_registeredBarrelInfos.Any(r => r.RegisteredBarrel.Id == registeredBarrelID))
                    {
                        continue;
                    }

                    var info = _registeredBarrelInfos.First(r => r.RegisteredBarrel.Id == registeredBarrelID);
                    info.CurrentColor = CasePreferencesHelper.GetColor(guidePref.NCase);
                    if (guidePref.CaseGuid != guideCaseGuid)
                    {
                        director.Document.Objects.Lock(registeredBarrelID, true);
                    }
                    else
                    {
                        info.IsSelected = true;
                    }
                }
            }

            foreach (var registeredBarrelInfo in _registeredBarrelInfos)
            {
                UpdateColor(registeredBarrelInfo, registeredBarrelInfo.CurrentColor);
            }
        }

        private void LinkUnlinkRegisteredBarrels(CMFImplantDirector director, GuidePreferenceDataModel guidePrefModel)
        {
            var selectedRegisteredBarrelIds = _registeredBarrelInfos.Where(r => r.IsSelected).Select(r => r.RegisteredBarrel.Id).ToList();
            RegisteredBarrelUtilities.SetLinkedRegisteredBarrels(director, guidePrefModel, selectedRegisteredBarrelIds);
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, CMFImplantDirector director)
        {
            CleanUp(doc);

            director.GuidePhaseStarted = true;

            CasePreferencePanel.GetView().InvalidateUI();
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, CMFImplantDirector director)
        {
            GuidePrefPanelVisualizationHelper.RestoreVisualisation(doc, false);
            CleanUp(doc);
        }

        public override void OnCommandExecuteCanceled(RhinoDoc doc, CMFImplantDirector director)
        {
            GuidePrefPanelVisualizationHelper.RestoreVisualisation(doc, false);
            CleanUp(doc);
        }

        private void CleanUp(RhinoDoc doc)
        {
            foreach (var registeredBarrelInfo in _registeredBarrelInfos)
            {
                UpdateColor(registeredBarrelInfo, registeredBarrelInfo.OriginalColor);
            }

            _registeredBarrelInfos.Clear();
            doc.Objects.UnselectAll();
            doc.Views.Redraw();

            doc.UndoRecordingEnabled = true;
        }

        private void UpdateColor(RegisteredBarrelInfo info, Color color)
        {
            var mat = info.RegisteredBarrel.GetMaterial(true);
            mat.AmbientColor = color;
            mat.DiffuseColor = color;
            mat.SpecularColor = color;
            mat.CommitChanges();
        }
    }
}