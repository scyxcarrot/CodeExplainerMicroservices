using System;

namespace IDS.Interface.Tasks
{
    public interface ITaskActuator: IDisposable
    {
        double Progress { get; set; }

        void CheckPause();

        void CheckCancel();
    }
}
