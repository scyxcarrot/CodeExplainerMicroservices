using IDS.Core.V2.DataModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace IDS.Testing.UnitTests.V2
{
    /// <summary>
    /// Summary description for TaskController
    /// </summary>
    [TestClass]
    public class TaskControlTests
    {
        [TestMethod]
        public void TaskPauseTest()
        {
            TaskControlFactoryUtilities.CreateTaskControlSet(out var controller, out var actuator);

            using(controller)
            using(actuator)
            using(var task = new Task(() =>
                  {
                      actuator.CheckPause();
                  }))
            {
                controller.Pause();
                task.Start();
                task.Wait(1);
                Assert.IsFalse(task.IsCompleted);

                controller.Resume();
                task.Wait(1);
                Assert.IsTrue(task.IsCompleted);
            }
        }

        [TestMethod]
        public void TaskCancelTest()
        {
            TaskControlFactoryUtilities.CreateTaskControlSet(out var controller, out var actuator, out var cancellationTokenSource);

            var control = new ReferableValue<bool>(true);

            using (controller)
            using(actuator)
            using (var task = new Task(() =>
                   {
                       while (control.Value)
                       {
                           actuator.CheckCancel();
                       }
                   }, cancellationTokenSource.Token))
            {

                task.Start();
                controller.Stop();

                try
                {
                    task.Wait(10);
                }
                catch (AggregateException ex)
                {
                    Assert.IsTrue(ex.InnerException is OperationCanceledException);
                }

                Assert.IsTrue(task.IsCanceled);
                Assert.IsTrue(task.IsCompleted);
                Assert.IsFalse(task.IsFaulted);
            }
        }
    }
}
