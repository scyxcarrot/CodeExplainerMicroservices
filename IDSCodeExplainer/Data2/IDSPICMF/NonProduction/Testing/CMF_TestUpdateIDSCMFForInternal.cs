#if (STAGING)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IDS.CMF.Preferences;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.PICMF.Forms;
using IDS.PICMF.Helper;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;

namespace IDS.PICMF.NonProduction
{
    public class CMF_TestUpdateIDSCMFForInternal : Command
    {
        public CMF_TestUpdateIDSCMFForInternal()
        {
            Instance = this;
        }

        public static CMF_TestUpdateIDSCMFForInternal Instance { get; private set; }

        public override string EnglishName => "CMF_TestUpdateIDSCMFForInternal";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var uatEnvironment = "UAT";
            var prodEnvironment = "Production";
            var optionList = new List<string> { uatEnvironment, prodEnvironment };
            var optionToggleIndex = 0;

            var getOption = new GetOption();
            getOption.SetCommandPrompt("Select an environment to download IDSCMF");
            getOption.AcceptNothing(true);

            while (true)
            {
                getOption.ClearCommandOptions();
                getOption.AddOptionList("Environments", optionList, optionToggleIndex);
                var result = getOption.Get();

                if (result == GetResult.Cancel)
                {
                    return Result.Cancel;
                }

                if (result == GetResult.Nothing)
                {
                    if (optionToggleIndex == optionList.IndexOf(prodEnvironment))
                    {
                        return AutoUpdate();
                    }

                    return GetUserInput();
                }

                if (result == GetResult.Option)
                {
                    optionToggleIndex = getOption.Option().CurrentListOptionIndex;
                    continue;
                }
            }
        }

        private Result AutoUpdate()
        {
            try
            {
                var helper = new UpdateHelper();
                helper.AutoUpdate(true);

                return Result.Success;
            }
            catch (Exception)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Could not update to Production version");
                return Result.Failure;
            }
        }

        private Result GetUserInput()
        {
            var buildNum = CheckIDSCMFVersionUAT();

            if (buildNum.Count < 1)
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "UAT Repo does not have any IDSCMF packages (Or server is down).");
                return Result.Cancel;
            }

            var dialog = new ListViewSelector(buildNum, "Select IDSCMF Version");
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

                if (optionSelected == selected)
                {
                    if (dialog.SelectedValue == null)
                    {
                        dialog.IsEnabled = false;
                        dialog.Close();
                        return Result.Failure;
                    }

                    UpdateToUAT(dialog.SelectedValue);
                    dialog.IsEnabled = false;
                    dialog.Close();
                    return Result.Success;
                }

                if (optionSelected == cancel)
                {
                    dialog.IsEnabled = false;
                    dialog.Close();
                    return Result.Cancel;
                }
            }
        }

        private void UpdateToUAT(string version)
        {
            var helper = new UpdateHelper();
            var autoDeployParams = CMFPreferences.GetAutoDeploymentParameters();

            autoDeployParams.AutoDeployBuildPropertiesUrl =
                $"https://artifactory-hq.materialise.net:443/artifactory/ids-generic-uat-local/AutoDeploy/{version}/IDSCMF/IDSCMF.zip?properties";
            autoDeployParams.AutoDeployBuildDownloadUrl =
                $"https://artifactory-hq.materialise.net:443/artifactory/ids-generic-uat-local/AutoDeploy/{version}/IDSCMF/IDSCMF.zip";

            autoDeployParams.SmartDesignPropertiesUrl =
                $"https://artifactory-hq.materialise.net:443/artifactory/ids-generic-uat-local/AutoDeploy/{version}/PBA/smartdesign_pkg.zip?properties";
            autoDeployParams.SmartDesignDownloadUrl =
                $"https://artifactory-hq.materialise.net:443/artifactory/ids-generic-uat-local/AutoDeploy/{version}/PBA/smartdesign_pkg.zip";

            helper.UpdateIDSCMFForInternal(autoDeployParams);
        }

        private List<string> CheckIDSCMFVersionUAT()
        {
            var helper = new UpdateHelper();
            var autoDeployParams = CMFPreferences.GetAutoDeploymentParameters();
            autoDeployParams.AutoDeployBuildPropertiesUrl = "https://artifactory-hq.materialise.net:443/artifactory/api/storage/ids-generic-uat-local/AutoDeploy?list&listFolders=1";

            var versions = helper.CheckIDSCMFVersion(autoDeployParams);
            if (versions.Files == null)
            {
                return new List<string>();
            }

            var builds = versions.Files.Where(v => v.IsFolder).OrderByDescending(v => v.LastModified);
            return builds.Select(b => Path.GetFileName(b.Uri)).ToList();
        }
    }
}

#endif