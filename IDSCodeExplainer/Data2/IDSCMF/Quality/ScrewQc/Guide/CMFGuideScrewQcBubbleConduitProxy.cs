using IDS.CMF.V2.ScrewQc;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Commands;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;

namespace IDS.CMF.ScrewQc
{
    public class CMFGuideScrewQcBubbleConduitProxy
    {
        private ScrewQcBubbleManager _screwQcBubblesManager;
        private ScrewQcLiveUpdateHandler _screwQcLiveUpdateHandler;
        private PreGuideScrewQcInput _preGuideScrewQcInputCache;
        private Dictionary<string, long> _checkAllTimeTracker;

        private static CMFGuideScrewQcBubbleConduitProxy _instance;

        public static CMFGuideScrewQcBubbleConduitProxy Instance =>
            _instance ?? (_instance = new CMFGuideScrewQcBubbleConduitProxy());

        public bool IsVisible => _screwQcBubblesManager?.IsShow() ?? false;

        public void SetUp(CMFImplantDirector director)
        {
            if (IsVisible)
            {
                return;
            }

            Command.EndCommand += Command_EndCommand;

            var screwQcManager =
                GuideScrewQcUtilities.CreateScrewQcManager(director, false, ref _preGuideScrewQcInputCache);
            var screwInfoTracker = new ScrewInfoRecordTracker(true);

            _screwQcLiveUpdateHandler = new ScrewQcLiveUpdateHandler(screwInfoTracker, director, screwQcManager, out var totalTimeTracker);
            _checkAllTimeTracker = ScrewQcUtilitiesV2.MergedTimeTracker(totalTimeTracker);

            _screwQcBubblesManager = GuideScrewQcUtilities.CreateScrewQcBubbleManager(director);

            _screwQcBubblesManager.UpdateScrewBubbles(
                GuideScrewQcUtilities.CreateScrewQcBubble(director, _screwQcLiveUpdateHandler.GetTrackScrewResults()));

            _screwQcBubblesManager.Show();
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
            _preGuideScrewQcInputCache = null;
            _checkAllTimeTracker = null;
        }

        public virtual Result ToggleOnOff(CMFImplantDirector director)
        {
            if (IsVisible)
            {
                TurnOff();
                return Result.Success;
            }

            if (!GuideScrewQcUtilities.PreScrewQcCheck(director))
            {
                return Result.Failure;
            }
#if (INTERNAL)
            var timer = new Stopwatch();
            timer.Start();
#endif
            SetUp(director);
#if (INTERNAL)
            timer.Stop();
            IDSPluginHelper.WriteLine(LogCategory.Default, $"Guide Screw QC: { (timer.ElapsedMilliseconds * 0.001).ToString(CultureInfo.InvariantCulture) } seconds");
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
            var screwQcManager =
                GuideScrewQcUtilities.CreateScrewQcManager(director, true, ref _preGuideScrewQcInputCache);

            _screwQcLiveUpdateHandler.Update(director, screwQcManager, out _);

            _screwQcBubblesManager.UpdateScrewBubbles(
                GuideScrewQcUtilities.CreateScrewQcBubble(director, _screwQcLiveUpdateHandler.GetTrackScrewResults()));

            director.Document.Views.ActiveView.Redraw();
        }
    }
}