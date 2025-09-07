using IDS.Core.V2.Tasks;
using IDS.Interface.Tasks;
using IDS.Interface.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using System;
using System.Diagnostics;
using System.Threading;

namespace IDS.Testing.UnitTests.V2
{
    [Ignore]
    [TestClass]
    public class TaskInvokerTests
    {
        #region Fake
        // Use to check update progress, pause, resume and stop due to moq can't pass the owner instance to their mock method
        public class FakeTaskCommand : TaskCommand<int, bool>
        {
            public FakeTaskCommand(Guid id, ITaskActuator taskActuator, IConsole console) :
                base(id, taskActuator, console)
            {
            }

            public override string Description => "Fake Task Command";

            public override bool Execute(int count)
            {
                if (count < 0)
                {
                    throw new ArgumentOutOfRangeException($"{count} is negative value");
                }

                for (var i = 0; i < count; i++)
                {
                    CheckControl();
                    Thread.Sleep(1);
                }

                return true;
            }
        }

        #endregion

        private ITaskInvoker CreateFakeTaskInvoker(int delayMs, out Mock<TaskInvoker<int, bool, FakeTaskCommand>> mockTaskInvoker)
        {
            var console = new TestConsole();
            var id = Guid.NewGuid();
            mockTaskInvoker = new Mock<TaskInvoker<int, bool, FakeTaskCommand>>(id, console);

            mockTaskInvoker.Protected().Setup<int>("PrepareParameters").Returns(delayMs);
            mockTaskInvoker.Protected().Setup("ProcessResult", ItExpr.IsAny<bool>()).Callback((bool result) =>
            {
                Assert.IsTrue(result);
            });
            return mockTaskInvoker.Object;
        }

        [TestMethod]
        public void TestCreateTaskInvoker_Positive()
        {
            using (var taskInvoker = CreateFakeTaskInvoker(5, out _))
            {
                Assert.IsNotNull(taskInvoker);
            }
        }

        [TestMethod]
        public void TestStartInvoker_Positive()
        {
            using (var taskInvoker = CreateFakeTaskInvoker(5, out var mockTaskInvoker))
            {
                Assert.IsNotNull(taskInvoker);

                Assert.IsTrue(taskInvoker.Start());
                mockTaskInvoker.Protected().Verify("PrepareParameters", Times.Once());

                var startedStatus = taskInvoker.Update();
                Assert.AreEqual(TaskStatus.Running, startedStatus);
            }
        }

        [TestMethod]
        public void TestStartInvoker_Negative()
        {
            using(var taskInvoker = CreateFakeTaskInvoker(5, out var mockTaskInvoker))
            {
                Assert.IsNotNull(taskInvoker);

                Assert.IsTrue(taskInvoker.Start());
                mockTaskInvoker.Protected().Verify("PrepareParameters", Times.Once());
                var startedStatus = taskInvoker.Update();
                Assert.AreEqual(TaskStatus.Running, startedStatus);

                Assert.IsFalse(taskInvoker.Start());
            }
        }

        [TestMethod]
        public void TestPauseInvoker_Positive()
        {
            using (var taskInvoker = CreateFakeTaskInvoker(5, out var mockTaskInvoker))
            {
                Assert.IsNotNull(taskInvoker);

                Assert.IsTrue(taskInvoker.Start());
                mockTaskInvoker.Protected().Verify("PrepareParameters", Times.Once());
                var startedStatus = taskInvoker.Update();
                Assert.AreEqual(TaskStatus.Running, startedStatus);

                Assert.IsTrue(taskInvoker.Pause());
                var pausedStatus = taskInvoker.Update();
                Assert.AreEqual(TaskStatus.Pause, pausedStatus);
            }
        }

        [TestMethod]
        public void TestPauseNotStartInvoker_Negative()
        {
            using (var taskInvoker = CreateFakeTaskInvoker(5, out _))
            {
                Assert.IsNotNull(taskInvoker);
                Assert.IsFalse(taskInvoker.Pause());
            }
        }

        [TestMethod]
        public void TestPauseTwiceInvoker_Negative()
        {
            using(var taskInvoker = CreateFakeTaskInvoker(5, out var mockTaskInvoker))
            {
                Assert.IsNotNull(taskInvoker);

                Assert.IsTrue(taskInvoker.Start());
                mockTaskInvoker.Protected().Verify("PrepareParameters", Times.Once());

                var startedStatus = taskInvoker.Update();
                Assert.AreEqual(TaskStatus.Running, startedStatus);

                Assert.IsTrue(taskInvoker.Pause());
                var pausedStatus = taskInvoker.Update();
                Assert.AreEqual(TaskStatus.Pause, pausedStatus);

                Assert.IsFalse(taskInvoker.Pause());
            }
        }

        private void WaitToComplete(ITaskInvoker taskInvoker, int maxWaitMs)
        {
            var haveTimeout = maxWaitMs >= 0;
            while (taskInvoker.Update() == TaskStatus.Running)
            {
                if (haveTimeout)
                {
                    Assert.IsTrue(maxWaitMs-- > 0);
                }
                Thread.Sleep(1);
            }
        }

        [TestMethod]
        public void TestCompleteInvoker_Positive()
        {
            using (var taskInvoker = CreateFakeTaskInvoker(5, out var mockTaskInvoker))
            {
                Assert.IsNotNull(taskInvoker);
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                Assert.IsTrue(taskInvoker.Start());
                mockTaskInvoker.Protected().Verify("PrepareParameters", Times.Once());

                var startedStatus = taskInvoker.Update();
                Assert.AreEqual(TaskStatus.Running, startedStatus);

                WaitToComplete(taskInvoker, -1);
                var completedStatus = taskInvoker.Update();

                stopwatch.Stop();
                var console = new TestConsole();
                console.WriteDiagnosticLine($"TestCompleteInvoker_Positive: {stopwatch.ElapsedMilliseconds} ms");

                Assert.AreEqual(TaskStatus.Completed, completedStatus);
                mockTaskInvoker.Protected().Verify("ProcessResult", Times.Once(), It.IsAny<bool>(), ItExpr.IsAny<bool>());
            }
        }

        [TestMethod]
        public void TestStopInvoker_Positive()
        {
            var taskInvoker = CreateFakeTaskInvoker(10, out var mockTaskInvoker);
            Assert.IsNotNull(taskInvoker);

            Assert.IsTrue(taskInvoker.Start());
            mockTaskInvoker.Protected().Verify("PrepareParameters", Times.Once());

            var startedStatus = taskInvoker.Update();
            Assert.AreEqual(TaskStatus.Running, startedStatus);

            Assert.IsTrue(taskInvoker.Stop());
            WaitToComplete(taskInvoker, 5);
            var stoppedStatus = taskInvoker.Update();

            Assert.AreEqual(TaskStatus.Stopped, stoppedStatus);
            mockTaskInvoker.Protected().Verify("ProcessResult", Times.Never(), It.IsAny<bool>(), ItExpr.IsAny<bool>());
        }

        [TestMethod]
        public void TestFailedInvoker_Positive()
        {
            using (var taskInvoker = CreateFakeTaskInvoker(-10, out var mockTaskInvoker))
            {
                Assert.IsNotNull(taskInvoker);

                Assert.IsTrue(taskInvoker.Start());
                mockTaskInvoker.Protected().Verify("PrepareParameters", Times.Once());

                WaitToComplete(taskInvoker, 1);
                var failedStatus = taskInvoker.Update();

                Assert.AreEqual(TaskStatus.Stopped, failedStatus);
                mockTaskInvoker.Protected().Verify("ProcessResult", Times.Never(), It.IsAny<bool>(), ItExpr.IsAny<bool>());
            }
        }
    }
}
