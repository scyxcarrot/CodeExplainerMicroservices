using IDS.Interface.Tasks;
using System.Threading;

namespace IDS.Core.V2.Tasks
{
    public class TaskController: ITaskController
    {
        private bool _isPause;
        private readonly Mutex _mutex;
        private readonly CancellationTokenSource _cancellationTokenSource;
        public ITaskProgress TaskProgress { get; }

        public TaskController(Mutex mutex, CancellationTokenSource cancellationTokenSource, ITaskProgress taskProgress)
        {
            _isPause = false;
            _mutex = mutex;
            _cancellationTokenSource = cancellationTokenSource;
            TaskProgress = taskProgress;
        }

        public bool Pause()
        {
            if (_isPause)
            {
                return false;
            }

            _isPause = true;
            return _mutex.WaitOne();
        }

        public bool Resume()
        {
            if (!_isPause)
            {
                return false;
            }

            _isPause = false;
            _mutex.ReleaseMutex();
            return true;
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
        }

        public void Dispose()
        {
            _mutex.Dispose();
            _cancellationTokenSource.Dispose();
        }
    }
}
