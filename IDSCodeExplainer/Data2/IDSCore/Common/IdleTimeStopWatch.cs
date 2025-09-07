using System.Diagnostics;

namespace IDS.Core.Common
{
    public class IdleTimeStopWatch
    {
        private readonly object _threadLock;
        private readonly Stopwatch _stopwatch;
        private long _editingTimeMs;
        private long _idleTimeMs;
        public long TotalTime => _stopwatch.ElapsedMilliseconds;

        public long EffectiveTimeMs => TotalTime - IdleTimeMs;

        public long EditingTimeMs // Time for user moving their mouse and it was part of EffectiveTimeMs but better separate it 
        {
            get
            {
                lock (_threadLock)
                {
                    return _editingTimeMs;
                }
            }
            private set
            {
                lock (_threadLock)
                {
                    _editingTimeMs = value;
                }
            }
        }

        public long IdleTimeMs
        {
            get
            {
                lock (_threadLock)
                {
                    return _idleTimeMs;
                }
            }
            private set
            {
                lock (_threadLock)
                {
                    _idleTimeMs = value;
                }
            }
        }

        public IdleTimeStopWatch()
        {
            _threadLock = new object();
            _stopwatch = new Stopwatch();
            IdleTimeMs = 0;
            EditingTimeMs = 0;
        }

        public void AccumulateIdleTimeMs(int milliseconds)
        {
            lock (_threadLock)
            {
                IdleTimeMs += milliseconds;
            }
        }

        public void AccumulateEditingTimeMs(int milliseconds)
        {
            lock (_threadLock)
            {
                EditingTimeMs += milliseconds;
            }
        }

        public void Start()
        {
            _stopwatch.Start();
        }

        public void Stop()
        {
            _stopwatch.Stop();
        }
    }
}
