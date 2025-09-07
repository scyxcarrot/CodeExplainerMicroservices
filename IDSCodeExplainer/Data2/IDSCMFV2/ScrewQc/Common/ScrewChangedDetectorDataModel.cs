using System.Collections.Generic;

namespace IDS.CMF.V2.ScrewQc
{
    public class ScrewChangedDetectorDataModel
    {
        public List<ScrewInfoRecord> AddedScrewsRecords;
        public List<ScrewInfoRecord> RemovedScrewsRecords;
        public List<ScrewInfoRecord> ChangedScrewsRecords;
        public List<ScrewInfoRecord> UnchangedScrewsRecords;

        public ScrewChangedDetectorDataModel()
        {
            AddedScrewsRecords = new List<ScrewInfoRecord>();
            RemovedScrewsRecords = new List<ScrewInfoRecord>();
            ChangedScrewsRecords = new List<ScrewInfoRecord>();
            UnchangedScrewsRecords = new List<ScrewInfoRecord>();
        }
    }
}
