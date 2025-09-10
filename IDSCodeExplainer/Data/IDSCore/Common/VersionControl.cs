using IDS.Core.ImplantDirector;
using IDS.Core.SplashScreen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;

namespace IDS.Core.PluginHelper
{
    public static class VersionControl
    {
        /// <summary>
        /// Gets the ids version.
        /// </summary>
        /// <returns></returns>
        public static string GetIDSVersion(IPluginInfoModel infoModel)
        {
            return infoModel.GetVersionLabel();
        }

        public static void DoVersionCheck(IImplantDirector director, bool fulltext, bool showIfOk, string writetoFilename, IPluginInfoModel infoModel)
        {
            var versionFullText = GetVersionCheckText(director, true);

            // Write to file if necessary`
            if (writetoFilename != string.Empty)
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(writetoFilename))
                {
                    file.Write(versionFullText);
                }
            }

            //MessageBox.Show(versionText);
            var splash = director != null ?
                new frmAbout(GetProjectIdsVersionHash(director), GetProjectRhinoMatSdkVersionHash(director), infoModel) : new frmAbout(infoModel);
            splash.ShowDialog();
        }

        public static string GetProjectIdsVersionHash(IImplantDirector director)
        {
            return director.ComponentVersions["IDS"]["build"];
        }

        public static string GetProjectRhinoMatSdkVersionHash(IImplantDirector director)
        {
            return director.ComponentVersions["RhinoMatSDKOperations"]["build"];
        }

        public static string GenerateIDSLabelBuildCommitHash(IImplantDirector director)
        {
            return GenerateIDSLabelBuildCommitHash(GetProjectIdsVersionHash(director));
        }

        public static string GenerateIDSLabelBuildCommitHash(string projectIdsHash)
        {
            var currIdsVersion = GetCurrentIDSVersion();

            const string mismatchString = "(Version mismatch)";
            var idsVersionMismatch = currIdsVersion == projectIdsHash ? string.Empty : mismatchString;
            return $"{currIdsVersion.Substring(0, 6)} / Project {projectIdsHash.Substring(0, 6)} {idsVersionMismatch}";
        }

        public static string GenerateRhinoMatSDKLabelBuildCommitHash(IImplantDirector director)
        {
            return GenerateRhinoMatSDKLabelBuildCommitHash(GetProjectRhinoMatSdkVersionHash(director));
        }

        public static string GenerateRhinoMatSDKLabelBuildCommitHash(string projectRhinoMatSdkHash)
        {
            var currRhinoMatSdkVersion = GetCurrentRhinoMatSdkVersion();

            const string mismatchString = "(Version mismatch)";
            var rhinoMatSdkVersionMismatch = currRhinoMatSdkVersion == projectRhinoMatSdkHash ? string.Empty : mismatchString;
            return
                $"{currRhinoMatSdkVersion.Substring(0, 6)} / Project {projectRhinoMatSdkHash.Substring(0, 6)} {rhinoMatSdkVersionMismatch}";
        }

        /// <summary>
        /// Gets the ids version.
        /// </summary>
        /// <value>
        /// The ids version.
        /// </value>
        public static string GetCurrentIDSVersion()
        {
            Dictionary<string, Dictionary<string, DateTime>> timestamps;
            var versions = GetVersionDictionaries(out timestamps);

            return versions["IDS"]["build"];
        }

        /// <summary>
        /// Gets the rhino mat SDK version.
        /// </summary>
        /// <value>
        /// The rhino mat SDK version.
        /// </value>
        public static string GetCurrentRhinoMatSdkVersion()
        {
            Dictionary<string, Dictionary<string, DateTime>> timestamps;
            var versions = VersionControl.GetVersionDictionaries(out timestamps);

            return versions["RhinoMatSDKOperations"]["build"];
        }

        public static string VersionSubCheck(IImplantDirector director, string componentName)
        {
            // Init
            string text = "";

            // Title
            text += $"{componentName}\n";

            // Predefine versionFileType strings
            var commitTag = "commit";
            var buildTag = "build";
            string[] versionFileTypes = { commitTag, buildTag };

            // Get hashes from file
            string commitHash;
            string buildHash;
            DateTime commitDate;
            DateTime buildDate;
            var commitAvailable = GetCommitAndDate(componentName, commitTag, out commitHash, out commitDate);
            var buildAvailable = GetCommitAndDate(componentName, buildTag, out buildHash, out buildDate);

            // Add to full text
            if (buildAvailable)
            {
                text += $"   * Program {buildTag}\t ({buildDate:yyyy-MM-dd}) {buildHash}\n";
            }
            if (commitAvailable)
            {
                text += $"   * Program {commitTag}\t ({commitDate:yyyy-MM-dd}) {commitHash}\n";
            }

            // Check project versions
            if (director != null)
            {
                foreach (var versionFileType in versionFileTypes)
                {
                    var componentHash = string.Empty;
                    var componentDate = DateTime.MinValue;
                    var performCheck = true;
                    if (versionFileType == commitTag)
                    {
                        componentHash = commitHash;
                        componentDate = commitDate;
                        performCheck = commitAvailable;
                    }
                    else if (versionFileType == buildTag)
                    {
                        componentHash = buildHash;
                        componentDate = buildDate;
                        performCheck = buildAvailable;
                    }

                    if (!performCheck)
                    {
                        continue;
                    }

                    string projectHash;
                    DateTime projectDate;
                    var versionsOK = CompareComponentVersion(director, componentName, componentHash, componentDate, versionFileType, out projectHash, out projectDate);

                    text += $"   * Project {versionFileType}\t ({projectDate:yyyy-MM-dd}) {projectHash}\n";
                    if (componentHash == string.Empty)
                    {
                        text += $"   * Project {componentName} {versionFileType} unknown.\n";
                    }
                    else if (!versionsOK)
                    {
                        text += $"   => Project was created in different {componentName} {versionFileType}.\n";
                    }
                }
            }

            text += "\n";

            return text;
        }

        private static bool CompareComponentVersion(IImplantDirector director, string componentName, string componentHash, DateTime componentDate, string versionType, out string projectHash, out DateTime projectDate)
        {
            // Initialize
            var versionOk = true;
            projectHash = string.Empty;
            projectDate = DateTime.MinValue;

            if (director.ComponentVersions[componentName].ContainsKey(versionType) && director.ComponentVersions[componentName][versionType] != string.Empty)
            {
                projectHash = director.ComponentVersions[componentName][versionType];
                projectDate = director.ComponentDateTimes[componentName][versionType];
                
                if (projectHash != componentHash || projectDate != componentDate)
                {
                    versionOk = false;
                }
            }
            else if (componentHash == string.Empty)
            {
                versionOk = false;
            }

            return versionOk;
        }

        private static bool GetCommitAndDate(string componentName, string versionFileType, out string commitHash, out DateTime commitDate)
        {
            // Read versions
            Dictionary<string, Dictionary<string, DateTime>> compDate;
            var compVers = GetVersionDictionaries(out compDate);

            // Get commit for version file type
            commitHash = string.Empty;
            commitDate = DateTime.MinValue;
            if (!compVers[componentName].ContainsKey(versionFileType))
            {
                return false;
            }

            commitDate = compDate[componentName][versionFileType];
            commitHash = compVers[componentName][versionFileType];

            return true;
        }

        public static string GetVersionCheckText(IImplantDirector director, bool fullText)
        {
            var text = "Version Check\n";
            text += $"IDS version {GetIDSVersion(director.PluginInfoModel)}\n";

            if (director == null)
            {
                text += "No IDS project loaded\n\n";
            }
            else
            {
                text += "\n";
            }

            Dictionary<string, Dictionary<string, DateTime>> compDate;
            var compVers = GetVersionDictionaries(out compDate);

            foreach (var componentName in compVers.Keys)
            {
                var subchecktext = VersionSubCheck(director, componentName);
                if (fullText)
                {
                    text += subchecktext;
                }
            }

            return text;
        }

        // Versions and dates of the software components
        public static Dictionary<string, Dictionary<string, string>> GetVersionDictionaries(out Dictionary<string, Dictionary<string, DateTime>> componentDateTimes)
        {
            // XML parsing variables
            var resources = new Resources();
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(resources.Versionsfile);

            var componentVersions = new Dictionary<string, Dictionary<string, string>>();
            componentDateTimes = new Dictionary<string, Dictionary<string, DateTime>>();

            // Parse
            foreach (XmlNode nodeComponent in xmlDoc.SelectNodes("/versions/component"))
            {
                var versionType = "build";
                var versions = new Dictionary<string, string>();
                var datetimes = new Dictionary<string, DateTime>();

                var selectedNode = nodeComponent.SelectSingleNode(versionType);

                if (selectedNode == null)
                    continue;

                versions.Add(versionType, selectedNode.InnerText);
                datetimes.Add(versionType, DateTime.ParseExact(
                    selectedNode.Attributes["date"].Value,
                    "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture));

                var name = nodeComponent.Attributes["name"].Value;

                componentVersions.Add(name, versions);
                componentDateTimes.Add(name, datetimes);
            }

            return componentVersions;
        }

        public static string GetVersionFromFile(string file, out DateTime timestamp)
        {
            // Initialize
            var hash = "";
            timestamp = new DateTime();

            try
            {
                var lines = System.IO.File.ReadAllLines(file);

                foreach (var line in lines)
                {
                    if (!line.Contains(","))
                    {
                        continue;
                    }

                    if (line.ToLower().Contains("softwareversion"))
                    {
                        hash = line.Split(new [] { "," }, StringSplitOptions.None)[1];
                    }
                    else if (line.ToLower().Contains("date"))
                    {
                        var datestring = line.Split(new [] { "," }, StringSplitOptions.None)[1];
                        timestamp = DateTime.ParseExact(datestring.Substring(0, 10), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                    }
                }
                return hash;
            }
            catch
            {
                throw new Exception($"Could not read version information from {file}");
            }
        }

        public static string GetRhinoVersion()
        {
            var rhinoExeFileName = Process.GetCurrentProcess().MainModule.FileName;
            FileVersionInfo rhinoExeInfo = FileVersionInfo.GetVersionInfo(rhinoExeFileName);
            return rhinoExeInfo.ProductVersion;
        }
    }
}
