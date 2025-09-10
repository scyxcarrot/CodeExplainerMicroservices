using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Threading;

namespace IDS.Core.Common
{
    public class IdleTimeTracker: IDisposable
    {
        private const uint IncrementThresholdMs = 1000;

        private readonly object _threadLock;
        private readonly WindowsMouseHook _mouseHook;
        private readonly int _mouseIdleMsTolerant;
        private readonly List<IdleTimeStopWatch> _idleTimeStopWatches;
        private readonly DispatcherTimer _dispatcherTimer;
        private int _prevTickCount;
        private int _mouseIdleConfirmationTimerMs;
        private bool _whenPcLogOut;

        public IdleTimeTracker(uint threadId, int mouseIdleMsTolerant)
        {
            _threadLock = new object();
            _mouseHook = new WindowsMouseHook(threadId);
            _mouseIdleMsTolerant = mouseIdleMsTolerant;
            _idleTimeStopWatches = new List<IdleTimeStopWatch>();
            _dispatcherTimer = new DispatcherTimer(DispatcherPriority.Normal);
            _prevTickCount = 0;
            _mouseIdleConfirmationTimerMs = mouseIdleMsTolerant;
            _whenPcLogOut = false;

            _mouseHook.MouseMove += MouseHook_MouseMove;
            _mouseHook.Install();
            // Must subscribe it in the rhino application thread
            ComponentDispatcher.ThreadIdle += AppIdleEventHandler;

            _dispatcherTimer.Tick += AppIdleEventHandler;
            // Interval is 100ms
            _dispatcherTimer.Interval = new TimeSpan(0, 0, 0,0,100);
            _dispatcherTimer.Start();
            SystemEvents.SessionSwitch += SystemEventsOnSessionSwitch;
        }

        private void SystemEventsOnSessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            switch (e.Reason)
            {
                case SessionSwitchReason.SessionLock:
                    _whenPcLogOut = true;
                    _mouseIdleConfirmationTimerMs = 0;
                    break;
                case SessionSwitchReason.SessionUnlock:
                    _whenPcLogOut = false;
                    break;
            }
        }

        // Call this constructor in Main/UI thread
        public IdleTimeTracker(int mouseIdleThreshold) : this(GetCurrentThreadId(), mouseIdleThreshold)
        {
        }

        public void Dispose()
        {
            _mouseHook.MouseMove -= MouseHook_MouseMove;
            _mouseHook.Dispose();
            _dispatcherTimer.Tick -= AppIdleEventHandler;
            SystemEvents.SessionSwitch -= SystemEventsOnSessionSwitch;
        }

        private void MouseHook_MouseMove(WindowsMouseHook.MouseHookEventStruct mouseStruct)
        {
            _mouseIdleConfirmationTimerMs = _mouseIdleMsTolerant;
        }

        private void AppIdleEventHandler(object sender, EventArgs e)
        {
            var tickDiff = Environment.TickCount - _prevTickCount;
            _prevTickCount = Environment.TickCount;

            // When PC had logout, it will directly count as IDLE 
            if (!_whenPcLogOut)
            {
                // If busy, the tick count different will be large;
                // Tick count overflow also cause tick count different will be large;
                // Skip this 2 condition
                if (Math.Abs(tickDiff) >= IncrementThresholdMs)
                {
                    return;
                }

                if (_mouseIdleConfirmationTimerMs > 0)
                {
                    lock (_threadLock)
                    {
                        _idleTimeStopWatches.ForEach(t => t.AccumulateEditingTimeMs(tickDiff));
                    }

                    _mouseIdleConfirmationTimerMs -= tickDiff;
                    return;
                }
            }

            lock (_threadLock)
            {
                _idleTimeStopWatches.ForEach(t => t.AccumulateIdleTimeMs(tickDiff));
            }
        }

        public void SubscribeIdleTimeStopwatch(IdleTimeStopWatch stopwatch)
        {
            lock (_threadLock)
            {
                if (_idleTimeStopWatches.Contains(stopwatch))
                {
                    return;
                }
                _idleTimeStopWatches.Add(stopwatch);
                stopwatch.Start();
            }
        }

        public bool UnsubscribeIdleTimeStopwatch(IdleTimeStopWatch stopwatch)
        {
            lock (_threadLock)
            {
                if (!_idleTimeStopWatches.Remove(stopwatch))
                {
                    return false;
                }

                stopwatch.Stop();
                return true;

            }
        }

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();
    }
}
