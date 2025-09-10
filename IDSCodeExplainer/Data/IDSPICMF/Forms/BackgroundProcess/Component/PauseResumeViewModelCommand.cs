using IDS.Interface.Tasks;
using System;
using System.Windows.Input;

namespace IDS.PICMF.Forms.BackgroundProcess
{
    public class PauseResumeViewModelCommand : ICommand
    {
        private readonly ITaskInvoker _model;

        public delegate void PostPauseResumeExecute();

        public PostPauseResumeExecute PostExecuteCallback;

        public PauseResumeViewModelCommand(ITaskInvoker model)
        {
            _model = model;
        }

        public bool CanExecute(object parameter)
        {
            var taskStatus = _model.Update();
            return taskStatus == TaskStatus.Running ||
                   taskStatus == TaskStatus.Pause;
        }

        public void Execute(object parameter)
        {
            var taskStatus = _model.Update();
            if (taskStatus == TaskStatus.Running)
            {
                _model.Pause();
            }
            else if (taskStatus == TaskStatus.Pause)
            {
                _model.Resume();
            }
            else
            {
                return;
            }

            PostExecuteCallback?.Invoke();
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }

}
