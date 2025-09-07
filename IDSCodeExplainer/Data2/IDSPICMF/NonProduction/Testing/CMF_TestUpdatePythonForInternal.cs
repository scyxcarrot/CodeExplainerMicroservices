#if (STAGING)
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IDS.Core.Http;
using IDS.PICMF.Forms;
using IDS.PICMF.Helper;
using Rhino;
using Rhino.Commands;
using Rhino.Input.Custom;

namespace IDSPICMF.NonProduction
{
    [System.Runtime.InteropServices.Guid("9773262b-fa1c-4af5-8583-dc62f08fc817")]
    [CommandStyle(Style.ScriptRunner)]
    public class CMF_TestUpdatePythonForInternal: Command
    {
        public CMF_TestUpdatePythonForInternal()
        {
            Instance = this;
        }

        public static CMF_TestUpdatePythonForInternal Instance { get; private set; }

        public override string EnglishName => "CMF_TestUpdatePythonForInternal";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {

            var helper = new UpdateHelper();

            // Fetch versions for Python and SmartDesign
            var pythonVersions = GetFolderNames(helper.CheckPythonVersions());
            var smartDesignVersions = GetFolderNames(helper.CheckSmartDesignVersions());

            // Prompt user to select a Python version
            var selectedPython = PromptUserToSelect(pythonVersions, "Select a Python Version");
            if (string.IsNullOrEmpty(selectedPython))
            {
                return Result.Cancel;
            }

            // Prompt user to select a SmartDesign version
            var selectedSmartDesign = PromptUserToSelect(smartDesignVersions, "Select a SmartDesign Version");
            if (string.IsNullOrEmpty(selectedSmartDesign))
            {
                return Result.Cancel;
            }

            // Update the Python and SmartDesign
            helper.UpdatePythonForInternal(selectedPython, selectedSmartDesign);

            return Result.Success;
        }

        private List<string> GetFolderNames(FilesResponseModel versionList)
        {
            return versionList.Files
                .Where(v => v.IsFolder)
                .Select(v => Path.GetFileName(v.Uri))
                .ToList();
        }

        private string PromptUserToSelect(List<string> options, string prompt)
        {
            var dialog = new ListViewSelector(options, prompt);
            dialog.Show();

            var getOption = new GetOption();
            getOption.SetCommandPrompt(prompt);
            var selectOption = getOption.AddOption("Selected");
            var cancelOption = getOption.AddOption("Cancel");
            getOption.EnableTransparentCommands(false);

            while (true)
            {
                getOption.Get();

                var option = getOption.Option();
                if (option == null)
                {
                    continue;
                }

                if (option.Index == cancelOption)
                {
                    dialog.IsEnabled = false;
                    dialog.Close();
                    return null; // Cancel selected
                }

                if (option.Index == selectOption)
                {
                    if (dialog.SelectedValue == null)
                    {
                        dialog.IsEnabled = false;
                        dialog.Close();
                        return null; // Failure case
                    }

                    dialog.IsEnabled = false;
                    dialog.Close();
                    return dialog.SelectedValue; // Selected value
                }
            }
        }

    }
}

#endif
