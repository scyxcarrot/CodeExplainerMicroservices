using IDS.CMF.Confidential;
using IDS.CMF.FileSystem;
using IDS.CMF.Preferences;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.Http;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.PICMF.Forms.AutoDeployment;
using Rhino;
using Rhino.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace IDS.PICMF.Helper
{
    public class UpdateHelper
    {
        private const int bufferSize = 16777215;

        public void AutoUpdate(bool allowInternal = false)
        {
            var autoDeployParams = CMFPreferences.GetAutoDeploymentParameters();

#if (STAGING)
            if (!autoDeployParams.Enable && !allowInternal)
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, "Auto Deployment disable, skip auto deployment");
                return;
            }
#endif

            // KeyVersion and KeyChecksumSha256 corresponds to package's variables on server
            var pluginBuildInfo = new BuildInfoModel(Credential.JfrogProdReposReadOnlyToken,
                autoDeployParams.PluginVariableName, autoDeployParams.ChecksumSha256VariableName);
            var pbaBuildInfo = new BuildInfoModel(Credential.JfrogProdReposReadOnlyToken,
                autoDeployParams.SmartDesignVariableName, autoDeployParams.ChecksumSha256VariableName);
            var pythonBuildInfo = new BuildInfoModel(
                Credential.JfrogProdReposReadOnlyToken,
                autoDeployParams.PBAPythonVariableName, 
                autoDeployParams.ChecksumSha256VariableName);

            if (CheckPBAPluginVersion(
                    pluginBuildInfo, 
                    pbaBuildInfo,
                    pythonBuildInfo,
                    autoDeployParams, 
                    out var currentVersion,
                    out var isUpdatePlugin, 
                    out var isUpdatePba,
                    out var isUpdatePython))
            {
                if (!ShowAutoDeployDialogMessage(isUpdatePlugin, isUpdatePba, isUpdatePython, currentVersion, pluginBuildInfo?.Version))
                {
                    return;
                }

                Update(
                    pluginBuildInfo, 
                    pbaBuildInfo, 
                    pythonBuildInfo,
                    isUpdatePlugin, 
                    isUpdatePba, 
                    isUpdatePython,
                    autoDeployParams);
            }
        }

#if (STAGING)
        public void UpdateIDSCMFForInternal(AutoDeploymentParams autoDeployParams)
        {
            var pluginBuildInfo = new BuildInfoModel(Credential.JfrogUatReposReadOnlyToken,
                autoDeployParams.PluginVariableName, autoDeployParams.ChecksumSha256VariableName);

            var pbaBuildInfo = new BuildInfoModel(Credential.JfrogUatReposReadOnlyToken,
                autoDeployParams.SmartDesignVariableName, autoDeployParams.ChecksumSha256VariableName);

            var pythonBuildInfo = new BuildInfoModel(
                Credential.JfrogProdReposReadOnlyToken,
                autoDeployParams.PBAPythonVariableName,
                autoDeployParams.ChecksumSha256VariableName);

            if (CheckPBAPluginVersion(
                    pluginBuildInfo, 
                    pbaBuildInfo, 
                    pythonBuildInfo, 
                    autoDeployParams, 
                    out var currentVersion,
                    out var isUpdatePlugin, 
                    out var isUpdatePba,
                    out var isUpdatePython))
            {
                Update(pluginBuildInfo, pbaBuildInfo, pythonBuildInfo, isUpdatePlugin, isUpdatePba, isUpdatePython, autoDeployParams);
            }
        }

        public void UpdatePBAForInternal(string version)
        {
            var autoDeployParams = CMFPreferences.GetAutoDeploymentParameters();

            autoDeployParams.SmartDesignPropertiesUrl = $"https://artifactory-hq.materialise.net:443/artifactory/api/storage/ids-generic-dev-local/PBA/{version}/smartdesign_pkg.zip?properties";
            autoDeployParams.SmartDesignDownloadUrl = $"https://artifactory-hq.materialise.net:443/artifactory/api/download/ids-generic-dev-local/PBA/{version}/smartdesign_pkg.zip";
            var pbaBuildInfo = new BuildInfoModel(Credential.JfrogDevReposReadOnlyToken,
                    autoDeployParams.SmartDesignVariableName, autoDeployParams.ChecksumSha256VariableName);
            var pythonBuildInfo = new BuildInfoModel(
                Credential.JfrogProdReposReadOnlyToken,
                autoDeployParams.PBAPythonVariableName,
                autoDeployParams.ChecksumSha256VariableName);
            IDSPluginHelper.WriteLine(LogCategory.Default, "Redirecting SmartDesign PBA for STAGING...");

            IsPbaUpdateRequired(pbaBuildInfo, pythonBuildInfo, autoDeployParams, out var isUpdatePba, out var isUpdatePython);
            if (string.IsNullOrEmpty(pbaBuildInfo.Version))
            {
                return;
            }

            if (!isUpdatePba)
            {
                IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"SmartDesign PBA is already updated: {pbaBuildInfo.Version}");
                return;
            }

            Update(null, pbaBuildInfo, pythonBuildInfo, false, isUpdatePba, false, autoDeployParams);
        }

        public void UpdatePythonForInternal(string version, string smartDesignVersion)
        {
            var autoDeployParams = CMFPreferences.GetAutoDeploymentParameters();

            autoDeployParams.PBAPythonPropertiesUrl = $"https://artifactory-hq.materialise.net:443/artifactory/api/storage/ids-generic-dev-local/PBAPython/{version}/PBA_Python.zip?properties";
            autoDeployParams.PBAPythonDownloadUrl = $"https://artifactory-hq.materialise.net:443/artifactory/api/download/ids-generic-dev-local/PBAPython/{version}/PBA_Python.zip";

            autoDeployParams.SmartDesignPropertiesUrl = $"https://artifactory-hq.materialise.net:443/artifactory/api/storage/ids-generic-dev-local/PBA/{smartDesignVersion}/smartdesign_pkg.zip?properties";
            autoDeployParams.SmartDesignDownloadUrl = $"https://artifactory-hq.materialise.net:443/artifactory/api/download/ids-generic-dev-local/PBA/{smartDesignVersion}/smartdesign_pkg.zip";

            var pythonBuildInfo = new BuildInfoModel(
                Credential.JfrogDevReposReadOnlyToken, 
                autoDeployParams.PBAPythonVariableName, 
                autoDeployParams.ChecksumSha256VariableName);
            var pbaBuildInfo = new BuildInfoModel(
                Credential.JfrogDevReposReadOnlyToken,
                autoDeployParams.SmartDesignVariableName,
                autoDeployParams.ChecksumSha256VariableName);


            IDSPluginHelper.WriteLine(LogCategory.Default, "Redirecting Python PBA for STAGING...");

            IsPbaUpdateRequired(pbaBuildInfo, pythonBuildInfo, autoDeployParams, out var isUpdatePba, out var isUpdatePython);
            if (string.IsNullOrEmpty(pythonBuildInfo.Version))
            {
                return;
            }

            if (!isUpdatePython)
            {
                IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"PBA Python is already updated: {pythonBuildInfo.Version}");
                return;
            }

            Update(null, pbaBuildInfo, pythonBuildInfo, false, isUpdatePba, isUpdatePython, autoDeployParams);
        }

        public FilesResponseModel CheckPythonVersions()
        {
            var autoDeployParams = CMFPreferences.GetAutoDeploymentParameters();

            autoDeployParams.PBAPythonPropertiesUrl = "https://artifactory-hq.materialise.net:443/artifactory/api/storage/ids-generic-dev-local/PBAPython?list&listFolders=1";
            var pythonBuildInfo = new BuildInfoModel(Credential.JfrogDevReposReadOnlyToken, null, null);

            if (!pythonBuildInfo.GetFilesList(autoDeployParams.PBAPythonPropertiesUrl, out var list))
            {
                return new FilesResponseModel();
            }

            return list;
        }

        public FilesResponseModel CheckSmartDesignVersions()
        {
            var autoDeployParams = CMFPreferences.GetAutoDeploymentParameters();

            autoDeployParams.SmartDesignPropertiesUrl = "https://artifactory-hq.materialise.net:443/artifactory/api/storage/ids-generic-dev-local/PBA?list&listFolders=1";
            var pbaBuildInfo = new BuildInfoModel(Credential.JfrogDevReposReadOnlyToken, null, null);

            if (!pbaBuildInfo.GetFilesList(autoDeployParams.SmartDesignPropertiesUrl, out var list))
            {
                return new FilesResponseModel();
            }

            return list;
        }

        public FilesResponseModel CheckIDSCMFVersion(AutoDeploymentParams autoDeployParams)
        {
            var versionInfo = new BuildInfoModel(Credential.JfrogUatReposReadOnlyToken, null, null);

            if (!versionInfo.GetFilesList(autoDeployParams.AutoDeployBuildPropertiesUrl, out var list))
            {
                return new FilesResponseModel();
            }

            return list;
        }
#endif

        private bool IsPluginUpdateRequired(BuildInfoModel pluginBuildInfo, string buildPropertiesUrl, out string currentVersion)
        {
            currentVersion = string.Empty;

            if (!pluginBuildInfo.GetBuildInfo(buildPropertiesUrl))
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "Failed to check latest build information for IDSCMF");
                return false;
            }

            var assembly = Assembly.GetExecutingAssembly();
            currentVersion = FileVersionInfo.GetVersionInfo(assembly.Location).ProductVersion;
            var isUpdatePlugin = pluginBuildInfo.Version != currentVersion;
            return isUpdatePlugin;
        }

        private void IsPbaUpdateRequired(BuildInfoModel pbaBuildInfo, BuildInfoModel pythonBuildInfo, AutoDeploymentParams autoDeployParams, out bool isUpdatePba, out bool isUpdatePython)
        {
            isUpdatePython = false;
            isUpdatePba = false;
            if (!pbaBuildInfo.GetBuildInfo(autoDeployParams.SmartDesignPropertiesUrl))
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "Failed to check latest build information for SmartDesign");
                return;
            }
            else if (!pythonBuildInfo.GetBuildInfo(autoDeployParams.PBAPythonPropertiesUrl))
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "Failed to check latest build information for PBA Python");
                return;
            }

            var cmfResource = new CMFResources();
            var batFileReturns = SmartDesignUtilities.ExecuteBatFileWithReturn(cmfResource.AutoDeploymentCheckPBAVersionScriptPath);
            
            if (batFileReturns.Any(p => p.Contains("No Python found")))
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning,
                  "Could not find PBA_env. Please rerun the All-In-One installer to use the SmartDesign package.");
            }
            else
            {
                var pythonVersion = batFileReturns
                    .Where(text => text.StartsWith("Python"))
                    .Select(text => Regex.Match(text, @"\d+\.\d+\.\d+").Value) // Extract "x.xx.xx"
                    .FirstOrDefault();

                isUpdatePba = !batFileReturns.Any(p => p.Contains(pbaBuildInfo.Version));

                if (pythonBuildInfo.Version != pythonVersion)
                {
                    isUpdatePython = true;
                    isUpdatePba = true; // when new env is created, require SmartDesign package to install 
                }
            }
        }

        private void Update(
            BuildInfoModel pluginBuildInfo, 
            BuildInfoModel pbaBuildInfo, 
            BuildInfoModel pythonBuildInfo,
            bool isUpdatePlugin, 
            bool isUpdatePba, 
            bool isUpdatePython,
            AutoDeploymentParams autoDeployParams)
        {
            IDSPluginHelper.WriteLine(LogCategory.Default, "Downloading...");
            var tempDirectory = Path.Combine(Path.GetTempPath(), "IDSCMF");
            Directory.CreateDirectory(tempDirectory);

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var cancellationToken = cancellationTokenSource.Token;
                var downloadDialog = new DownloadDialog(pluginBuildInfo?.Version, isUpdatePlugin, isUpdatePba, isUpdatePython);
                var zipFilePath = Path.Combine(tempDirectory, "IDSCMF.zip");
                var smartDesignZipPath = Path.Combine(tempDirectory, "smartdesign_pkg.zip");
                var pythonZipPath = Path.Combine(tempDirectory, "PBA_Python.zip");
                var tasksList = new List<Task>();
                var pluginDownloaded = false;
                var pbaDownloaded = false;
                var pythonDownloaded = false;

                if (isUpdatePlugin)
                {
                    tasksList.Add(Task.Run(() =>
                    {
                        pluginDownloaded = pluginBuildInfo.Download(autoDeployParams.AutoDeployBuildDownloadUrl,
                            autoDeployParams.DownloadTimeOutMin, zipFilePath,
                            bufferSize, downloadDialog.PluginProgressBar.ProgressDataModel, cancellationToken);

                        CheckAndCloseDialog(isUpdatePlugin, isUpdatePba, isUpdatePython,
                            pluginDownloaded, pbaDownloaded, pythonDownloaded, downloadDialog);

                    }, cancellationToken));
                }

                if (isUpdatePba)
                {
                    tasksList.Add(Task.Run(() =>
                    {
                        pbaDownloaded = pbaBuildInfo.Download(autoDeployParams.SmartDesignDownloadUrl,
                            autoDeployParams.DownloadTimeOutMin, smartDesignZipPath,
                            bufferSize, downloadDialog.PbaProgressBar.ProgressDataModel, cancellationToken);

                        CheckAndCloseDialog(isUpdatePlugin, isUpdatePba, isUpdatePython,
                            pluginDownloaded, pbaDownloaded, pythonDownloaded, downloadDialog);

                    }, cancellationToken));
                }

                if (isUpdatePython)
                {
                    tasksList.Add(Task.Run(() =>
                    {
                        pythonDownloaded = pythonBuildInfo.Download(
                            autoDeployParams.PBAPythonDownloadUrl,
                            autoDeployParams.DownloadTimeOutMin, 
                            pythonZipPath,
                            bufferSize, downloadDialog.PythonProgressBar.ProgressDataModel, cancellationToken);

                        CheckAndCloseDialog(isUpdatePlugin, isUpdatePba, isUpdatePython,
                            pluginDownloaded, pbaDownloaded, pythonDownloaded, downloadDialog);

                    }, cancellationToken));
                }

                downloadDialog.ShowDialog();

                // Cancels if nothing was downloaded
                if (!(pluginDownloaded || pbaDownloaded || pythonDownloaded))
                {
                    cancellationTokenSource.Cancel();
                }

                try
                {
                    Task.WaitAll(tasksList.ToArray(), cancellationToken);

                    var unzipFilePath = "";
                    var unzipPbaPath = "";

                    if (isUpdatePlugin)
                    {
                        unzipFilePath = Path.Combine(tempDirectory, "IDSCMF");
                        ZipFile.ExtractToDirectory(zipFilePath, unzipFilePath);
                    }

                    if (isUpdatePba)
                    {
                        unzipPbaPath = Path.Combine(tempDirectory, "PBA");
                        ZipFile.ExtractToDirectory(smartDesignZipPath, unzipPbaPath);
                    }

                    if (isUpdatePython)
                    {
                        var unzipPythonPath = @"C:\IDS\PBA_Python";
                        SystemTools.DeleteRecursively(unzipPythonPath);
                        ZipFile.ExtractToDirectory(pythonZipPath, unzipPythonPath);
                    }


                    var cmfResource = new CMFResources();
                    RunScriptWithRhinoClosed(cmfResource.AutoDeploymentInstallationProxyScriptPath,
                        $"\"{unzipFilePath}\" \"{tempDirectory}\" \"{unzipPbaPath}\" {isUpdatePlugin} {isUpdatePba} {isUpdatePython}");

                    return;
                }
                catch (OperationCanceledException)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Warning,
                        "User cancelled download new packages");
                }
                catch (Exception ex)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error,
                        $"Failed to download new packages due to: {ex.Message}");
                }
            }

            SystemTools.DeleteRecursively(tempDirectory);
        }

        private void CheckAndCloseDialog(
            bool isUpdatePlugin, 
            bool isUpdatePba, 
            bool isUpdatePython,
            bool pluginDownloaded,
            bool pbaDownloaded,
            bool pythonDownloaded,
            DownloadDialog downloadDialog)
        {

            // Using await or Task.WaitAll would cause deadlock between the thread and WPF.
            // Since the design of this Auto Update is to block users from using Rhino, using Task.WhenAll is not ideal
            // This IS a workaround and a proper way to implement should be investigated (Tech Debt - REQUIREMENT 1117883)

            var canClose =
                (!isUpdatePlugin || pluginDownloaded) &&
                (!isUpdatePba || pbaDownloaded) &&
                (!isUpdatePython || pythonDownloaded);


            if (canClose)
            {
                downloadDialog.Dispatcher.Invoke(() => downloadDialog.Close());
            }
        }

        private static void RunScriptWithRhinoClosed(string scriptPath, string arguments)
        {
            RhinoApp.Closing += (sender, args) =>
            {
                var childProcess = new Process
                {
                    StartInfo =
                    {
                        FileName = scriptPath,
                        Arguments = arguments,
                        RedirectStandardOutput = false,
                        UseShellExecute = true,
                        CreateNoWindow = true
                    }
                };
                childProcess.Start();
            };

            var dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += (sender, args) => { RhinoApp.Exit(); };
            dispatcherTimer.Interval = TimeSpan.FromSeconds(1);
            dispatcherTimer.Start();
        }

        private static bool ShowAutoDeployDialogMessage(bool isUpdatePlugin, bool isUpdatePba, bool isUpdatePython, string currentVersion, string latestVersion)
        {
            var message = "The following are outdated:\n";

            if (isUpdatePlugin)
            {
                message += $"\n- Current IDSCMF version is {currentVersion}, while latest is {latestVersion}";
            }

            if (isUpdatePba)
            {
                message += "\n- SmartDesign PBA";
            }

            if (isUpdatePython)
            {
                message += "\n- PBA Python";
            }

            message += "\n\nDo you want to update?";

            var result = Dialogs.ShowMessage(message, "Packages Outdated!", ShowMessageButton.YesNo, ShowMessageIcon.Question);

            return result == ShowMessageResult.Yes;
        }

        private bool CheckPBAPluginVersion(
            BuildInfoModel pluginBuildInfo, 
            BuildInfoModel pbaBuildInfo,
            BuildInfoModel pythonBuildInfo,
            AutoDeploymentParams autoDeployParams, 
            out string pluginCurrentVersion, 
            out bool isUpdatePlugin, 
            out bool isUpdatePba,
            out bool isUpdatePython)
        {
            isUpdatePlugin = IsPluginUpdateRequired(pluginBuildInfo, autoDeployParams.AutoDeployBuildPropertiesUrl, out pluginCurrentVersion);
            IsPbaUpdateRequired(pbaBuildInfo, pythonBuildInfo, autoDeployParams, out isUpdatePba, out isUpdatePython);

            if (string.IsNullOrEmpty(pluginBuildInfo.Version) || string.IsNullOrEmpty(pbaBuildInfo.Version) || string.IsNullOrEmpty(pythonBuildInfo.Version))
            {
                return false;
            }

            if (!isUpdatePlugin && !isUpdatePba)
            {
                return false;
            }

            return true;
        }
    }
}