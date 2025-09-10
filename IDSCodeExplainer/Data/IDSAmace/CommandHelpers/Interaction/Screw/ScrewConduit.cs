using IDS.Amace;
using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Quality;
using IDS.Core.DataTypes;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Quality;
using Rhino.Display;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

namespace IDS.Visualization
{
    /// <summary>
    /// The screw quality check conduit
    /// </summary>
    /// <seealso cref="Rhino.Display.DisplayConduit" />
    public class ScrewConduit : DisplayConduit
    {
        private static readonly Color NotOkColor = Color.Red;
        private static readonly Color OkColor = Color.Green;

        /// <summary>
        /// The director
        /// </summary>
        private readonly ImplantDirector _director = null;

        /// <summary>
        /// The screw conduit mode
        /// </summary>
        public ScrewConduitMode ScrewConduitMode;

        /// <summary>
        /// The bump intersections other screws
        /// </summary>
        private Dictionary<int, List<int>> _screwHoleBumpIntersections = new Dictionary<int, List<int>>();

        /// <summary>
        /// The bump intersections in the cup zone
        /// </summary>
        private List<int> _cupZoneBumpsDestroyed = new List<int>();
        
        /// <summary>
        /// The screw overlaps
        /// </summary>
        private Dictionary<int, List<int>> _screwIntersections = new Dictionary<int, List<int>>();

        /// <summary>
        /// The screw vicinity issues
        /// </summary>
        private Dictionary<int, List<int>> _screwVicinityIssues = new Dictionary<int, List<int>>();

        /// <summary>
        /// The insert trajectories intersections
        /// </summary>
        private List<int> _insertTrajectoryIntersections = new List<int>();

        /// <summary>
        /// The shaft trajectory intersections
        /// </summary>
        private List<int> _shaftTrajectoryIntersections = new List<int>();

        /// <summary>
        /// Include slow checks
        /// </summary>
        private bool _includeSlowChecks;

        /// <summary>
        /// The screw's guide hole intersection issues
        /// </summary>
        private Dictionary<int, List<int>> _guideHoleIntersections = new Dictionary<int, List<int>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ScrewConduit" /> class.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <param name="screwConduitMode">The screw conduit mode.</param>
        public ScrewConduit(ImplantDirector director, ScrewConduitMode screwConduitMode)
        {
            this._director = director;

            // Precalculations
            UpdateConduit(screwConduitMode);
        }

        public ScrewConduit(ImplantDirector director, ScrewConduitMode screwConduitMode, DesignPhase designPhase)
        {
            this._director = director;

            // Precalculations
            UpdateConduit(screwConduitMode, designPhase);
        }

        /// <summary>
        /// Updates the conduit.
        /// </summary>
        /// <param name="screwConduitMode">The screw conduit mode.</param>
        public void UpdateConduit(ScrewConduitMode screwConduitMode)
        {
            UpdateConduit(screwConduitMode, _director.CurrentDesignPhase);
        }

        private void UpdateConduit(ScrewConduitMode screwConduitMode, DesignPhase designPhase)
        {
            // Update the show warnings
            ScrewConduitMode = screwConduitMode;

            // Clean all qc check precalculations
            _screwIntersections = new Dictionary<int, List<int>>();
            _screwVicinityIssues = new Dictionary<int, List<int>>();
            _screwHoleBumpIntersections = new Dictionary<int, List<int>>();
            _cupZoneBumpsDestroyed = new List<int>();
            _insertTrajectoryIntersections = new List<int>();
            _shaftTrajectoryIntersections = new List<int>();
            _guideHoleIntersections = new Dictionary<int, List<int>>();

            // Calculate screw overlap checks when screw qc checks are wanted
            if (ScrewConduitMode == ScrewConduitMode.WarningColors || ScrewConduitMode == ScrewConduitMode.WarningTextAndColor)
            {
#if DEBUG
                var timer = new Stopwatch();
                timer.Start();
#endif

                // Get all screws
                var screwManager = new ScrewManager(_director.Document);
                var screws = screwManager.GetAllScrews().ToList();

                var screwAnalysis = new AmaceScrewAnalysis();
                _screwIntersections = screwAnalysis.PerformScrewIntersectionCheck(screws,0);
                _screwVicinityIssues = screwAnalysis.PerformScrewIntersectionCheck(screws, 1);
                _guideHoleIntersections = screwAnalysis.PerformGuideHoleBooleanIntersectionCheck(screws, _director.DrillBitRadius);

#if DEBUG
                timer.Stop();
                IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "Update Screw Conduit in {0:mm\\:ss\\.fffff}", timer.Elapsed);
#endif
            }

            // Calculate the complex checks only when in implant qc phase
            if (designPhase == DesignPhase.ImplantQC && (ScrewConduitMode == ScrewConduitMode.WarningColors || ScrewConduitMode == ScrewConduitMode.WarningTextAndColor))
            {
                // Include the slow checks
                _includeSlowChecks = true;

                // Calculate all complex screw analyses
                var objManager = new AmaceObjectManager(_director);
                var plateBumps = objManager.GetBuildingBlock(IBB.PlateBumps).Geometry as Mesh;

                var screwManager = new ScrewManager(_director.Document);
                var screws = screwManager.GetAllScrews().ToList();

                var aMaceScrewAnalysis = new AmaceScrewAnalysis();
                aMaceScrewAnalysis.PerformSlowScrewChecks(screws, plateBumps, _director.cup, 
                    out _screwHoleBumpIntersections, out _cupZoneBumpsDestroyed, out _insertTrajectoryIntersections, out _shaftTrajectoryIntersections);

            }
        }

        /// <summary>
        /// Called after all non-highlighted objects have been drawn and PostDrawObjects has been called.
        /// Depth writing and testing are turned OFF. If you want to draw with depth writing/testing,
        /// see PostDrawObjects.
        /// <para>The default implementation does nothing.</para>
        /// </summary>
        /// <param name="e">The event argument contains the current viewport and display state.</param>
        protected override void DrawForeground(DrawEventArgs e)
        {
            // do not refresh while panning, rotating,...
            if (e.Display.IsDynamicDisplay)
            {
                return;
            }

            base.DrawForeground(e);
            var screwManager = new ScrewManager(_director.Document);
            var screws = screwManager.GetAllScrews().ToList();

            foreach (var screw in screws)
            {
                // Location of the warning (1 mm above the screw head )
                var screwStringLocation = screw.HeadPoint - screw.Direction;
                // Initialize string with the screw number
                var screwString = screw.Index.ToString();
                // Initialize bubble color
                var screwBubbleColor = Color.DarkBlue;
                // Show warnings and colors if necessary
                if (ScrewConduitMode == ScrewConduitMode.WarningColors || ScrewConduitMode == ScrewConduitMode.WarningTextAndColor)
                {
                    screwBubbleColor = OkColor;

                    ReportScrewIntersectionIssues(screw, ref screwString, ref screwBubbleColor);
                    ReportScrewVicinityIssues(screw, ref screwString, ref screwBubbleColor);
                    ReportScrewLengthIssues(screw, ref screwString, ref screwBubbleColor);
                    ReportCupRimAngleIssues(screw, ref screwString, ref screwBubbleColor);
                    ReportBonePenetrationIssues(screw, ref screwString, ref screwBubbleColor);
                    ReportScrewInCupZoneIssues(screw, ref screwString, ref screwBubbleColor);
                    ReportScrewGuideHoleIssues(screw, ref screwString, ref screwBubbleColor);

                    if (_includeSlowChecks)
                    {
                        ReportScrewHoleBumpIntersections(screw, ref screwString, ref screwBubbleColor);
                        ReportCupZoneBumpDestructions(screw, ref screwString, ref screwBubbleColor);
                        ReportInsertTrajectoryIssues(screw, ref screwString, ref screwBubbleColor);
                        ReportShaftTrajectoryIssues(screw, ref screwString, ref screwBubbleColor);
                    }
                }
                // Draw the dot
                e.Display.DrawDot(screwStringLocation, screwString, screwBubbleColor, Color.White);
            }
        }

        private void ReportShaftTrajectoryIssues(Screw screw, ref string screwString, ref Color screwBubbleColor)
        {
            if (!_shaftTrajectoryIntersections.Contains(screw.Index))
            {
                return;
            }
            SetNotOkColorIfModeRequiresIt(ref screwBubbleColor);

            if (ScrewConduitMode == ScrewConduitMode.WarningTextAndColor)
            {
                screwString += "\nShaft";
            }
        }

        private void ReportInsertTrajectoryIssues(Screw screw, ref string screwString, ref Color screwBubbleColor)
        {
            if (!_insertTrajectoryIntersections.Contains(screw.Index))
            {
                return;
            }
            SetNotOkColorIfModeRequiresIt(ref screwBubbleColor);

            if (ScrewConduitMode == ScrewConduitMode.WarningTextAndColor)
            {
                screwString += "\nInsert";
            }
        }

        private void SetNotOkColorIfModeRequiresIt(ref Color screwBubbleColor)
        {
            if (ScrewConduitMode == ScrewConduitMode.WarningColors ||
                ScrewConduitMode == ScrewConduitMode.WarningTextAndColor)
            {
                screwBubbleColor = NotOkColor;
            }
        }

        private void ReportCupZoneBumpDestructions(Screw screw, ref string screwString, ref Color screwBubbleColor)
        {
            if (!_cupZoneBumpsDestroyed.Contains(screw.Index))
            {
                return;
            }
            SetNotOkColorIfModeRequiresIt(ref screwBubbleColor);

            if (ScrewConduitMode == ScrewConduitMode.WarningTextAndColor)
            {
                screwString += "\nBump in Cup";
            }
        }

        private void ReportScrewHoleBumpIntersections(Screw screw, ref string screwString, ref Color screwBubbleColor)
        {
            ReportIssues(_screwHoleBumpIntersections, screw.Index, "Bump", ref screwString, ref screwBubbleColor);
        }

        private void ReportScrewInCupZoneIssues(Screw screw, ref string screwString, ref Color screwBubbleColor)
        {
            // ReSharper disable once SwitchStatementMissingSomeCases
            if (screw.CheckCupZone() == QualityCheckResult.NotOK)
            {
                SetNotOkColorIfModeRequiresIt(ref screwBubbleColor);

                if (ScrewConduitMode == ScrewConduitMode.WarningTextAndColor)
                {
                    screwString += "\nScrew in Cup";
                }
            }
            else if (screw.CheckCupZone() == QualityCheckResult.Error)
            {
                SetNotOkColorIfModeRequiresIt(ref screwBubbleColor);

                if (ScrewConduitMode == ScrewConduitMode.WarningTextAndColor)
                {
                    screwString += "\nScrew in Cup ERROR";
                }
            }
        }

        private void ReportBonePenetrationIssues(Screw screw, ref string screwString, ref Color screwBubbleColor)
        {
            // ReSharper disable once SwitchStatementMissingSomeCases
            if (screw.CheckBonePenetration() == QualityCheckResult.NotOK)
            {
                SetNotOkColorIfModeRequiresIt(ref screwBubbleColor);

                if (ScrewConduitMode == ScrewConduitMode.WarningTextAndColor)
                {
                    screwString += "\nBone";
                }
            }
            else if (screw.CheckBonePenetration() == QualityCheckResult.Error)
            {
                SetNotOkColorIfModeRequiresIt(ref screwBubbleColor);

                if (ScrewConduitMode == ScrewConduitMode.WarningTextAndColor)
                {
                    screwString += "\nBone ERROR";
                }
            }
        }

        private void ReportCupRimAngleIssues(Screw screw, ref string screwString, ref Color screwBubbleColor)
        {
            // ReSharper disable once SwitchStatementMissingSomeCases
            if (screw.CheckCupRimAngle() == QualityCheckResult.NotOK)
            {
                SetNotOkColorIfModeRequiresIt(ref screwBubbleColor);

                if (ScrewConduitMode == ScrewConduitMode.WarningTextAndColor)
                {
                    screwString += $"\nAngle {screw.CupRimAngleDegrees:F1}°";
                }
            }
            else if (screw.CheckCupRimAngle() == QualityCheckResult.Error)
            {
                SetNotOkColorIfModeRequiresIt(ref screwBubbleColor);

                if (ScrewConduitMode == ScrewConduitMode.WarningTextAndColor)
                {
                    screwString += "\nAngle ERROR";
                }
            }
        }

        private void ReportScrewLengthIssues(Screw screw, ref string screwString, ref Color screwBubbleColor)
        {
            var checkLength = screw.CheckScrewLength();
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (checkLength)
            {
                case QualityCheckResult.NotOK:
                    SetNotOkColorIfModeRequiresIt(ref screwBubbleColor);
                    if (ScrewConduitMode == ScrewConduitMode.WarningTextAndColor)
                    {
                        screwString += "\nLength<14mm";
                    }
                    break;
                case QualityCheckResult.Error:
                    SetNotOkColorIfModeRequiresIt(ref screwBubbleColor);
                    if (ScrewConduitMode == ScrewConduitMode.WarningTextAndColor)
                    {
                        screwString += "\nLength ERROR";
                    }
                    break;
            }
        }

        private void ReportScrewIntersectionIssues(Screw screw, ref string screwString, ref Color screwBubbleColor)
        {
            ReportIssues(_screwIntersections, screw.Index, "Screw", ref screwString, ref screwBubbleColor);
        }

        private void ReportScrewVicinityIssues(Screw screw, ref string screwString, ref Color screwBubbleColor)
        {
            if (!_screwIntersections.ContainsKey(screw.Index))
            {
                ReportIssues(_screwVicinityIssues, screw.Index, "Close", ref screwString, ref screwBubbleColor);
            }
        }

        private void ReportScrewGuideHoleIssues(Screw screw, ref string screwString, ref Color screwBubbleColor)
        {
            ReportIssues(_guideHoleIntersections, screw.Index, "Guide Hole", ref screwString, ref screwBubbleColor);
        }

        private void ReportIssues(Dictionary<int, List<int>> dictionary, int screwIndex, string issueName, ref string screwString, ref Color screwBubbleColor)
        {
            if (!dictionary.ContainsKey(screwIndex))
            {
                return;
            }
            SetNotOkColorIfModeRequiresIt(ref screwBubbleColor);
            if (ScrewConduitMode != ScrewConduitMode.WarningTextAndColor)
            {
                return;
            }
            screwString += $"\n{issueName} (";
            foreach (var i in dictionary[screwIndex])
            {
                screwString += $"{i:F0},";
            }
            screwString = screwString.Substring(0, screwString.Length - 1) + ")";
        }
    }
}