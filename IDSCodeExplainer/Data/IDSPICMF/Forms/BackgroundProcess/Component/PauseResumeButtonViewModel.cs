using IDS.Interface.Tasks;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;

namespace IDS.PICMF.Forms.BackgroundProcess
{
    public class PauseResumeButtonViewModel: INotifyPropertyChanged
    {
        private readonly ITaskInvoker _model;

        private readonly DispatcherTimer _dispatcherTimer;

        public bool CanPause => _model.Update() != TaskStatus.Running;

        public ICommand PauseResumeCommand { get; }

        public PauseResumeButtonViewModel(ITaskInvoker model)
        {
            _model = model;

            var pauseResumeCommand = new PauseResumeViewModelCommand(model);
            pauseResumeCommand.PostExecuteCallback += () =>
            {
                OnPropertyChanged(nameof(CanPause));
            };
            PauseResumeCommand = pauseResumeCommand;

            _dispatcherTimer = new DispatcherTimer(DispatcherPriority.Normal);
            _dispatcherTimer.Tick += InvokerStatusSecondUpdate;
            _dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 1, 0);
            _dispatcherTimer.Start();
        }

        private void InvokerStatusSecondUpdate(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(CanPause));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
