using IDS.Core.V2.Utilities;
using System;
using System.Diagnostics;

namespace IDS.CMF.V2.Tracking
{
    public class TimeTracking : IDisposable
    {
        private readonly Stopwatch _stopwatch;
        private readonly string _actionName;
        private readonly Action<string, string> _onDispose;

        public TimeTracking(string actionName, Action<string, string> onDispose)
        {
            _stopwatch = Stopwatch.StartNew();
            _actionName = actionName;
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            _stopwatch.Stop();
            _onDispose?.Invoke(
                $"{_actionName} {TrackingConstants.TimeMinuteSecondUnit}",
                StringUtilitiesV2.ElapsedTimeSpanToString(_stopwatch.Elapsed));
        }

        public static IDisposable NewInstance(string actionName, Action<string, string> onDispose)
        {
            return new TimeTracking(actionName, onDispose);
        }

        public static IDisposable NewInstance(string actionName, Func<string, string, bool> onDispose)
        {
            return new TimeTracking(actionName, (param1, param2) => onDispose(param1, param2));
        }
    }
}
