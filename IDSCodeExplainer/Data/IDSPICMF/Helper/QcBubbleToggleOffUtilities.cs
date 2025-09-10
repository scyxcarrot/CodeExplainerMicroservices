using IDS.CMF;
using IDS.CMF.Constants;
using IDS.CMF.ScrewQc;
using IDS.Core.CommandBase;
using System.Collections.Generic;

namespace IDS.PICMF.Helper
{
    public static class QcBubbleToggleOffUtilities
    {
        /// <summary>
        /// Method to turn off qc bubble
        /// </summary>
        private static void TurnOffCmfImplantScrewQcBubbleConduitProxy()
        {
            var proxy = CMFImplantScrewQcBubbleConduitProxy.Instance;
            proxy.TurnOff();
        }

        /// <summary>
        /// Method to check if commandName passed need to turn off the qc bubble or not
        /// </summary>
        private static void TurnOffScrewQcBubbleOnCommandBeginExecute(string commandName)
        {
            var turnOffQcBubbleCommands = new List<string>
            {
                CommandEnglishName.CMFToggleTransparency,
                CommandEnglishName.CMFPlaceImplant,
                CommandEnglishName.CMFChangeScrewNumber,
                CommandEnglishName.CMFCreateLandmark,
                CommandEnglishName.CMFIndicateAnatObstacles,
                CommandEnglishName.CMFRemoveLandmark,
                CommandEnglishName.CMFToggleScrewInfoBubble,
                CommandEnglishName.CMFToggleScrewNumber
            };

            if (turnOffQcBubbleCommands.Contains(commandName))
            {
                TurnOffCmfImplantScrewQcBubbleConduitProxy();
            }
        }

        /// <summary>
        /// Method to check if commandName passed need to turn off the qc bubble or not
        /// </summary>
        private static void TurnOffScrewQcBubbleOnCommandSuccessfulExecuted(string commandName)
        {
            var turnOffQcBubbleCommands = new List<string>
            {
                CommandEnglishName.CMFStartPlanningPhase,
                CommandEnglishName.CMFStartPlanningQCPhase,
                CommandEnglishName.CMFStartGuidePhase,
                CommandEnglishName.CMFStartMetalQCPhase,
                CommandEnglishName.CMFImportImplantSupport,
                CommandEnglishName.CMFCreateImplantSupport,
                CommandEnglishName.CMFImportRecut,
                CommandEnglishName.CMFUpdatePlanning,
                CommandEnglishName.CMFSmartDesign,
                CommandEnglishName.CMFImplantPreview,
                CommandEnglishName.CMFPastillePreview,
            };

            if (turnOffQcBubbleCommands.Contains(commandName))
            {
                TurnOffCmfImplantScrewQcBubbleConduitProxy();
            }
        }

        /// <summary>
        /// Method to check if commandName passed need to reset the implant screw QC result or not
        /// </summary>
        private static void ResetImplantScrewQcResultOnCommandSuccessfulExecuted(CMFImplantDirector director, string commandName)
        {
            switch (commandName)
            {
                case CommandEnglishName.CMFImportImplantSupport:
                case CommandEnglishName.CMFCreateImplantSupport:
                case CommandEnglishName.CMFImportRecut:
                case CommandEnglishName.CMFUpdatePlanning:
                case CommandEnglishName.CMFSmartDesign:
                    director.ImplantScrewQcLiveUpdateHandler = null;
                    break;
            }
        }

        private static void OnCommandBeginExecuteSuccessfullyEvent(object sender, CommandCallbackEventArgs<CMFImplantDirector> e)
        {
            TurnOffScrewQcBubbleOnCommandBeginExecute(e.CommandName);
        }

        private static void OnCommandEndExecuteSuccessfullyEvent(object sender, CommandCallbackEventArgs<CMFImplantDirector> e)
        {
            ResetImplantScrewQcResultOnCommandSuccessfulExecuted(e.Director, e.CommandName);
            TurnOffScrewQcBubbleOnCommandSuccessfulExecuted(e.CommandName);
        }

        public static void SubscribeEvent()
        {
            CmfCommandBase.CommandBeginExecuteSuccessfullyEvent += OnCommandBeginExecuteSuccessfullyEvent;
            CmfCommandBase.CommandEndExecuteSuccessfullyEvent += OnCommandEndExecuteSuccessfullyEvent; 
        }
    }
}