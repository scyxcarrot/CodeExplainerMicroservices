using IDS.Interface.Tasks;
using IDS.Interface.Tools;
using System;
using System.Threading;
using System.Threading.Tasks;
using TaskStatus = IDS.Interface.Tasks.TaskStatus;

namespace IDS.Core.V2.Tasks
{
    public abstract class TaskInvoker<TParameter, TResult, TTaskCommand>: ITaskInvoker 
        where TTaskCommand: ITaskCommand<TParameter, TResult>
    {
        #region Task Components
        protected readonly IConsole console;

        private TaskStatus _status;

        private readonly ITaskCommand<TParameter, TResult> _taskCommand;

        private readonly ITaskController _controller;

        private readonly ITaskActuator _actuator;

        private readonly CancellationTokenSource _cancellationTokenSource;

        private Task<TResult> _task;
        #endregion

        #region Information
        public Guid Id { get; }

        public int EstimateCpuConsumption => _taskCommand.EstimateConsumption;

        public ITaskProgress TaskProgress { get; }

        public string Description => _taskCommand.Description;
        #endregion

        protected TaskInvoker(Guid id, IConsole console)
        {
            Id = id;
            this.console = console;
            
            var taskProgress = new TaskProgress(); 
            TaskProgress = taskProgress;
            TaskControlFactory.CreateTaskControlSet(taskProgress, out _controller, out _actuator, out _cancellationTokenSource);
            
            var type = typeof(TTaskCommand);
            _taskCommand = (TTaskCommand)Activator.CreateInstance(type, Id, _actuator, this.console);

        }

        #region ITaskInvoke

        public bool Start()
        {
            if (_status != TaskStatus.Initialed)
            {
                console.WriteWarningLine("This task had been try to start again");
                return false;
            }

            var parameters = PrepareParameters();
            _task = Task.Run(() => _taskCommand.Execute(parameters), _cancellationTokenSource.Token);
            _status = TaskStatus.Running;
            return true;
        }

        public bool Pause()
        {
            if (_status != TaskStatus.Running)
            {
                return false;
            }

            _controller.Pause();
            _status = TaskStatus.Pause;
            return true;
        }

        public bool Resume()
        {
            if (_status != TaskStatus.Pause)
            {
                return false;
            }

            _controller.Resume();
            _status = TaskStatus.Running;
            return true;
        }

        private void ForceToStop()
        {
            _controller.Stop();

            if (_status == TaskStatus.Pause)
            {
                _controller.Resume();
            }
        }

        public bool Stop()
        {
            if (!CanStop())
            {
                return false;
            }

            ForceToStop();
            return true;
        }

        public bool CanStop()
        {
            return !IsCompleted();
        }

        public bool IsCompleted()
        {
            return _status == TaskStatus.Stopped ||
                   _status == TaskStatus.Completed ||
                   _status == TaskStatus.Failed;
        }

        private void WaitTaskSafe()
        {
            try
            {
                _task.Wait();
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is TaskCanceledException)
                {
                    console.WriteWarningLine($"Task cancelled ");
                }
                else
                {
                    // TODO: Create exception utilities to print full exception info with stack trace and inner exception
                    console.WriteErrorLine($"Task failed due to: {ex.InnerException.Message}");
                }
            }
            catch (Exception ex)
            {
                // TODO: Create exception utilities to print full exception info with stack trace and inner exception
                console.WriteErrorLine($"Task failed due to: {ex.Message}");
            }
        }

        private void TryCompleteTaskSafe()
        {
            WaitTaskSafe();

            if (_task.IsCanceled)
            {
                _status = TaskStatus.Stopped;
            }
            else if (_task.IsFaulted)
            {
                _status = TaskStatus.Stopped;
            }
            else
            {
                _status = TaskStatus.Completed;
                var result = _task.Result;
                ProcessResult(result);
            }
        }

        private void UpdateRunningTaskStatus()
        {
            if (_status == TaskStatus.Running && 
                _task.IsCompleted)
            {
                TryCompleteTaskSafe();
            }
        }

        public TaskStatus Update()
        {
            if (_status == TaskStatus.Stopped ||
                _status == TaskStatus.Completed ||
                _status == TaskStatus.Failed)
            {
                return _status;
            }

            UpdateRunningTaskStatus();

            return _status;
        }
        #endregion

        #region Abstract Method

        protected abstract TParameter PrepareParameters();

        protected abstract void ProcessResult(TResult result);

        #endregion

        public virtual void Dispose()
        {
            ForceToStop();
            _controller?.Dispose();
            _actuator?.Dispose();
            _cancellationTokenSource?.Dispose();
            _task?.Dispose();
        }
    }
}
