using IDS.Interface.Tasks;
using System;
using System.Collections.Generic;

namespace IDS.Core.V2.Tasks
{
    public class TaskQueue
    {
        private readonly Dictionary<Guid, ITaskInvoker> _taskInvokers = new Dictionary<Guid, ITaskInvoker>();

        public bool Add(ITaskInvoker taskInvoker)
        {
            if (_taskInvokers.ContainsKey(taskInvoker.Id))
            {
                return false;
            }

            _taskInvokers.Add(taskInvoker.Id, taskInvoker);
            return true;
        }

        public bool Remove(Guid taskInvokerId)
        {
            if (!_taskInvokers.ContainsKey(taskInvokerId))
            {
                return false;
            }

            _taskInvokers.Remove(taskInvokerId);
            return true;
        }

        private TaskStatus UpdateTaskInvoker(ITaskInvoker taskInvoker)
        {
            var status = taskInvoker.Update();
            switch (status)
            {
                case TaskStatus.Pause:
                case TaskStatus.Completed:
                case TaskStatus.Stopped:
                case TaskStatus.Failed:
                    CpuManager.Instance.Unsubscribe(taskInvoker.Id);
                    break;
                case TaskStatus.Running:
                    CpuManager.Instance.Subscribe(taskInvoker.Id, taskInvoker.EstimateCpuConsumption);
                    break;
                case TaskStatus.Initialed:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return status;
        }

        private bool TryInvokeTask(ITaskInvoker taskInvoker)
        {
            if (!CpuManager.Instance.HasCapacity(taskInvoker.EstimateCpuConsumption) || 
                !taskInvoker.Start())
            {
                return false;
            }
            
            CpuManager.Instance.Subscribe(taskInvoker.Id, taskInvoker.EstimateCpuConsumption);
            return true;
        }

        public void Update()
        {
            foreach (var taskInvokerKeyValuePair in _taskInvokers)
            {
                var taskInvoker = taskInvokerKeyValuePair.Value;
                var status = UpdateTaskInvoker(taskInvoker);
                if (status == TaskStatus.Initialed)
                {
                    TryInvokeTask(taskInvoker);
                }
            }
        }

        public void PauseAll()
        {
            foreach (var taskInvokerKeyValuePair in _taskInvokers)
            {
                var taskInvoker = taskInvokerKeyValuePair.Value;
                var status = UpdateTaskInvoker(taskInvoker);

                if (status == TaskStatus.Running)
                {
                    taskInvoker.Pause();
                    CpuManager.Instance.Unsubscribe(taskInvoker.Id);
                }
            }
        }

        public void StopAll()
        {
            foreach (var taskInvokerKeyValuePair in _taskInvokers)
            {
                var taskInvoker = taskInvokerKeyValuePair.Value;
                var status = UpdateTaskInvoker(taskInvoker);

                switch (status)
                {
                    case TaskStatus.Initialed:
                    case TaskStatus.Running:
                    case TaskStatus.Pause:
                        taskInvoker.Stop();
                        //Ignore the id if not exist
                        CpuManager.Instance.Unsubscribe(taskInvoker.Id);
                        break;
                    case TaskStatus.Completed:
                    case TaskStatus.Stopped:
                    case TaskStatus.Failed:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
