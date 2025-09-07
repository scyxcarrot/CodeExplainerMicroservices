using IDS.Core.PluginHelper;
using Rhino.Commands;
using Rhino.UI;

namespace IDS.Core.Plugin
{
    public static class IDSDialogHelper
    {
        public static ShowMessageResult ShowMessage(string message,
            string title,
            ShowMessageButton buttons,
            ShowMessageIcon icon,
            RunMode mode,
            ShowMessageResult defaultAnswer)
        {
            return mode == RunMode.Scripted ? defaultAnswer : Dialogs.ShowMessage(message, title, buttons, icon);
        }

        public static void ShowSuppressibleMessage(string message,
            string title,
            ShowMessageIcon icon)
        {
            var resources = new Resources();
            if (!resources.SuppressPromptMessage)
            {
                Dialogs.ShowMessage(message, title, ShowMessageButton.OK, icon);
            }
        }

        public static ShowMessageResult ShowYesNoMessage(string message,string title, RunMode mode)
        {
            return ShowMessage(message, title, ShowMessageButton.YesNo, ShowMessageIcon.Exclamation, mode, ShowMessageResult.Yes);
        }

        public static ShowMessageResult ShowExportAdditionalEntitiesConfirmationMessage(RunMode mode, string additionalEntities)
        {
            return ShowYesNoMessage($"Do you want to export {additionalEntities}?", $"Export {additionalEntities}", mode);
        }
    }
}
