using IDS.Core.V2.Tasks;
using IDS.Interface.Tasks;
using System.Threading;

namespace IDS.Testing
{
    public class TaskControlFactoryUtilities
    {
        public static void CreateTaskControlSet(out ITaskController controller,
            out ITaskActuator actuator, out CancellationTokenSource cancellationTokenSource)
        {
            var taskProgress = new TaskProgress();
            TaskControlFactory.CreateTaskControlSet(taskProgress, out controller, out actuator, out cancellationTokenSource);
        }

        public static void CreateTaskControlSet(out ITaskController controller, out ITaskActuator actuator)
        {
            CreateTaskControlSet(out controller, out actuator, out _);
        }
    }
}
