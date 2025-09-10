using IDS.Core.V2.DataModels;
using IDS.Core.V2.Tasks;
using IDS.Interface.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IDS.Testing.UnitTests.V2
{
    [TestClass]
    public class TaskCommandTests
    {
        #region Fake
        // Use to check update progress, pause, resume and stop due to moq can't pass the owner instance to their mock method
        private class FakeSwitchTaskCommand : TaskCommand<ReferableValue<bool>, bool>
        {
            public FakeSwitchTaskCommand(Guid id, string description, ITaskActuator taskActuator) : 
                base(id, taskActuator, null)
            {
                Description = description;
            }

            public override string Description { get; }

            public override bool Execute(ReferableValue<bool> externalSwitch)
            {
                while (externalSwitch.Value)
                {
                    CheckControl();
                }

                CheckControl();
                return true;
            }
        }

        private class FakeProgressTaskCommand : TaskCommand<ReferableValue<double>, bool>
        {
            public FakeProgressTaskCommand(Guid id, string description, ITaskActuator taskActuator) :
                base(id, taskActuator, null)
            {
                Description = description;
            }

            public override string Description { get; }

            public override bool Execute(ReferableValue<double> setPoint)
            {
                SetCheckPoint(setPoint.Value);
                return true;
            }
        }

        #endregion

        [TestMethod]
        public void Basic()
        {
            const int testNum = 10;
            TaskControlFactoryUtilities.CreateTaskControlSet(out var controller, out var actuator);

            using (controller)
            using (actuator)
            {
                var taskId = Guid.NewGuid();
                var mockIsEvenNumTaskContent = new Mock<TaskCommand<int, bool>>(taskId, actuator, null);
                mockIsEvenNumTaskContent.Setup(t => t.Description).Returns("Mock Task");
                mockIsEvenNumTaskContent.Setup(t => t.Execute(It.IsAny<int>())).Returns<int>((i) => i % 2 == 0);

                var isEvenNumTask = mockIsEvenNumTaskContent.Object;
                var result = isEvenNumTask.Execute(testNum);
                Assert.AreEqual(true, result, $"{testNum} is not even number");
            }
        }

        [TestMethod]
        public void ExceedMaxProgress()
        {
            const double capMaxProgress = 99.99;
            const double overMaxProgress = capMaxProgress + 0.1;

            TaskControlFactoryUtilities.CreateTaskControlSet(out var controller, out var actuator);

            using(controller)
            using (actuator)
            {
                var taskId = Guid.NewGuid();
                var fakeTaskContent = new FakeProgressTaskCommand(taskId, "Fake Task", actuator);
                var setPoint = new ReferableValue<double>(overMaxProgress);
                fakeTaskContent.Execute(setPoint);
                Assert.AreEqual(capMaxProgress, fakeTaskContent.Progress, 0.1);
            }
        }

        [TestMethod]
        public void ExceedMinProgress()
        {
            const double capMinProgress = 0;
            const double overMinProgress = capMinProgress - 0.1;

            TaskControlFactoryUtilities.CreateTaskControlSet(out var controller, out var actuator);
            using (controller)
            using (actuator)
            {
                var taskId = Guid.NewGuid();
                var fakeTaskContent = new FakeProgressTaskCommand(taskId, "Fake Task", actuator);
                var setPoint = new ReferableValue<double>(overMinProgress);
                fakeTaskContent.Execute(setPoint);
                Assert.AreEqual(capMinProgress, fakeTaskContent.Progress, 0.1);
            }
        }

        [TestMethod]
        public void TaskIntegration()
        {
            TaskControlFactoryUtilities.CreateTaskControlSet(out var controller, out var actuator);

            var taskId = Guid.NewGuid();
            var fakeTaskContent = new FakeSwitchTaskCommand(taskId, "Fake Task", actuator);
            var externalSwitch = new ReferableValue<bool>(true);

            using(controller)
            using(actuator)
            using (var task = Task.Run(() => fakeTaskContent.Execute(externalSwitch)))
            {
                Thread.Sleep(1); // Some delay to make sure the other thread hit the function
                Assert.IsFalse(task.IsCompleted);

                externalSwitch.Value = false;
                task.Wait();
                Assert.IsTrue(task.IsCompleted);
            }
        }

        [TestMethod]
        public void Cancel()
        {
            TaskControlFactoryUtilities.CreateTaskControlSet(out var controller, out var actuator, out var cancellationTokenSource);

            var taskId = Guid.NewGuid();
            var fakeTaskContent = new FakeSwitchTaskCommand(taskId, "Fake Task", actuator);
            var externalSwitch = new ReferableValue<bool>(true);

            using (controller)
            using (actuator)
            using(cancellationTokenSource)
            using (var task = Task.Run(() => { fakeTaskContent.Execute(externalSwitch);}, cancellationTokenSource.Token))
            {
                controller.Stop();

                try
                {
                    task.Wait();
                }
                catch (AggregateException ex)
                {
                    Assert.IsTrue(ex.InnerException is OperationCanceledException);
                }

                Assert.IsTrue(task.IsCompleted);
            }
        }

        [TestMethod]
        public void Pause()
        {
            TaskControlFactoryUtilities.CreateTaskControlSet(out var controller, out var actuator);

            var taskId = Guid.NewGuid();
            var fakeTaskContent = new FakeSwitchTaskCommand(taskId, "Fake Task", actuator);
            var externalSwitch = new ReferableValue<bool>(false);

            controller.Pause();

            using(controller)
            using(actuator)
            using (var task = Task.Run(() => fakeTaskContent.Execute(externalSwitch)))
            {
                task.Wait(1);
                Assert.IsFalse(task.IsCompleted);  

                controller.Resume();
                task.Wait(1);
                Assert.IsTrue(task.IsCompleted);
            }
        }

        [TestMethod]
        public void CreateDynamicTaskCommand()
        {
            TaskControlFactoryUtilities.CreateTaskControlSet(out _, out var actuator);
            var taskId = Guid.NewGuid();

            var type = typeof(FakeSwitchTaskCommand);
            var instance = Activator.CreateInstance(type, taskId, "Fake Task", actuator); 

            Assert.IsNotNull(instance);
            Assert.IsTrue(instance is FakeSwitchTaskCommand);
        }
    }
}
