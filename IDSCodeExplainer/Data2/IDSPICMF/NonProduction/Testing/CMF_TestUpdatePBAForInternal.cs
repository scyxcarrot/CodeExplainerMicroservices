#if (STAGING)

using System.IO;
using System.Linq;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.PICMF.Forms;
using IDS.PICMF.Helper;
using Rhino;
using Rhino.Commands;
using Rhino.Input.Custom;

namespace IDS.PICMF.NonProduction
{
    [System.Runtime.InteropServices.Guid("C76041A5-2E22-48F3-BA6E-D3D97A88DBA0")]
    [CommandStyle(Style.ScriptRunner)]
    public class CMF_TestUpdatePBAForInternal : Command
    {
        public CMF_TestUpdatePBAForInternal()
        {
            Instance = this;
        }

        public static CMF_TestUpdatePBAForInternal Instance { get; private set; }

        public override string EnglishName => "CMF_TestUpdatePBAForInternal";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var helper = new UpdateHelper();
            var versions = helper.CheckSmartDesignVersions();

            if (versions.Files == null)
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "Repo does not have any PBA packages (Or server is down).");
                return Result.Failure;
            }

            // PBAs are saved in a file
            var versionNum = versions.Files.Where(v => v.IsFolder).Select(v => Path.GetFileName(v.Uri)).ToList();

            var dialog = new ListViewSelector(versionNum, "Select PBA Version");
            dialog.Show();

            var getOption = new GetOption();
            getOption.SetCommandPrompt("Select package version");
            var selected = getOption.AddOption("Selected");
            var cancel = getOption.AddOption("Cancel");
            getOption.EnableTransparentCommands(false);

            while (true)
            {
                getOption.Get();

                var option = getOption.Option();
                if (option == null)
                {
                    continue;
                }

                var optionSelected = option.Index;

                if (optionSelected == cancel)
                {
                    dialog.IsEnabled = false;
                    dialog.Close();
                    return Result.Cancel;
                }

                if (optionSelected == selected)
                {
                    if (dialog.SelectedValue == null)
                    {
                        dialog.IsEnabled = false;
                        dialog.Close();
                        return Result.Failure;
                    }

                    helper.UpdatePBAForInternal(dialog.SelectedValue);
                    dialog.IsEnabled = false;
                    dialog.Close();
                    return Result.Success;
                }
            }
        }
    }
}

#endif