using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace IDS.CMF.V2.ScrewQc
{
    public static class ScrewQcUtilitiesV2
    {
        public static IEnumerable<T> SortScrewInfoRecords<T>(IEnumerable<T> records, bool guideScrewFirst = true)
            where T : ScrewInfoRecord
        {
            return records.OrderBy(x => x.IsGuideFixationScrew == guideScrewFirst ? x.NCase * 1000 + x.Index :
                x.NCase * 20000 + x.Index);
        }

        public static Dictionary<string, long> MergedTimeTracker(Dictionary<Guid, Dictionary<string, long>> totalTimeTracker)
        {
            Dictionary<string, long> mergeTimeTracker = null;

            foreach (var timeTrackerValue in totalTimeTracker.Select(timeTracker => timeTracker.Value))
            {
                if (mergeTimeTracker == null)
                {
                    mergeTimeTracker = timeTrackerValue;
                }
                else
                {
                    foreach (var map in timeTrackerValue)
                    {
                        mergeTimeTracker[map.Key] = map.Value;
                    }
                }
            }

            return mergeTimeTracker;
        }

        public static string FormatScrewAnatomicalObstacleResult(double distanceToAnatomicalObstacles)
        {
            return !double.IsNaN(distanceToAnatomicalObstacles) ? string.Format(CultureInfo.InvariantCulture, "{0:0.##}", distanceToAnatomicalObstacles) : "N/A";
        }

        public static QcDocCellColor DistToTableDataColor(double distanceToAnatomicalObstacles)
        {
            var distanceRounded = Math.Round(distanceToAnatomicalObstacles, 2);
            QcDocCellColor cellColor;

            if (distanceRounded >= 1)
            {
                cellColor = QcDocCellColor.Green;
            }
            else if (distanceRounded >= 0.5)
            {
                cellColor = QcDocCellColor.Yellow;
            }
            else if (distanceRounded > 0)
            {
                cellColor = QcDocCellColor.Orange;
            }
            else if (double.IsNaN(distanceRounded))
            {
                cellColor = QcDocCellColor.Green;
            }
            else
            {
                cellColor = QcDocCellColor.Red;
            }

            return cellColor;
        }
    }
}
