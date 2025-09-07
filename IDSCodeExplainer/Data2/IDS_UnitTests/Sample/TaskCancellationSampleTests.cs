using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IDS.Testing.Sample
{
#if (RUN_SAMPLE)
        [TestClass]
    public class TaskRunCancellationTokenUnitTests
    {
        [TestMethod]
        public void CancellationTokenPassedToSynchronousTaskRun_CancelsTaskWithTaskCanceledException()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();
            var task = Task.Run(() => { }, cts.Token);
            TaskCanceledException exception = null;
            try
            {
                task.Wait();
            }
            catch (AggregateException ex)
            {
                // TaskCanceledException derives from OperationCanceledException
                exception = ex.InnerException as TaskCanceledException;
            }

            Assert.IsTrue(task.IsCanceled);
            Assert.IsNotNull(exception);
            Assert.AreEqual(cts.Token, exception.CancellationToken);
        }

        [TestMethod]
        public void CancellationTokenPassedToAsynchronousTaskRun_CancelsTaskWithTaskCanceledException()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();
            var task = Task.Run(async () => { await Task.Yield(); }, cts.Token);
            TaskCanceledException exception = null;
            try
            {
                task.Wait();
            }
            catch (AggregateException ex)
            {
                // TaskCanceledException derives from OperationCanceledException
                exception = ex.InnerException as TaskCanceledException;
            }

            Assert.IsTrue(task.IsCanceled);
            Assert.IsNotNull(exception);
            Assert.AreEqual(cts.Token, exception.CancellationToken);
        }

        [TestMethod]
        public void CancellationTokenObservedBySynchronousDelegate_FaultsTaskWithOperationCanceledException()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();
            var task = Task.Run(() => { cts.Token.ThrowIfCancellationRequested(); });
            OperationCanceledException exception = null;
            try
            {
                task.Wait();
            }
            catch (AggregateException ex)
            {
                exception = ex.InnerException as OperationCanceledException;
            }

            Assert.IsTrue(task.IsFaulted);
            Assert.IsNotNull(exception);
            Assert.AreEqual(cts.Token, exception.CancellationToken);
        }

        [TestMethod]
        public void CancellationTokenObservedByAsynchronousDelegate_CancelsTaskWithTaskCanceledException()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();
            var task = Task.Run(async () =>
            {
                await Task.Yield();
                cts.Token.ThrowIfCancellationRequested();
            });
            OperationCanceledException exception = null;
            try
            {
                task.Wait();
            }
            catch (AggregateException ex)
            {
                // TaskCanceledException derives from OperationCanceledException
                exception = ex.InnerException as TaskCanceledException;
            }

            Assert.IsTrue(task.IsCanceled); // Note: canceled, not faulted!
            Assert.IsNotNull(exception);
            Assert.AreEqual(cts.Token, exception.CancellationToken);
        }

        [TestMethod]
        public void CancellationTokenObservedSynchronouslyAndPassed_CancelsTaskWithTaskCanceledException()
        {
            var cts = new CancellationTokenSource();
            var taskReady = new ManualResetEvent(false);
            var task = Task.Run(() =>
            {
                taskReady.Set();
                while (true)
                    cts.Token.ThrowIfCancellationRequested();
            }, cts.Token);
            taskReady.WaitOne();
            cts.Cancel();
            OperationCanceledException exception = null;
            try
            {
                task.Wait();
            }
            catch (AggregateException ex)
            {
                // TaskCanceledException derives from OperationCanceledException
                exception = ex.InnerException as TaskCanceledException;
            }

            Assert.IsTrue(task.IsCanceled);
            Assert.IsNotNull(exception);
            Assert.AreEqual(cts.Token, exception.CancellationToken);
        }

        [TestMethod]
        public void CancellationTokenObservedAsynchronouslyAndPassed_CancelsTaskWithTaskCanceledException()
        {
            var cts = new CancellationTokenSource();
            var taskReady = new ManualResetEvent(false);
            var task = Task.Run(async () =>
            {
                await Task.Yield();
                taskReady.Set();
                while (true)
                    cts.Token.ThrowIfCancellationRequested();
            }, cts.Token);
            taskReady.WaitOne();
            cts.Cancel();
            OperationCanceledException exception = null;
            try
            {
                task.Wait();
            }
            catch (AggregateException ex)
            {
                // TaskCanceledException derives from OperationCanceledException
                exception = ex.InnerException as TaskCanceledException;
            }

            Assert.IsTrue(task.IsCanceled);
            Assert.IsNotNull(exception);
            Assert.AreEqual(cts.Token, exception.CancellationToken);
        }

        [TestMethod]
        public void CancelAndWaitWithToken()
        {
            using(var cts = new CancellationTokenSource())
            using (var task = Task.Run(() => {
                       while (true)
                           cts.Token.ThrowIfCancellationRequested();
                   }, cts.Token))
            {
                cts.Cancel();
                try
                {
                    task.Wait(cts.Token);
                }
                catch (OperationCanceledException)
                {
                }

                Assert.IsTrue(task.IsCanceled);
            }
        }

        [TestMethod]
        public void CancelAndWaitWithoutToken()
        {
            using (var cts = new CancellationTokenSource())
            using (var task = Task.Run(() => {
                       while (true)
                           cts.Token.ThrowIfCancellationRequested();
                   }))
            {
                cts.Cancel();
                OperationCanceledException exception = null;
                try
                {
                    task.Wait();
                }
                catch (AggregateException ex)
                {
                    // TaskCanceledException derives from OperationCanceledException
                    exception = ex.InnerException as TaskCanceledException;
                }

                Assert.IsTrue(task.IsCanceled);
                Assert.IsNotNull(exception);
                Assert.AreEqual(cts.Token, exception.CancellationToken);
            }
        }
    }
#endif

}