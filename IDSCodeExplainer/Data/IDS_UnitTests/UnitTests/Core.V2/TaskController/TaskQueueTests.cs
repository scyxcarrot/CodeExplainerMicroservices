using IDS.Core.V2.DataModels;
using IDS.Core.V2.Tasks;
using IDS.Interface.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;

namespace IDS.Testing.UnitTests.V2
{
    [TestClass]
    public class TaskQueueTests
    {
        private class MockTaskInvokerCreator
        {
            public Guid Id { get; }

            public int CpuConsumption { get; }

            public ReferableValue<TaskStatus> StatusRef { get; }

            public MockTaskInvokerCreator(int cpuConsumption)
            {
                Id = Guid.NewGuid();
                CpuConsumption = cpuConsumption;
                StatusRef = new ReferableValue<TaskStatus>(TaskStatus.Initialed);
            }

            public ITaskInvoker Create()
            {
                var mockTaskInvoker = new Mock<ITaskInvoker>();
                mockTaskInvoker.SetupGet(i => i.Id).Returns(Id);
                mockTaskInvoker.SetupGet(i => i.EstimateCpuConsumption).Returns(CpuConsumption);
                mockTaskInvoker.Setup(i => i.Start()).Returns(() =>
                {
                    if (StatusRef.Value == TaskStatus.Initialed)
                    {
                        StatusRef.Value = TaskStatus.Running;
                        return true;
                    }
                    return false;
                });
                mockTaskInvoker.Setup(i => i.Pause()).Returns(() =>
                {
                    if (StatusRef.Value == TaskStatus.Running)
                    {
                        StatusRef.Value = TaskStatus.Pause;
                        return true;
                    }
                    return false;
                });
                mockTaskInvoker.Setup(i => i.Resume()).Returns(() =>
                {
                    if (StatusRef.Value == TaskStatus.Pause)
                    {
                        StatusRef.Value = TaskStatus.Running;
                        return true;
                    }
                    return false;
                });
                mockTaskInvoker.Setup(i => i.Stop()).Returns(() =>
                {
                    if (StatusRef.Value == TaskStatus.Initialed ||
                        StatusRef.Value == TaskStatus.Pause ||
                        StatusRef.Value == TaskStatus.Running)
                    {
                        StatusRef.Value = TaskStatus.Stopped;
                        return true;
                    }
                    return false;
                });

                mockTaskInvoker.Setup(i => i.Update()).Returns(() => StatusRef.Value);

                return mockTaskInvoker.Object;
            }

        }

        [TestInitialize]
        public void TestInitialize()
        {
            CpuManager.Instance.Reset();
        }

        [TestMethod]
        public void TestGetCpuCore_Positive()
        {
            Assert.AreEqual(Environment.ProcessorCount - 1, CpuManager.Instance.MaxLogicalCoreResource);
            Assert.IsTrue(CpuManager.Instance.MaxLogicalCoreResource > 0);
        }

        [TestMethod]
        public void TestAddTaskInvokerToQueue_Positive()
        {
            var taskQueue = new TaskQueue();

            var id = Guid.NewGuid();
            var mockTaskInvoker = new Mock<ITaskInvoker>();
            mockTaskInvoker.SetupGet(i => i.Id).Returns(id);

            Assert.IsTrue(taskQueue.Add(mockTaskInvoker.Object));
        }

        [TestMethod]
        public void TestAddTwiceTaskInvokerToQueue_Negative()
        {
            var taskQueue = new TaskQueue();

            var id = Guid.NewGuid();
            var mockTaskInvoker = new Mock<ITaskInvoker>();
            mockTaskInvoker.SetupGet(i => i.Id).Returns(id);
            var taskInvoker = mockTaskInvoker.Object;

            Assert.IsTrue(taskQueue.Add(taskInvoker));
            Assert.IsFalse(taskQueue.Add(taskInvoker));
        }

        [TestMethod]
        public void TestRemoveTaskInvokerFromQueue_Positive()
        {
            var taskQueue = new TaskQueue();

            var id = Guid.NewGuid();
            var mockTaskInvoker = new Mock<ITaskInvoker>();
            mockTaskInvoker.SetupGet(i => i.Id).Returns(id);
            var taskInvoker = mockTaskInvoker.Object;

            Assert.IsTrue(taskQueue.Add(taskInvoker));
            Assert.IsTrue(taskQueue.Remove(id));
        }

        [TestMethod]
        public void TestRemoveNonExistingTaskInvokerFromQueue_Negative()
        {
            var taskQueue = new TaskQueue();

            var id = Guid.NewGuid();

            Assert.IsFalse(taskQueue.Remove(id));
        }

        [TestMethod]
        public void TestUpdateQueue_Positive()
        {
            var taskQueue = new TaskQueue();

            var creator = new MockTaskInvokerCreator(1);
            var taskInvoker = creator.Create();

            Assert.IsTrue(taskQueue.Add(taskInvoker));

            taskQueue.Update();

            Assert.AreEqual(TaskStatus.Running, creator.StatusRef.Value);
        }

        [TestMethod]
        public void TestUpdateQueueWithFullCpuConsumption_Positive()
        {
            var taskQueue = new TaskQueue();

            var creator1 = new MockTaskInvokerCreator(-1);
            var taskInvoker1 = creator1.Create();
            
            var creator2 = new MockTaskInvokerCreator(1);
            var taskInvoker2 = creator2.Create();

            Assert.IsTrue(taskQueue.Add(taskInvoker1));
            Assert.IsTrue(taskQueue.Add(taskInvoker2));

            taskQueue.Update();

            Assert.AreEqual(TaskStatus.Running, creator1.StatusRef.Value);
            Assert.AreEqual(TaskStatus.Initialed, creator2.StatusRef.Value);
        }

        [TestMethod]
        public void TestPauseAllTaskInvoker_Positive()
        {
            var taskQueue = new TaskQueue();

            var creator1 = new MockTaskInvokerCreator(1);
            var taskInvoker1 = creator1.Create();

            var creator2 = new MockTaskInvokerCreator(1);
            var taskInvoker2 = creator2.Create();

            Assert.IsTrue(taskQueue.Add(taskInvoker1));
            Assert.IsTrue(taskQueue.Add(taskInvoker2));
            Assert.IsTrue(CpuManager.Instance.MaxLogicalCoreResource > 2);

            taskQueue.Update();

            Assert.AreEqual(TaskStatus.Running, creator1.StatusRef.Value);
            Assert.AreEqual(TaskStatus.Running, creator2.StatusRef.Value);

            taskQueue.PauseAll();

            Assert.AreEqual(TaskStatus.Pause, creator1.StatusRef.Value);
            Assert.AreEqual(TaskStatus.Pause, creator2.StatusRef.Value);
        }

        [TestMethod]
        public void TestStopAllTaskInvoker_Positive()
        {
            var taskQueue = new TaskQueue();

            var creator1 = new MockTaskInvokerCreator(1);
            var taskInvoker1 = creator1.Create();

            var creator2 = new MockTaskInvokerCreator(1);
            var taskInvoker2 = creator2.Create();

            Assert.IsTrue(taskQueue.Add(taskInvoker1));
            Assert.IsTrue(taskQueue.Add(taskInvoker2));
            Assert.IsTrue(CpuManager.Instance.MaxLogicalCoreResource > 2);

            taskQueue.Update();

            Assert.AreEqual(TaskStatus.Running, creator1.StatusRef.Value);
            Assert.AreEqual(TaskStatus.Running, creator2.StatusRef.Value);

            taskQueue.StopAll();

            Assert.AreEqual(TaskStatus.Stopped, creator1.StatusRef.Value);
            Assert.AreEqual(TaskStatus.Stopped, creator2.StatusRef.Value);
        }
    }
}
