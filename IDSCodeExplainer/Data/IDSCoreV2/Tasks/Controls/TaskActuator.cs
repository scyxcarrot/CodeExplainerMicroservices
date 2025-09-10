using IDS.Interface.Tasks;
using System.Threading;

namespace IDS.Core.V2.Tasks
{
    public class TaskActuator: ITaskActuator
    {
        private readonly Mutex _mutex;
        private readonly CancellationTokenSource _cancellationTokenSourceSource;
        private readonly TaskProgress _taskProgress;

        public double Progress
        {
            get
            {
                lock (_taskProgress)
                {
                    return _taskProgress.Progress;
                }
            }
            set
            {
                lock (_taskProgress)
                {
                    _taskProgress.Progress = value;
                }
            }
        }

        public TaskActuator(Mutex mutex, CancellationTokenSource cancellationTokenSource, TaskProgress taskProgress)
        {
            _mutex = mutex;
            _cancellationTokenSourceSource = cancellationTokenSource;
            _taskProgress = taskProgress;
        }

        public void CheckPause()
        {
            _mutex.WaitOne();
            _mutex.ReleaseMutex();
        }

        public void CheckCancel()
        {
            _cancellationTokenSourceSource.Token.ThrowIfCancellationRequested();
        }

        public void Dispose()
        {
            _mutex.Dispose();
            _cancellationTokenSourceSource.Dispose();
        }
    }
}
