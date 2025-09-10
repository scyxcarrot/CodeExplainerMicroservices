using IDS.Interface.Tasks;
using System.Threading;

namespace IDS.Core.V2.Tasks
{
    public static class TaskControlFactory
    {
        public static void CreateTaskControlSet(TaskProgress taskProgress, out ITaskController controller, 
            out ITaskActuator actuator, out CancellationTokenSource cancellationTokenSource)
        {
            var mutex = new Mutex();
            cancellationTokenSource = new CancellationTokenSource();
            controller = new TaskController(mutex, cancellationTokenSource, taskProgress);
            actuator = new TaskActuator(mutex, cancellationTokenSource, taskProgress);
        }
    }
}
