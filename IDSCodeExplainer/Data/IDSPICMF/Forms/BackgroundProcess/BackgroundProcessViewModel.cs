using IDS.Interface.Tasks;

namespace IDS.PICMF.Forms.BackgroundProcess
{
    public class BackgroundProcessViewModel
    {
        public ITaskInvoker Model { get; }

        public string Name => Model.Description;

        public ITaskProgress TaskProgress => Model.TaskProgress;

        public PauseResumeButtonViewModel PauseResumeButton { get; }

        public StopButtonViewModel StopButton { get; }

        public LogPanelViewModel LogPanel { get; }

        public BackgroundProcessViewModel(ITaskInvoker model, LogPanelViewModel logPanel)
        {
            Model = model;
            PauseResumeButton = new PauseResumeButtonViewModel(Model);
            StopButton = new StopButtonViewModel(Model);
            LogPanel = logPanel;
        }
    }
}
