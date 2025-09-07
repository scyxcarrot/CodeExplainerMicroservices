using IDS.Interface.Tasks;
using System.Windows.Input;

namespace IDS.PICMF.Forms.BackgroundProcess
{
    public class StopButtonViewModel
    {
        private readonly ITaskInvoker _model;
        public ICommand StopCommand { get; }

        public StopButtonViewModel(ITaskInvoker model)
        {
            _model = model;
            StopCommand = new StopViewModelCommand(_model);
        }
    }
}
