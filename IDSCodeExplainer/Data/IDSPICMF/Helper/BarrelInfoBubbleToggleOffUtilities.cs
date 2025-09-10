using IDS.CMF;
using IDS.CMF.Constants;
using IDS.Core.CommandBase;
using System.Collections.Generic;
using IDS.CMF.Visualization;

namespace IDS.PICMF.Helper
{
    public static class BarrelInfoBubbleToggleOffUtilities
    {
        /// <summary>
        /// Method to turn off barrel info bubble
        /// </summary>
        private static void TurnOffCMFToggleBarrelInfoBubbleConduitProxy()
        {
            var proxy = BarrelInfoConduitProxy.GetInstance();
            proxy.Reset();
        }

        /// <summary>
        /// Method to check if commandName passed need to turn off the barrel info bubble or not
        /// </summary>
        private static void TurnOffBarrelInfoBubbleOnCommandSuccessfulExecuted(string commandName)
        {
            var turnOffBarrelInfoBubbleCommands = new List<string>
            {
                CommandEnglishName.CMFStartPlanningPhase,
                CommandEnglishName.CMFStartPlanningQCPhase,
                CommandEnglishName.CMFStartImplantPhase,
                CommandEnglishName.CMFStartMetalQCPhase,
                CommandEnglishName.CMFOverrideBarrelType,
            };

            if (turnOffBarrelInfoBubbleCommands.Contains(commandName))
            {
                TurnOffCMFToggleBarrelInfoBubbleConduitProxy();
            }
        }

        private static void OnCommandEndExecuteSuccessfullyEvent(object sender, CommandCallbackEventArgs<CMFImplantDirector> e)
        {
            TurnOffBarrelInfoBubbleOnCommandSuccessfulExecuted(e.CommandName);
        }

        public static void SubscribeEvent()
        {
            CmfCommandBase.CommandEndExecuteSuccessfullyEvent += OnCommandEndExecuteSuccessfullyEvent; 
        }

        public static void UnsubscribeEvent()
        {
            CmfCommandBase.CommandEndExecuteSuccessfullyEvent -= OnCommandEndExecuteSuccessfullyEvent;
        }
    }
}