using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.V2.ScrewQc
{
    public static class ScrewChangedDetector
    {
        public static ScrewChangedDetectorDataModel CompareScrews(IEnumerable<ScrewInfoRecord> currentScrewRecords,
            IEnumerable<ScrewInfoRecord> previousScrewRecords)
        {
            var changedDetectorDataModel = new ScrewChangedDetectorDataModel();

            var currentScrewRecordsCopies = currentScrewRecords.ToList();
            var previousScrewRecordsCopies = previousScrewRecords.ToList();

            foreach (var currentScrew in currentScrewRecordsCopies)
            {
                var previousScrew = previousScrewRecordsCopies.FirstOrDefault(ps => ps.Id == currentScrew.Id);
                if (previousScrew == null)
                {
                    changedDetectorDataModel.AddedScrewsRecords.Add(currentScrew);
                    continue;
                }
                
                if (!currentScrew.IsSameScrewProperties(previousScrew))
                {
                    changedDetectorDataModel.ChangedScrewsRecords.Add(currentScrew);
                }
                else
                {
                    changedDetectorDataModel.UnchangedScrewsRecords.Add(currentScrew);
                }

                previousScrewRecordsCopies.Remove(previousScrew);
            }

            changedDetectorDataModel.RemovedScrewsRecords.AddRange(previousScrewRecordsCopies);
            return changedDetectorDataModel;
        }
    }
}
