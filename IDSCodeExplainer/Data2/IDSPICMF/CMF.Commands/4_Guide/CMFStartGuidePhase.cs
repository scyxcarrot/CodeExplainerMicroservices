using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Constants;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Relations;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.PICMF.Forms;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.UI;
using Style = Rhino.Commands.Style;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("8948B12A-77A0-44FF-B438-16335994B905")]
    [IDSCMFCommandAttributes(~DesignPhase.Draft)]
    [CommandStyle(Style.ScriptRunner)]
    public class CMFStartGuidePhase : CmfCommandBase
    {
        public CMFStartGuidePhase()
        {
            TheCommand = this;
            VisualizationComponent = new CMFStartGuidePhaseVisualization();
        }

        public static CMFStartGuidePhase TheCommand { get; private set; }

        public override string EnglishName => CommandEnglishName.CMFStartGuidePhase;

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var firstTime = !director.GuidePhaseStarted;
            if (firstTime)
            {
                var result = Dialogs.ShowMessage("Registered barrels will be selected based on implant number-guide number. Are you sure you want to proceed?",
                    "Default registered barrels selection",
                    ShowMessageButton.YesNo,
                    ShowMessageIcon.Exclamation);
                if (result == ShowMessageResult.No)
                {
                    return Result.Cancel;
                }
            }

            // Define target phase
            var targetPhase = DesignPhase.Guide;
            var success = PhaseChanger.ChangePhase(director, targetPhase);
            if (!success)
            {
                return Result.Failure;
            }

            var objManager = new CMFObjectManager(director);

            Mesh guideSupport = null;
            if (objManager.HasBuildingBlock(IBB.GuideSupport))
            {
                guideSupport = (Mesh)objManager.GetBuildingBlock(IBB.GuideSupport).Geometry;
            }

            var screwBarrelRegistration = new CMFBarrelRegistrator(director);
            if (!screwBarrelRegistration.RegisterOnlyNewGuideRegisteredBarrel(guideSupport, out bool areAllBarrelsMeetingSpecs))
            {
                screwBarrelRegistration.Dispose();
                return Result.Failure;
            }

            if (areAllBarrelsMeetingSpecs)
            {
                IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"New barrels meet specification.");
            }

            if (firstTime)
            {
                RegisteredBarrelUtilities.SetDefaultLinkedImplantScrews(director);
            }

            CasePreferencePanel.OpenPanel();
            CasePreferencePanel.GetView().SetToGuidePhasePreset();
            CasePreferencePanel.GetView().InvalidateUI();

            screwBarrelRegistration.Dispose();
            return Result.Success;
        }
    }
}