using IDS.Core.ImplantDirector;
using IDS.Core.PluginHelper;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.UI;
using System.Linq;

namespace IDS.Core.Operations
{
    public static class EntitiesDeleter
    {
        public static bool DeleteEntities(string commandPromptMessage, string dialogBoxMessage, string dialogBoxTitle, IImplantDirector director)
        {
            var select = new GetObject();
            select.SetCommandPrompt(commandPromptMessage);
            select.EnablePreSelect(false, false);
            select.EnablePostSelect(true);
            select.AcceptNothing(true);
            select.EnableTransparentCommands(false);

            var objectManager = new ObjectManager(director);

            // Get user input
            while (true)
            {
                var res = select.GetMultiple(0, 0);

                if (res == GetResult.Cancel || res == GetResult.Nothing)
                {
                    return false;
                }
                if (res != GetResult.Object)
                {
                    continue;
                }

                // Ask confirmation and delete if user clicks 'Yes'
                var result = Dialogs.ShowMessage(dialogBoxMessage, dialogBoxTitle, ShowMessageButton.YesNoCancel, ShowMessageIcon.Exclamation);
                if (result == ShowMessageResult.Yes)
                {
                    // Get selected objects
                    var selected = director.Document.Objects.GetSelectedObjects(false, false).ToList();
                    // Delete one by one (including dependencies)
                    foreach (var rhobj in selected)
                    {
                        objectManager.DeleteObject(rhobj.Id);
                    }

                    // Stop user input
                    break;
                }
                if (result == ShowMessageResult.Cancel)
                {
                    return false;
                }
            }
            
            return true;
        }
    }
}