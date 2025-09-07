using IDS.Interface.Tasks;
using IDS.Interface.Tools;
using System;
using System.Diagnostics;
using System.Threading;

namespace IDS.Core.V2.Tasks
{
    public abstract class TaskCommand<TParams, TResult>: ITaskCommand<TParams, TResult>
    {
        public const int DefaultEstimateConsumption = 1;

        #region Information

        public virtual int EstimateConsumption => DefaultEstimateConsumption;

        public Guid Id { get; }

        public abstract string Description { get; }

        #endregion

        #region Control
        private readonly ITaskActuator _taskActuator;

        public double Progress
        {
            get => _taskActuator.Progress;
            private set => _taskActuator.Progress = value;
        }

        #endregion

        protected readonly IConsole console;

        protected TaskCommand(Guid id, ITaskActuator taskActuator, IConsole console)
        {
            Id = id;
            _taskActuator = taskActuator;
            this.console = console;
        }

        public abstract TResult Execute(TParams parameters);


        // Move this to actuator so can unit test better, and more SOLID
        protected void CheckControl()
        {
            _taskActuator.CheckPause();
            _taskActuator.CheckCancel();
        }

        // Move this to actuator so can unit test better, and more SOLID
        protected void SetCheckPoint(double progress)
        {
            Progress = progress;
            CheckControl();
        }

        protected void NonBlockingDelay(long delayMilliseconds)
        {
            var delayWatch = new Stopwatch();
            delayWatch.Start();
            while (delayWatch.ElapsedMilliseconds <= delayMilliseconds)
            {
                CheckControl();
                Thread.Sleep(1);
            }
        }
    }
}
