using IDS.Core.Common;
using IDS.Core.Enumerators;
using IDS.Core.SplashScreen;
using IDS.Core.Utilities;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Rhino;
using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Management;
#if (INTERNAL)
using IDS.Core.Forms;
#endif

namespace IDS.Core.PluginHelper
{
    public static class Msai
    {
        public static string DocumentNameKey => "DocumentName";

        private static string _userEmail;

        private static readonly IdleTimeTracker _idleTimeTracker = InitializeIdleTimeTracker();

        private static string GetInstrumentationKey(string productLine)
        {
            var productLineLowered = productLine.ToLower();

            switch (productLineLowered)
            {
                case "cmf":
                    return "2236ed93-99cf-4c68-adff-08a3cfa429a4";
                case "amace":
                    return "619d204f-c662-41f6-a1ec-948f67a1236a";
                case "glenius":
                    return "b1562391-dc87-4054-98e5-e98b5e1789cc";
                default:
                    return null;
            }
        }

        private struct ClientInfo
        {
            public TelemetryClient Client;
            public IPluginInfoModel PluginInfo;
            public IdleTimeStopWatch SessionIdleTimeStopwatch;
        }

        private static PcInfo _pcInfo;

        private static VersionInfo _versionInfo;

        private static Dictionary<string, ClientInfo> _clients = new Dictionary<string, ClientInfo>();

        private static IdleTimeTracker InitializeIdleTimeTracker()
        {
            var resources = new Resources();
            return new IdleTimeTracker(resources.IdleConfirmationTimeMs);
        }

        public static void Initialize(IPluginInfoModel pluginInfo, string fileName, int version, int draft)
        {
            var instrumentationKey = GetInstrumentationKey(pluginInfo.ProductName);

            if (instrumentationKey == null) //MSAI not yet set
            {
                return;
            }

            if (!_clients.ContainsKey(pluginInfo.ProductName))
            {
                var config = TelemetryConfiguration.CreateDefault();

                var client = new TelemetryClient(config);
                client.InstrumentationKey = instrumentationKey;
                client.Context.Session.Id = Guid.NewGuid().ToString();
                client.Context.Device.OperatingSystem = Environment.OSVersion.ToString();
                client.Context.User.Id = GenerateUserId();
                client.Context.User.AccountId = GenerateEmailAddress();

                var clientInfo = new ClientInfo()
                {
                    Client = client,
                    PluginInfo = pluginInfo,
                    SessionIdleTimeStopwatch = new IdleTimeStopWatch()
                };
                _idleTimeTracker.SubscribeIdleTimeStopwatch(clientInfo.SessionIdleTimeStopwatch);

                _clients.Add(pluginInfo.ProductName, clientInfo);

            }

            var sessionParameters = new Dictionary<string, string>();
            AddSessionParameters(pluginInfo, ref sessionParameters, fileName);
            sessionParameters.Add("Case Version", version.ToString());
            sessionParameters.Add("Case Draft", draft.ToString());

            TrackOpsEvent("SessionStart", pluginInfo.ProductName, sessionParameters, null);
        }

        public static void Terminate(IPluginInfoModel pluginInfo, string fileName, int version, int draft)
        {
            if (_clients.ContainsKey(pluginInfo.ProductName))
            {
                var idleStopwatch = _clients[pluginInfo.ProductName].SessionIdleTimeStopwatch;
                _idleTimeTracker.UnsubscribeIdleTimeStopwatch(idleStopwatch);

                var elapsedInSeconds = idleStopwatch.TotalTime * 0.001;
                var effectiveElapsedInSeconds = idleStopwatch.EffectiveTimeMs * 0.001;
                var idleElapsedInSeconds = idleStopwatch.IdleTimeMs * 0.001;
                var editingElapsedInSeconds = idleStopwatch.EditingTimeMs * 0.001;

                var sessionParameters = new Dictionary<string, string>();
                AddSessionParameters(pluginInfo, ref sessionParameters, fileName);
                sessionParameters.Add("Case Version", version.ToString());
                sessionParameters.Add("Case Draft", draft.ToString());


                var sessionMetric = new Dictionary<string, double>();
                sessionMetric.Add("SessionTime", elapsedInSeconds);
                sessionMetric.Add("SessionIdleTime", idleElapsedInSeconds);
                sessionMetric.Add("SessionEffectiveTime", effectiveElapsedInSeconds);
                sessionMetric.Add("SessionEditingTime", editingElapsedInSeconds);

                TrackOpsEvent("SessionEnd", pluginInfo.ProductName, sessionParameters, sessionMetric);

                _clients[pluginInfo.ProductName].Client.Flush();
                System.Threading.Thread.Sleep(2000);
                _clients.Remove(pluginInfo.ProductName);
            }
        }

        public static void PublishToAzure()
        {
            if (_clients == null || !_clients.Any())
            {
                return;
            }

            foreach (var client in _clients)
            {
                client.Value.Client.Flush();
            }
        }

        private static void AddSessionParameters(IPluginInfoModel pluginInfo, ref Dictionary<string,string> sessionParameters, string fileName)
        {
            sessionParameters.Add("ProductName", pluginInfo.ProductName);
            sessionParameters.Add("VersionLabel", pluginInfo.GetVersionLabel());
            sessionParameters.Add("FileVersionLabel", pluginInfo.GetFileVersionLabel());
            sessionParameters.Add("ManufacturedDate", pluginInfo.GetManufacturedDate());
            sessionParameters.Add("CopyrightYear", pluginInfo.GetCopyrightYear());
            sessionParameters.Add("LNumber", pluginInfo.GetLNumber());
            sessionParameters.Add("UserId", GenerateUserId());
            sessionParameters.Add("UserEmail", GenerateEmailAddress());

            if (!sessionParameters.ContainsKey(DocumentNameKey))
            {
                sessionParameters.Add(DocumentNameKey, fileName);
            }

            AddVersionInfo(ref sessionParameters);
        }

        public static string GenerateUserId()
        {
            return StringUtilities.ToMD5Hash(Environment.MachineName);
        }

        public static string GenerateEmailAddress()
        {
            if (!string.IsNullOrEmpty(_userEmail))
            {
                return _userEmail;
            }

            try
            {
                _userEmail =  UserPrincipal.Current.EmailAddress.ToLower();
            }
            catch
            {
                _userEmail = string.Empty;
            }

            return _userEmail;
        }

        private static string GenerateOpsKey(string productLine)
        {
            return $"[OPS][{productLine}]";
        }

        private static string GenerateOpsKey(string eventKey, string productLine)
        {
            return $"{GenerateOpsKey(productLine)}_{eventKey}";
        }

        private static string GenerateDevKey(string productLine)
        {
            return $"[DEV][{productLine}]";
        }

        private static string GenerateDevKey(string eventKey, string productLine)
        {
            return $"{GenerateDevKey(productLine)}_{eventKey}";
        }

        public static void TrackOpsEvent(string eventKey, string productLine)
        {
            if (!_clients.ContainsKey(productLine) || !_clients[productLine].Client.IsEnabled())
            {
                return;
            }

            var parameters = new Dictionary<string, string>();
            AddUserInfo(ref parameters);
            AddPCSpecs(ref parameters);
            AddDocumentName(ref parameters);
            AddVersionInfo(ref parameters);

            _clients[productLine].Client.TrackEvent(GenerateOpsKey(eventKey, productLine), parameters);
        }

        public static void TrackOpsEvent(string eventKey, string productLine, Dictionary<string,string> parameters = null, Dictionary<string, double> metrics = null)
        {
            if (!_clients.ContainsKey(productLine) || !_clients[productLine].Client.IsEnabled())
            {
                return;
            }

            AddUserInfo(ref parameters);
            AddPCSpecs(ref parameters);
            AddDocumentName(ref parameters);
            AddVersionInfo(ref parameters);

            _clients[productLine].Client.TrackEvent(GenerateOpsKey(eventKey, productLine), parameters, metrics);
        }

        public static void TrackDevEvent(string eventKey, string productLine)
        {
            if (!_clients.ContainsKey(productLine) || !_clients[productLine].Client.IsEnabled())
            {
                return;
            }

            var parameters = new Dictionary<string, string>();
            AddUserInfo(ref parameters);
            AddPCSpecs(ref parameters);
            AddDocumentName(ref parameters);
            AddVersionInfo(ref parameters);

            _clients[productLine].Client.TrackEvent(GenerateDevKey(eventKey, productLine), parameters);
        }

        public static void TrackDevEvent(string eventKey, string productLine, Dictionary<string, string> parameters = null, Dictionary<string, double> metrics = null)
        {
            if (!_clients.ContainsKey(productLine) || !_clients[productLine].Client.IsEnabled())
            {
                return;
            }

            AddUserInfo(ref parameters);
            AddPCSpecs(ref parameters);
            AddDocumentName(ref parameters);
            AddVersionInfo(ref parameters);

            _clients[productLine].Client.TrackEvent(GenerateDevKey(eventKey, productLine), parameters, metrics);
        }

        public static void TrackException(Exception e, string productLine, Dictionary<string, string> parameters = null, Dictionary<string, double> metrics = null)
        {
            if (!_clients.ContainsKey(productLine) || !_clients[productLine].Client.IsEnabled())
            {
                return;
            }

            if (parameters == null)
            {
                parameters = new Dictionary<string, string>();
            }

            parameters.Add("Identifier", GenerateOpsKey(productLine));
            AddUserInfo(ref parameters);
            AddPCSpecs(ref parameters);

            var id = Guid.NewGuid();
            IDSPluginHelper.WriteLine(LogCategory.Default, $"Problem Id: {id}");
            parameters.Add("Problem Id", id.ToString());

            AddDocumentName(ref parameters);
            AddVersionInfo(ref parameters);

            _clients[productLine].Client.TrackException(e, parameters, metrics);
        }

        private static void AddUserInfo(ref Dictionary<string, string> parameters)
        {
            if (parameters == null)
            {
                parameters = new Dictionary<string, string>();
                parameters.Add("UserId", GenerateUserId());
                parameters.Add("UserEmail", GenerateEmailAddress());
            }
            else
            {
                if (!parameters.ContainsKey("UserId"))
                {
                    parameters.Add("UserId", GenerateUserId());
                }

                if (!parameters.ContainsKey("UserEmail"))
                {
                    parameters.Add("UserEmail", GenerateEmailAddress());
                }
            }
        }

        private static void AddPCSpecs(ref Dictionary<string, string> parameters)
        {
            if (!_pcInfo.IsInitialized)
            {
                _pcInfo = RetrieveProcessorInfo();
            }

            parameters.Add("OS Name", _pcInfo.OsFullName);
            parameters.Add("OS Version", _pcInfo.OsVersion);
            parameters.Add("OS Platform", _pcInfo.OsPlatform);
            parameters.Add("System Language", _pcInfo.SystemLanguage);
            parameters.Add("Total System Memory (MB)", StringUtilities.DoubleStringify(_pcInfo.TotalSystemMemory, 2));

            for (var i = 0; i < _pcInfo.CpuInfos.Count; i++)
            {
                parameters.Add($"CPU Name {i}", _pcInfo.CpuInfos[i].CpuName);
                parameters.Add($"CPU N Logical Processor {i}", _pcInfo.CpuInfos[i].CpuNLogicalProcessor);
                parameters.Add($"CPU N Cores {i}", _pcInfo.CpuInfos[i].CpuNCores);
                parameters.Add($"CPU Max Clock Speed (MHz) {i}", _pcInfo.CpuInfos[i].CpuMaxClockSpeed);
            }

            for (var i = 0; i < _pcInfo.GpuInfos.Count; i++)
            {
                parameters.Add($"GPU Name {i}", _pcInfo.GpuInfos[i].GpuName);
                parameters.Add($"GPU Chip {i}", _pcInfo.GpuInfos[i].GpuChip);
                parameters.Add($"GPU Memory (MB) {i}", StringUtilities.DoubleStringify(_pcInfo.GpuInfos[i].GpuRam, 2));
                parameters.Add($"GPU Driver Version {i}", _pcInfo.GpuInfos[i].GpuDriverVersion);
                parameters.Add($"GPU Status {i}", _pcInfo.GpuInfos[i].GpuStatus);

            }
        }

        private static void AddDocumentName(ref Dictionary<string, string> parameters)
        {
            if (!parameters.ContainsKey(DocumentNameKey))
            {
                if (RhinoDoc.ActiveDoc != null && IDSPluginHelper.GetDirector(RhinoDoc.ActiveDoc.DocumentId) != null)
                {
                    var filename = IDSPluginHelper.GetDirector(RhinoDoc.ActiveDoc.DocumentId).FileName;
                    parameters.Add(DocumentNameKey, filename);
                }
            }
        }

        private static void AddVersionInfo(ref Dictionary<string, string> parameters)
        {
            if (string.IsNullOrEmpty(_versionInfo.RhinoVersion))
            {
                _versionInfo.RhinoVersion = VersionControl.GetRhinoVersion();
            }

            if (string.IsNullOrEmpty(_versionInfo.PluginVersion))
            {
                _versionInfo.PluginVersion = IDSPluginHelper.PluginVersion;
#if (INTERNAL)
                _versionInfo.PluginVersion += ".debug";
#endif
            }

            const string rhinoVersionKey = "Rhino Version";
            const string pluginVersionKey = "Plugin Version";

            if (!parameters.ContainsKey(rhinoVersionKey))
            {
                parameters.Add(rhinoVersionKey, _versionInfo.RhinoVersion);
            }

            if (!parameters.ContainsKey(pluginVersionKey))
            {
                parameters.Add(pluginVersionKey, _versionInfo.PluginVersion);
            }
        }
        
#if (INTERNAL)
        public static void ShowIdleTimeTrackerUI(IPluginInfoModel pluginInfo)
        {
            if (_clients.ContainsKey(pluginInfo.ProductName))
            {
                var idleStopwatch = _clients[pluginInfo.ProductName].SessionIdleTimeStopwatch;
                var idleTimeDialog = new IdleTimeTrackerDashboard(_idleTimeTracker, idleStopwatch);
                idleTimeDialog.Show();
            }
        }
#endif

        public static void SubscribeIdleTimeStopwatch(IdleTimeStopWatch stopwatch)
        {
            _idleTimeTracker.SubscribeIdleTimeStopwatch(stopwatch);
        }

        public static bool UnsubscribeIdleTimeStopwatch(IdleTimeStopWatch stopwatch)
        {
            return _idleTimeTracker.UnsubscribeIdleTimeStopwatch(stopwatch);
        }

        private struct PcInfo
        {
            public string OsFullName { get; set; }
            public string OsVersion { get; set; }
            public string OsPlatform { get; set; }
            public double TotalSystemMemory { get; set; }
            public string SystemLanguage { get; set; }

            public List<CpuInfo> CpuInfos { get; set; }
            public List<GpuInfo> GpuInfos { get; set; }

            public bool IsInitialized { get; set; }
        }

        private struct CpuInfo
        {
            public string CpuName { get; set; }
            public string CpuNCores { get; set; }
            public string CpuNLogicalProcessor { get; set; }
            public string CpuMaxClockSpeed { get; set; }
        }

        private struct GpuInfo
        {
            public string GpuName { get; set; }
            public string GpuChip { get; set; }
            public double GpuRam { get; set; }
            public string GpuDriverVersion { get; set; }
            public string GpuStatus { get; set; }
        }

        private struct VersionInfo
        {
            public string RhinoVersion { get; set; }
            public string PluginVersion { get; set; }
        }

        private static PcInfo RetrieveProcessorInfo()
        {
            var computerInfo = new Microsoft.VisualBasic.Devices.ComputerInfo();
            var tot = computerInfo.TotalPhysicalMemory;

            var res = new PcInfo
            {
                TotalSystemMemory = MathUtilities.ConvertBytesToMegabytes(tot),
                OsFullName = computerInfo.OSFullName,
                OsVersion = computerInfo.OSVersion,
                OsPlatform = computerInfo.OSPlatform,
                CpuInfos = new List<CpuInfo>(),
                GpuInfos = new List<GpuInfo>()
            };

            var cpuSearcher = new ManagementObjectSearcher("Select * From Win32_processor");
            var cpuSearcherList = cpuSearcher.Get();
            foreach (var mo in cpuSearcherList)
            {
                var cpuInfo = new CpuInfo();
                try
                {
                    cpuInfo.CpuName = mo["Name"].ToString();
                    cpuInfo.CpuNCores = mo["NumberOfCores"].ToString();
                    cpuInfo.CpuNLogicalProcessor = mo["NumberOfLogicalProcessors"].ToString();
                    cpuInfo.CpuMaxClockSpeed = mo["MaxClockSpeed"].ToString();
                }
                catch
                {
                    cpuInfo.CpuName = "Error";
                    cpuInfo.CpuNCores = "Error";
                    cpuInfo.CpuNLogicalProcessor = "Error";
                    cpuInfo.CpuMaxClockSpeed = "Error";
                }

                res.CpuInfos.Add(cpuInfo);
            }

            var gpuSearcher = new ManagementObjectSearcher("Select * from Win32_VideoController");
            var gpuSearcherList = gpuSearcher.Get();
            foreach (var mo in gpuSearcherList)
            {
                var gpuInfo = new GpuInfo();
                try
                {
                    gpuInfo.GpuName = mo["Name"].ToString();
                    gpuInfo.GpuRam = MathUtilities.ConvertBytesToMegabytes((uint)mo["AdapterRAM"]);
                    gpuInfo.GpuDriverVersion = mo["DriverVersion"].ToString();
                    gpuInfo.GpuChip = mo["VideoProcessor"].ToString();
                    gpuInfo.GpuStatus = mo["Status"].ToString();
                }
                catch
                {
                    gpuInfo.GpuName = "Error";
                    gpuInfo.GpuRam = -1;
                    gpuInfo.GpuDriverVersion = "Error";
                    gpuInfo.GpuChip = "Error";
                    gpuInfo.GpuStatus = "Error";
                }

                res.GpuInfos.Add(gpuInfo);
            }

            res.SystemLanguage = System.Globalization.CultureInfo.CurrentCulture.EnglishName;

            res.IsInitialized = true;
            return res;
        }
    }
}
