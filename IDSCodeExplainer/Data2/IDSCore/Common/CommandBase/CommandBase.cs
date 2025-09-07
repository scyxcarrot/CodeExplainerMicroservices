using IDS.Core.Common;
using IDS.Core.Enumerators;
using IDS.Core.ImplantDirector;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Core.V2.ExternalTools;
using IDSCore.Forms;
using Rhino;
using Rhino.Commands;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;


namespace IDS.Core.CommandBase
{
    public abstract class CommandBase<TImplantDirector> : Command where TImplantDirector : class, IImplantDirector
    {
        public static event EventHandler<CommandCallbackEventArgs<TImplantDirector>> CommandBeginExecuteSuccessfullyEvent;
        public static event EventHandler<CommandCallbackEventArgs<TImplantDirector>> CommandEndExecuteSuccessfullyEvent;

        private event EventHandler<bool> LoadIndicatorEvent;

        protected bool SubscribedLoadEvent { get; set; } = false;

        protected ICommandVisualizationComponent VisualizationComponent { get; set; }

        private LoadingIndicator _loadIndicatorWindow;

        protected bool IsUseBaseCustomUndoRedo { get; set; } = true;
        protected object BaseCustomUndoRedoTag { get; set; }

        protected Dictionary<string, string> TrackingParameters { get; set; }
        protected Dictionary<string, double> TrackingMetrics { get; set; }
        protected static readonly Mutex mutex = new Mutex();

        protected bool CloseAppAfterCommandEnds { get; set; } = false;

        protected void ShowLoadIndicator(bool isVisible)
        {
            if (isVisible)
            {
                // initialize a new CTS token
                IDSPluginHelper.LoadIndicatorCancellationTokenSource = new CancellationTokenSource();
                var cancellationToken = IDSPluginHelper.LoadIndicatorCancellationTokenSource;
                // Signal to coordinate between threads
                var windowInitializedSignal = new ManualResetEventSlim(false);
                // Loading Screen thread
                var newWindowThread = new Thread(() =>
                {
                    Thread.CurrentThread.Name = "LoadingIndicatorThread"; // added thread name for easy debug
                    try
                    {
                        // Create our context, and install it:
                        SynchronizationContext.SetSynchronizationContext(
                            new DispatcherSynchronizationContext(
                                Dispatcher.CurrentDispatcher));
                        _loadIndicatorWindow = new LoadingIndicator();
                        // When the window closes, shut down the dispatcher
                        _loadIndicatorWindow.Closed += (s, e) => Dispatcher.CurrentDispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
                        _loadIndicatorWindow.Show();
                        // Signal that initialization is complete
                        windowInitializedSignal.Set();
                        // Start the Dispatcher Processing
                        Dispatcher.Run();
                    }
                    catch (Exception ex)
                    {
                        IDSPluginHelper.WriteLine(LogCategory.Error, $"Error in UI thread: {ex.Message}");
                        // Always signal even on error to prevent deadlock
                        windowInitializedSignal.Set();
                    }
                });
                // Set the apartment state
                newWindowThread.SetApartmentState(ApartmentState.STA);
                newWindowThread.IsBackground = true;
                newWindowThread.Start();

                Task.Run((() =>
                {
                    try
                    {
                        // Wait for UI thread to be fully initialized, up to 5 seconds
                        windowInitializedSignal.Wait(5000);

                        // Monitor for cancellation
                        cancellationToken.Token.WaitHandle.WaitOne();

                        if (_loadIndicatorWindow?.Dispatcher != null)
                        {
                            _loadIndicatorWindow.Dispatcher.Invoke(() =>
                                {
                                    _loadIndicatorWindow.Close();
                                });
                        }
                    }
                    catch (Exception ex)
                    {
                        IDSPluginHelper.WriteLine(LogCategory.Error, $"Error in monitoring task: {ex.Message}");
                    }
                    finally
                    {
                        //unsubscribe the event here
                        try
                        {
                            LoadIndicatorEvent -= HandleLoadingIndicator;
                            _loadIndicatorWindow = null;
                        }
                        catch(Exception ex)
                        {
                            // Ignore error when force clean up
                        }
                        windowInitializedSignal?.Dispose();
                    }
                }));
            }
            else
            {
                // request for cancel and it will shut down both thread. 
                IDSPluginHelper.LoadIndicatorCancellationTokenSource?.Cancel();
            }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            TrackingParameters = new Dictionary<string, string>();
            TrackingMetrics = new Dictionary<string, double>();

            var idleTimeStopWatch = new IdleTimeStopWatch();
            Msai.SubscribeIdleTimeStopwatch(idleTimeStopWatch);

            var director = OnInitializeDirector(doc, mode);

            if (CheckCommandCanExecute(doc, mode, director))
            {
                VisualizationComponent?.OnCommandBeginVisualization(doc);

                AddLoadingEvent(); // if the command is subscribed 
                AddRunModeFlag(mode);
                AddPcMetrics();

                CommandBeginExecuteSuccessfullyEvent?.Invoke(this,
                    new CommandCallbackEventArgs<TImplantDirector>(director, EnglishName, mode));

                var hasInitialDirector = director != null;
                var countBefore = director?.IdsDocument?.GetUndoStackCount();
                director?.IdsDocument?.BeginTransaction();
                // trigger the event before command start
                LoadIndicatorEvent?.Invoke(this, true);

                var res = OnCommandExecute(doc, mode, director);

                if (hasInitialDirector)
                {
                    var countAfter = director.IdsDocument?.GetUndoStackCount();
                    var needEmptyCommand = countBefore == countAfter;
                    if (needEmptyCommand)
                    {
                        director.IdsDocument?.AddEmptyCommand();
                    }
                    director.IdsDocument?.Commit();
                }

                if (director == null) //It could have been set after execution (usually first time case loaded)
                {
                    director = OnInitializeDirector(doc, mode);
                }

                switch (res)
                {
                    case Result.Success:
                        DoCommonOperationsAfterCommandExecuted(idleTimeStopWatch, res, doc, director);
                        OnCommandExecuteSuccess(doc, director);
                        VisualizationComponent?.OnCommandSuccessVisualization(doc);
                        CommandEndExecuteSuccessfullyEvent?.Invoke(this,
                            new CommandCallbackEventArgs<TImplantDirector>(director, EnglishName, mode));
                        RhinoLayerUtilities.DeleteEmptyLayers(doc);
                        doc.Views.Redraw();

                        if (IsUseBaseCustomUndoRedo)
                        {
                            doc.AddCustomUndoEvent("BaseOnUndoRedo", BaseOnUndoRedo, BaseCustomUndoRedoTag ?? director);
                        }
                        return Result.Success;
                    case Result.Cancel:
                        DoCommonOperationsAfterCommandExecuted(idleTimeStopWatch, res, doc, director);
                        OnCommandExecuteCanceled(doc, director);
                        VisualizationComponent?.OnCommandCanceledVisualization(doc);
                        RhinoLayerUtilities.DeleteEmptyLayers(doc);
                        doc.Views.Redraw();
                        return Result.Cancel;
                    case Result.Nothing:
                        break;
                    case Result.Failure:
                        break;
                    case Result.UnknownCommand:
                        break;
                    case Result.CancelModelessDialog:
                        break;
                    case Result.ExitRhino:
                        break;
                    default:
                        IDSPluginHelper.WriteLine(Core.Enumerators.LogCategory.Error, "Returned result not implemented!");
                        doc.Views.Redraw();
                        RhinoLayerUtilities.DeleteEmptyLayers(doc);

                        DoCommonOperationsAfterCommandExecuted(idleTimeStopWatch, res, doc, director);
                        return Result.Failure;
                }

                //If Failed
                DoCommonOperationsAfterCommandExecuted(idleTimeStopWatch, res, doc, director);
                OnCommandExecuteFailed(doc, director);
                VisualizationComponent?.OnCommandFailureVisualization(doc);
                RhinoLayerUtilities.DeleteEmptyLayers(doc);
                doc.Views.Redraw();
                return Result.Failure;
            }

            //If can't execute
            OnCommandCannotExecute(doc, director);
            RhinoLayerUtilities.DeleteEmptyLayers(doc);
            doc.Views.Redraw();

            DoCommonOperationsAfterCommandExecuted(idleTimeStopWatch, Result.Failure, doc, director);
            return Result.Failure;
        }

        private void DoCommonOperationsAfterCommandExecuted(IdleTimeStopWatch idleTimeStopWatch, Result res, RhinoDoc doc, TImplantDirector director)
        {
            OnCommandJustExecuted(doc, director);
            AddCommandTimeMetric(idleTimeStopWatch);
            AddCommandResultParameter(res);
            AddCaseInfo(doc, director);
            LoadIndicatorEvent?.Invoke(this, false);
            TrackingParameters.Add("Phase", director.CurrentDesignPhaseName);
            TrackingParameters.Add("IsForUserTesting", director.IsForUserTesting ? "Yes" : "No");
            TrackingParameters.Add("DisplayMode", doc.Views.ActiveView?.ActiveViewport.DisplayMode.EnglishName);
            Msai.TrackOpsEvent(EnglishName, director.PluginInfoModel.ProductName, TrackingParameters, TrackingMetrics);

            if (CloseAppAfterCommandEnds)
            {
                Msai.Terminate(director.PluginInfoModel, director.FileName, director.version, director.draft);
                RhinoApp.Exit();
            }
        }

        //Command Execution Events
        public virtual bool CheckCommandCanExecute(RhinoDoc doc, RunMode mode, TImplantDirector director)
        {
            if (!IDSPluginHelper.CheckIfCommandIsAllowed(this) || director == null)
            {
                return false;
            }

            return true;
        }

        //Command Execution Events
        public virtual TImplantDirector OnInitializeDirector(RhinoDoc doc, RunMode mode)
        {
            return IDSPluginHelper.GetDirector<TImplantDirector>(doc.DocumentId);
        }

        private void BaseOnUndoRedo(object sender, CustomUndoEventArgs e)
        {
            e.Document.AddCustomUndoEvent("BaseOnUndoRedo", BaseOnUndoRedo, e.Tag);

            UndoRedo += OnCommandUndoRedo;

            IImplantDirector director;
            if (e.Tag is TImplantDirector implantDirector)
            {
                director = implantDirector;
            }
            else
            {
                var document = e.Document;
                director = IDSPluginHelper.GetDirector<IImplantDirector>((int)document.RuntimeSerialNumber);
            }

            if (e.CreatedByRedo)
            {
                director?.IdsDocument?.Redo();
                OnCommandBaseCustomRedo(e.Document, e.Tag);
            }
            else //Undo
            {
                director?.IdsDocument?.Undo();
                OnCommandBaseCustomUndo(e.Document, e.Tag);
            }
        }

        private void OnCommandUndoRedo(object sender, UndoRedoEventArgs e)
        {
            UndoRedo -= OnCommandUndoRedo;
            RhinoLayerUtilities.DeleteEmptyLayers(RhinoDoc.ActiveDoc);
        }

        public abstract Result OnCommandExecute(RhinoDoc doc, RunMode mode, TImplantDirector director);
        public virtual void OnCommandCannotExecute(RhinoDoc doc, TImplantDirector director) { }
        public virtual void OnCommandJustExecuted(RhinoDoc doc, TImplantDirector director) { }
        public virtual void OnCommandExecuteSuccess(RhinoDoc doc, TImplantDirector director) { }
        public virtual void OnCommandExecuteFailed(RhinoDoc doc, TImplantDirector director) { }
        public virtual void OnCommandExecuteCanceled(RhinoDoc doc, TImplantDirector director) { }
        public virtual void OnCommandBaseCustomUndo(RhinoDoc doc, object tag) { }
        public virtual void OnCommandBaseCustomRedo(RhinoDoc doc, object tag) { }

        protected virtual void HandleLoadingIndicator(object sender, bool isVisible)
        {
            //simple on/off is provided
            ShowLoadIndicator(isVisible);
        }

        private void AddCommandTimeMetric(IdleTimeStopWatch idleTimeStopWatch)
        {
            Msai.UnsubscribeIdleTimeStopwatch(idleTimeStopWatch);
            var elapsedInSeconds = idleTimeStopWatch.TotalTime * 0.001;
            var effectiveElapsedInSeconds = idleTimeStopWatch.EffectiveTimeMs * 0.001;
            var idleElapsedInSeconds = idleTimeStopWatch.IdleTimeMs * 0.001;
            var editingElapsedInSeconds = idleTimeStopWatch.EditingTimeMs * 0.001;


            TrackingMetrics.Add("CommandTime", elapsedInSeconds);
            TrackingMetrics.Add("CommandIdleTime", idleElapsedInSeconds);
            TrackingMetrics.Add("CommandEffectiveTime", effectiveElapsedInSeconds);
            TrackingMetrics.Add("CommandEditingTime", editingElapsedInSeconds);
        }

        private void AddCommandResultParameter(Result res)
        {
            TrackingParameters.Add("Result", res.ToString());
        }

        protected virtual void AddCaseInfo(RhinoDoc doc, TImplantDirector director)
        {
            TrackingParameters.Add(Msai.DocumentNameKey, director.FileName);
            TrackingParameters.Add("Case Version", director.version.ToString());
            TrackingParameters.Add("Case Draft", director.draft.ToString());
        }

        private void AddLoadingEvent()
        {
            if (SubscribedLoadEvent)
            {
                // initialize the UI component for cold start
                _loadIndicatorWindow = new LoadingIndicator();
                _loadIndicatorWindow = null;

                if (LoadIndicatorEvent != null)
                {
                    // to ensure only have 1 subscriber
                    LoadIndicatorEvent -= HandleLoadingIndicator;
                }
                LoadIndicatorEvent += HandleLoadingIndicator;
            }
        }

        private void AddRunModeFlag(RunMode mode)
        {
            IDSPluginHelper.ScriptMode = (mode == RunMode.Scripted);
        }

        private void AddPcMetrics()
        {
            var computerInfo = new Microsoft.VisualBasic.Devices.ComputerInfo();
            var avail = computerInfo.AvailablePhysicalMemory;
            var tot = computerInfo.TotalPhysicalMemory;
            var utilization = (((double)tot - (double)avail) / (double)tot) * 100;

            TrackingMetrics.Add("Available Memory (MB)", MathUtilities.ConvertBytesToMegabytes(computerInfo.AvailablePhysicalMemory));
            TrackingMetrics.Add("System Memory Utilization (Percentage)", utilization);
        }

        protected void AddTrackingParameterSafely(string key, string value)
        {
            mutex.WaitOne();
            if (TrackingParameters.ContainsKey(key))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, $"Duplicated key found for MSAI Tracking for key {key}. You can still proceed with your work but kindly report this to development team. Thank you!");
                mutex.ReleaseMutex();
                return;
            }

            TrackingParameters.Add(key, value);
            mutex.ReleaseMutex();
        }

        protected void UpdateTrackingInfo(MsaiTrackingInfo trackingInfo)
        {
            foreach (var trackingParameter in trackingInfo.TrackingParameters)
            {
                AddTrackingParameterSafely(trackingParameter.Key, trackingParameter.Value);
            }

            foreach (var trackingMetric in trackingInfo.TrackingMetrics)
            {
                TrackingMetrics.Add(trackingMetric.Key, trackingMetric.Value);
            }
        }
    }
}
