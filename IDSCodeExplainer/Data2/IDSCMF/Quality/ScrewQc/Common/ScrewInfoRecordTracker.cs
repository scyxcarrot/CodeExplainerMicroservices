using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.V2.DataModel;
using IDS.CMF.V2.ScrewQc;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.ScrewQc
{
    /// <summary>
    /// Class <c>ScrewChangedTracker</c> models a point in a two-dimensional plane.
    /// </summary>
    public class ScrewInfoRecordTracker
    {
        private readonly bool _forGuideScrew;
        private readonly List<ScrewInfoRecord> _previousScrewInfoRecords;

        public ScrewInfoRecordTracker(bool forGuideScrew)
        {
            _forGuideScrew = forGuideScrew;
            _previousScrewInfoRecords = new List<ScrewInfoRecord>();
        }

        public ScrewInfoRecordTracker(IEnumerable<ImplantScrewSerializableDataModel> screwSerializableDataModels)
        {
            _forGuideScrew = false;
            _previousScrewInfoRecords = screwSerializableDataModels.Select(s => 
                (ScrewInfoRecord)new ImplantScrewInfoRecord(s)).ToList();
        }

        public IEnumerable<ScrewInfoRecord> GetHistoricalRecords()
        {
            return _previousScrewInfoRecords.Select(s => s.CastedClone()).ToList();
        }

        public IEnumerable<ScrewInfoRecord> GetLatestRecords(CMFImplantDirector director)
        {
            var screws = GetScrews(director).ToArray();
           return GetScrewRecords(screws).ToList();
        }

        public void UpdateRecords(CMFImplantDirector director)
        {
            _previousScrewInfoRecords.Clear();
            _previousScrewInfoRecords.AddRange(GetLatestRecords(director));
        }

        private IEnumerable<Screw> GetScrews(CMFImplantDirector director)
        {
            var screwManager = new ScrewManager(director);
            return screwManager.GetAllScrews(_forGuideScrew);
        }

        private IEnumerable<ScrewInfoRecord> GetScrewRecords(IEnumerable<Screw> screws)
        {
            return screws.Select(s => (_forGuideScrew ?
                (ScrewInfoRecord)new GuideScrewInfoRecord(s) :
                (ScrewInfoRecord)new ImplantScrewInfoRecord(s)));
        }

        public IEnumerable<CommonScrewSerializableDataModel> GetLatestCommonScrewSerializableDataModel()
        {
            if (_forGuideScrew)
            {
                return _previousScrewInfoRecords.Select(s =>
                    ((GuideScrewInfoRecord)s).GetGuideScrewSerializableDataModel());
            }
            return _previousScrewInfoRecords.Select(s =>
                ((ImplantScrewInfoRecord)s).GetImplantScrewSerializableDataModel());
        }
    }
}
