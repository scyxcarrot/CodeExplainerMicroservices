using System;

namespace IDS.Interface.Tasks
{
    public interface ITaskController: IDisposable
    {
        ITaskProgress TaskProgress { get; }

        bool Pause();

        bool Resume();

        void Stop();
    }
}
