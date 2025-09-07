using System.Collections.Generic;
using IDS.Interface.Tools;

namespace IDS.Core.V2.ExternalTools
{
    public class MsaiTrackingInfo
    {
        private readonly IConsole _console;

        private Dictionary<string, string> _trackingParameters;

        public Dictionary<string, string> TrackingParameters
        {

            get
            {
                lock (_trackingParameters)
                {
                    return _trackingParameters;
                }
            }
        }

        private Dictionary<string, double> _trackingMetrics;

        public Dictionary<string, double> TrackingMetrics
        {

            get
            {
                lock (_trackingMetrics)
                {
                    return _trackingMetrics;
                }
            }
        }

        public MsaiTrackingInfo(IConsole console)
        {
            _console = console;
            _trackingParameters = new Dictionary<string, string>();
            _trackingMetrics = new Dictionary<string, double>();
        }

        public bool AddTrackingParameterSafely(string key, string value)
        {
            lock (_trackingParameters)
            {
                if (_trackingParameters.ContainsKey(key))
                {
                    _console.WriteErrorLine($"Duplicated key found for MSAI Tracking for key {key}. You can still proceed with your work but kindly report this to development team. Thank you!");
                    return false;
                }

                _trackingParameters.Add(key, value);
                return true;
            }
        }

        public void ForceAddTrackingParameterSafely(string key, string value)
        {
            lock (_trackingParameters)
            {
                if (_trackingParameters.ContainsKey(key))
                {
                    _trackingParameters[key] = value;
                }

                _trackingParameters.Add(key, value);
            }
        }

        public bool AddTrackingMetricSafely(string key, double value)
        {
            lock (_trackingMetrics)
            {
                if (_trackingMetrics.ContainsKey(key))
                {
                    _console.WriteErrorLine($"Duplicated key found for MSAI Tracking for key {key}. You can still proceed with your work but kindly report this to development team. Thank you!");
                    return false;
                }

                _trackingMetrics.Add(key, value); 
                return true;
            }

        }
    }
}
