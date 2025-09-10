using IDS.Core.Common;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;

namespace IDS.Core.Forms
{
    /// <summary>
    /// Interaction logic for IdleTimeTrackerDashboard.xaml
    /// </summary>
    public partial class IdleTimeTrackerDashboard : Window
    {
        private readonly DispatcherTimer _dispatcherTimer;
        private readonly IdleTimeTracker _idleTimeTracker;
        private readonly IdleTimeStopWatch _sessionIdleTimeStopWatch;
        private IdleTimeStopWatch _resetableIdleTimeStopWatch;

        public IdleTimeTrackerDashboard(IdleTimeTracker idleTimeTracker, IdleTimeStopWatch sessionIdleTimeStopWatch)
        {
            InitializeComponent();
            _dispatcherTimer = new DispatcherTimer();
            _idleTimeTracker = idleTimeTracker;
            _sessionIdleTimeStopWatch = sessionIdleTimeStopWatch;
            _resetableIdleTimeStopWatch = new IdleTimeStopWatch();
            _idleTimeTracker.SubscribeIdleTimeStopwatch(_resetableIdleTimeStopWatch);
        }

        private void IdleTimeTrackerWatch_OnLoaded(object sender, RoutedEventArgs e)
        {
            _dispatcherTimer.Tick += DispatcherTimer_Tick;
            _dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            _dispatcherTimer.Start();
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            TxtSessionIdle.Text = $"{_sessionIdleTimeStopWatch.IdleTimeMs / 1000} sec";
            TxtSessionShortBreak.Text = $"{_sessionIdleTimeStopWatch.EditingTimeMs / 1000} sec";
            TxtSessionEffective.Text = $"{_sessionIdleTimeStopWatch.EffectiveTimeMs / 1000} sec";
            TxtSessionTotal.Text = $"{_sessionIdleTimeStopWatch.TotalTime / 1000} sec";

            TxtIdle.Text = $"{_resetableIdleTimeStopWatch.IdleTimeMs / 1000} sec";
            TxtShortBreak.Text = $"{_resetableIdleTimeStopWatch.EditingTimeMs / 1000} sec";
            TxtEffective.Text = $"{_resetableIdleTimeStopWatch.EffectiveTimeMs / 1000} sec";
            TxtTotal.Text = $"{_resetableIdleTimeStopWatch.TotalTime / 1000} sec";
        }

        private void IdleTimeTrackerWatch_OnClosing(object sender, CancelEventArgs e)
        {
            _idleTimeTracker.UnsubscribeIdleTimeStopwatch(_resetableIdleTimeStopWatch);
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            _idleTimeTracker.UnsubscribeIdleTimeStopwatch(_resetableIdleTimeStopWatch);
            _resetableIdleTimeStopWatch = new IdleTimeStopWatch();
            _idleTimeTracker.SubscribeIdleTimeStopwatch(_resetableIdleTimeStopWatch);
        }
    }
}
