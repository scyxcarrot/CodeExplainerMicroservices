using IDS.CMF.V2.ScrewQc;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Commands;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;

namespace IDS.CMF.ScrewQc
{
    // TODO: Merge implant and guide bubble conduit proxy
    public class CMFImplantScrewQcBubbleConduitProxy
    {
        private ScrewQcBubbleManager _screwQcBubblesManager;
        private ScrewQcLiveUpdateHandler _screwQcLiveUpdateHandler;
        private PreImplantScrewQcInput _preImplantScrewQcInputCache;
        private DisplayAllImplantScrewOsteotomiesDistances _measurementsDisplay;
        private Dictionary<string, long> _checkAllTimeTracker;

        private static CMFImplantScrewQcBubbleConduitProxy _instance;

        public static CMFImplantScrewQcBubbleConduitProxy Instance =>
            _instance ?? (_instance = new CMFImplantScrewQcBubbleConduitProxy());

        public bool IsVisible => _screwQcBubblesManager?.IsShow() ?? false;

        public void SetUp(CMFImplantDirector director)
        {
            if (IsVisible)
            {
                return;
            }

            var screwQcManager = ImplantScrewQcUtilities.CreateScrewQcManager(director, ref _preImplantScrewQcInputCache);
            Dictionary<Guid, Dictionary<string, long>> totalTimeTracker;

            if (director.ImplantScrewQcLiveUpdateHandler == null)
            {
                var screwInfoTracker = new ScrewInfoRecordTracker(false);
                director.ImplantScrewQcLiveUpdateHandler = new ScrewQcLiveUpdateHandler(screwInfoTracker, director, screwQcManager, out totalTimeTracker);
            }
            else
            {
                director.ImplantScrewQcLiveUpdateHandler.Update(director, screwQcManager, out totalTimeTracker);
            }

            _checkAllTimeTracker = ScrewQcUtilitiesV2.MergedTimeTracker(totalTimeTracker);
            _screwQcLiveUpdateHandler = director.ImplantScrewQcLiveUpdateHandler;

            _screwQcBubblesManager = ImplantScrewQcUtilities.CreateScrewQcBubbleManager(director, out _measurementsDisplay);

            _screwQcBubblesManager.UpdateScrewBubbles(
                ImplantScrewQcUtilities.CreateScrewQcBubble(director, _screwQcLiveUpdateHandler.GetTrackScrewResults()));

            _screwQcBubblesManager.Show();
            Command.EndCommand += Command_EndCommand;
        }

        public ImmutableDictionary<string, long> GetCheckAllTimeTracker()
        {
            if (_checkAllTimeTracker == null)
            {
                return new Dictionary<string, long>().ToImmutableDictionary();
            }
            return _checkAllTimeTracker.ToImmutableDictionary();
        }

        public void TurnOff()
        {
            if (!IsVisible)
            {
                return;
            }

            Command.EndCommand -= Command_EndCommand;
            _screwQcBubblesManager.Clear();
            _screwQcBubblesManager = null;
            _screwQcLiveUpdateHandler = null;
            _preImplantScrewQcInputCache = null;
            _measurementsDisplay = null;
            _checkAllTimeTracker = null;
        }

        public virtual Result ToggleOnOff(CMFImplantDirector director)
        {
            if (IsVisible)
            {
                TurnOff();
                return Result.Success;
            }

            if (!ImplantScrewQcUtilities.PreScrewQcCheck(director))
            {
                return Result.Failure;
            }
#if (INTERNAL)
            var timer = new Stopwatch();
            timer.Start();
#endif
            try
            {
                SetUp(director);
            }
            catch (Exception e)
            {
                HandleOnError(director, e.Message);
                return Result.Failure;
            }
#if (INTERNAL)
            timer.Stop();
            IDSPluginHelper.WriteLine(LogCategory.Default, $"Implant Screw QC: { (timer.ElapsedMilliseconds * 0.001).ToString(CultureInfo.InvariantCulture) } seconds");
#endif
            return Result.Success;
        }

        private void Command_EndCommand(object sender, CommandEventArgs e)
        {
            if (e.CommandResult == Result.Success)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            var director =
                IDSPluginHelper.GetDirector<CMFImplantDirector>((int) RhinoDoc.ActiveDoc.RuntimeSerialNumber);

            try
            {
                var screwQcManager =
                    ImplantScrewQcUtilities.CreateScrewQcManager(director, ref _preImplantScrewQcInputCache);

                _screwQcLiveUpdateHandler.Update(director, screwQcManager, out _);
                var screwQcResults = _screwQcLiveUpdateHandler.GetTrackScrewResults();
                _measurementsDisplay.Update(screwQcResults.Values);
                _screwQcBubblesManager.UpdateScrewBubbles(
                    ImplantScrewQcUtilities.CreateScrewQcBubble(director, screwQcResults));
            }
            catch (Exception e)
            {
                HandleOnError(director, e.Message);
                TurnOff();
            }
            
            director.Document.Views.ActiveView.Redraw();
        }

        private void HandleOnError(CMFImplantDirector director, string message)
        {
            IDSPluginHelper.WriteLine(LogCategory.Error, "Error when running QC check for screws");
            IDSPluginHelper.WriteLine(LogCategory.Error, message);
            director.ImplantScrewQcLiveUpdateHandler = null;
        }
    }
}
