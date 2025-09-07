using IDS.Interface.Tasks;
using System;
using System.Windows.Input;

namespace IDS.PICMF.Forms.BackgroundProcess
{
    public class StopViewModelCommand : ICommand
    {
        private readonly ITaskInvoker _model;

        public StopViewModelCommand(ITaskInvoker model)
        {
            _model = model;
        }

        public bool CanExecute(object parameter)
        {
            _model.Update();
            return _model.CanStop();
        }

        public void Execute(object parameter)
        {
            _model.Stop();
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
