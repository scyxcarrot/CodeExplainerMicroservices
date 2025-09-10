#if (INTERNAL)
using IDS.Core.V2.Tasks;
using IDS.Core.V2.Utilities;
using IDS.Interface.Tasks;
using IDS.Interface.Tools;
using IDS.PICMF.Forms.BackgroundProcess;
using Rhino;
using Rhino.Commands;
using System;
using System.Diagnostics;

namespace IDS.PICMF.NonProduction
{
    [System.Runtime.InteropServices.Guid("BE85DA23-E926-4A8F-A121-33AE78E409F0")]
    public sealed class CMF_TestBackgroundProcess : Command
    {
        public sealed class DummyTaskCommand: TaskCommand<int, TimeSpan>
        {
            public DummyTaskCommand(Guid id, ITaskActuator taskActuator, IConsole console) : 
                base(id, taskActuator, console)
            {
            }

            public override string Description => 
                $"Dummy Delay Task <{Id.ToString().Substring(0, 6)}>";

            public override TimeSpan Execute(int delaySeconds)
            {
                if (delaySeconds < 0)
                {
                    throw new ArgumentOutOfRangeException($"delaySeconds({delaySeconds}) shouldn't less than 0");
                }

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                while (stopwatch.ElapsedMilliseconds < delaySeconds * 1000)
                {
                    NonBlockingDelay(500);
                    var milliseconds = stopwatch.ElapsedMilliseconds;
                    console.WriteDiagnosticLine($"Delay {milliseconds / 1000} sec {milliseconds % 1000} ms");
                    var progress = Convert.ToDouble(milliseconds) /
                                   Convert.ToDouble(delaySeconds * 1000);
                    progress *= 100;
                    SetCheckPoint(progress);
                }
                stopwatch.Stop();
                return stopwatch.Elapsed;
            }
        }

        public class DummyTaskInvoker: TaskInvoker<int, TimeSpan, DummyTaskCommand>
        {
            private readonly int _delaySeconds;
            public DummyTaskInvoker(Guid id, IConsole console, int delaySeconds) : base(id, console)
            {
                _delaySeconds = delaySeconds;
            }

            protected override int PrepareParameters()
            {
                console.WriteLine("Started...");
                return _delaySeconds;
            }

            protected override void ProcessResult(TimeSpan result)
            {
                console.WriteLine($"Total Wait Time: {StringUtilitiesV2.ElapsedTimeSpanToString(result)}");
                RhinoApp.WriteLine($"Total Wait Time: {StringUtilitiesV2.ElapsedTimeSpanToString(result)}");
            }
        }

        public CMF_TestBackgroundProcess()
        {
            Instance = this;
        }

        public static CMF_TestBackgroundProcess Instance { get; private set; }

        public override string EnglishName => "CMF_TestBackgroundProcess";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            BackgroundProcessPanelWpfHost.OpenPanel();
            var panelViewModel = BackgroundProcessPanelWpfHost.GetViewModel();
            var logPanelViewModel = new LogPanelViewModel();
            var logConsole = new BackGroundProcessConsole(logPanelViewModel);

            //var dummy = new DummyTaskInvoker(Guid.NewGuid(), logConsole, 10);
            var dummy = new DummyTaskInvoker(Guid.NewGuid(), logConsole, -1);

            var viewModel = new BackgroundProcessViewModel(dummy, logPanelViewModel);

            panelViewModel.AddActiveTask(viewModel);
            return Result.Success;
        }
    }
}
#endif