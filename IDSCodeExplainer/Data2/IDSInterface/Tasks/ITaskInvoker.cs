using System;

namespace IDS.Interface.Tasks
{
    public interface ITaskInvoker: IDisposable
    {
        Guid Id {get;}

        string Description { get; }

        int EstimateCpuConsumption { get; }

        ITaskProgress TaskProgress { get; }

        bool Start();

        bool Pause();

        bool Resume();

        bool Stop();

        bool CanStop();

        bool IsCompleted();

        TaskStatus Update();
    }
}
