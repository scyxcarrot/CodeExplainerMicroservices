using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.V2.DataModel;
using IDS.CMF.V2.ScrewQc;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace IDS.CMF.ScrewQc
{
    public class ScrewQcLiveUpdateHandler
    {
        private readonly ScrewInfoRecordTracker _tracker;

        private readonly Dictionary<Guid, ImmutableList<IScrewQcResult>> _screwQcResultDatabase;

        public ScrewQcLiveUpdateHandler(ScrewInfoRecordTracker tracker, CMFImplantDirector director,
            ScrewQcCheckerManager screwQcCheckerManager, out Dictionary<Guid, Dictionary<string, long>> totalTimeTracker):
            this(tracker, new Dictionary<Guid, ImmutableList<IScrewQcResult>>())
        {
            _tracker.UpdateRecords(director);
            var allScrewRecords = _tracker.GetLatestRecords(director);

            var screwInfoRecordHelper = new ScrewInfoRecordHelper(director);
            var screws = allScrewRecords.Select(r => screwInfoRecordHelper.GetScrewByRecord(r));

            var results = screwQcCheckerManager.CheckAll(screws, out totalTimeTracker);

            foreach (var result in results)
            {
                _screwQcResultDatabase.Add(result.Key, result.Value);
            }
        }

        public ScrewQcLiveUpdateHandler(ScrewInfoRecordTracker tracker, 
            Dictionary<Guid, ImmutableList<IScrewQcResult>> screwQcResultDatabase)
        {
            _tracker = tracker;
            _screwQcResultDatabase = screwQcResultDatabase;
        }

        private void RemoveScrewQcResultsInExistingDatabase(Guid removedScrewId)
        {
            if (_screwQcResultDatabase.ContainsKey(removedScrewId))
            {
                var removedResult = _screwQcResultDatabase[removedScrewId];
                _screwQcResultDatabase.Remove(removedScrewId);
                
                var removedMutableResults = removedResult.Where(r => r is IScrewQcMutableResult).ToList();
                var removedMutableResultsCount = removedMutableResults.Count;

                foreach (var remainResults in _screwQcResultDatabase.Values)
                {
                    var remainMutableResults =
                        remainResults.Where(r => r is IScrewQcMutableResult).ToList();

                    if (removedMutableResultsCount != remainMutableResults.Count)
                    {
                        throw new IDSException("Some screw qc result is not tally");
                    }

                    for (var i = 0; i < removedMutableResultsCount; i++)
                    {
                        var removedMutableResult = removedMutableResults[i];
                        var remainMutableResult = remainMutableResults[i];
                        if (removedMutableResult.GetScrewQcCheckName() != remainMutableResult.GetScrewQcCheckName())
                        {
                            IDSPluginHelper.WriteLine(LogCategory.Warning,
                                $"The screw result is not tally, \"{removedMutableResult.GetScrewQcCheckName()}\" != \"{remainMutableResult.GetScrewQcCheckName()}\"");
                            continue;
                        }
                        
                        ((IScrewQcMutableResult)remainMutableResult).RemoveScrewFromResult(removedScrewId);
                    }
                }
            }
        }

        private void AddScrewQcResultsInExistingDatabase(Guid addedScrewId, ImmutableList<IScrewQcResult> addedResults, ScrewInfoRecordHelper helper)
        {
            var addedMutableResults = addedResults.Where(r => r is IScrewQcMutableResult).ToList();
            var addedMutableResultsCount = addedMutableResults.Count;

            foreach (var existingIdAndResults in _screwQcResultDatabase)
            {
                var existingScrewId = existingIdAndResults.Key;
                var existingResults = existingIdAndResults.Value;
                var existingMutableResults = existingResults.Where(r => r is IScrewQcMutableResult).ToList();

                if (existingMutableResults.Count != addedMutableResultsCount)
                {
                    throw new IDSException("Some screw qc result is not tally");
                }

                for (var i = 0; i < addedMutableResultsCount; i++)
                {
                    var existingMutableResult = existingMutableResults[i];
                    var addedResult = addedMutableResults[i];
                    if (existingMutableResult.GetScrewQcCheckName() != addedResult.GetScrewQcCheckName())
                    {
                        IDSPluginHelper.WriteLine(LogCategory.Warning,
                            $"The screw result is not tally, \"{existingMutableResult.GetScrewQcCheckName()}\" != \"{existingMutableResult.GetScrewQcCheckName()}\"");
                        continue;
                    }
                    
                    ((IScrewQcMutableResult)existingMutableResult).AddScrewToResult(helper.GetRecordById(existingScrewId),
                        helper.GetRecordById(addedScrewId), addedResult);
                }
            }

            _screwQcResultDatabase.Add(addedScrewId, addedResults);
        }

        private void UpdateScrewInfoRecordInExistingDatabase(List<ScrewInfoRecord> unchangedScrewInfoRecords)
        {
            foreach (var existingIdAndResults in _screwQcResultDatabase)
            {
                var existingResults = existingIdAndResults.Value;
                foreach (var existingResult in existingResults)
                {
                    if (existingResult is IScrewQcMutableResult existingMutableResult)
                    {
                        existingMutableResult.UpdateLatestScrewInResult(unchangedScrewInfoRecords);
                    }
                }
            }
        }

        private void PostUpdate()
        {
            foreach (var existingIdAndResults in _screwQcResultDatabase)
            {
                var existingResults = existingIdAndResults.Value;
                foreach (var existingResult in existingResults)
                {
                    if (existingResult is IScrewQcMutableResult existingMutableResult)
                    {
                        existingMutableResult.PostUpdate();
                    }
                }
            }
        }

        public void Update(CMFImplantDirector director, ScrewQcCheckerManager screwQcCheckerManager, 
            out Dictionary<Guid, Dictionary<string,long>> totalTimeTracker)
        {
            totalTimeTracker = new Dictionary<Guid, Dictionary<string, long>>();
            var historicalRecords = _tracker.GetHistoricalRecords();
            var latestRecords = _tracker.GetLatestRecords(director);
            _tracker.UpdateRecords(director);
            
            var changedDetectorDataModel = ScrewChangedDetector.CompareScrews(latestRecords, historicalRecords);
            var screwInfoRecordHelper = new ScrewInfoRecordHelper(director);

            var removedScrewsId = new List<Guid>();
            removedScrewsId.AddRange(changedDetectorDataModel.RemovedScrewsRecords.Select(r => r.Id));
            removedScrewsId.AddRange(changedDetectorDataModel.ChangedScrewsRecords.Select(r => r.Id));

            foreach (var id in removedScrewsId)
            {
                RemoveScrewQcResultsInExistingDatabase(id);
            }

            UpdateScrewInfoRecordInExistingDatabase(changedDetectorDataModel.UnchangedScrewsRecords);

            var addedScrewRecords = new List<ScrewInfoRecord>();
            addedScrewRecords.AddRange(changedDetectorDataModel.AddedScrewsRecords);
            addedScrewRecords.AddRange(changedDetectorDataModel.ChangedScrewsRecords);
            var addedScrews = addedScrewRecords.Select(r => screwInfoRecordHelper.GetScrewByRecord(r));

            foreach (var screw in addedScrews)
            {
                var results = screwQcCheckerManager.Check(screw, out var timeTracker);
                totalTimeTracker.Add(screw.Id, timeTracker);
                AddScrewQcResultsInExistingDatabase(screw.Id, results, screwInfoRecordHelper);
            }

            PostUpdate();
        }

        public ImmutableDictionary<Guid, ImmutableList<IScrewQcResult>> GetTrackScrewResults()
        {
            return _screwQcResultDatabase.ToImmutableDictionary();
        }

        public void GetSerializableData(out ImmutableList<CommonScrewSerializableDataModel> latestScrewSerializableDataModels,
            out ImmutableDictionary<Guid, ImmutableDictionary<string, object>> latestScrewSerializableQcResult)
        {
            latestScrewSerializableDataModels = _tracker.GetLatestCommonScrewSerializableDataModel().ToImmutableList();

            latestScrewSerializableQcResult = _screwQcResultDatabase.ToDictionary(
                r => r.Key, 
                r => r.Value.ToImmutableDictionary(
                    c => c.GetScrewQcCheckName(), 
                    c => c.GetSerializableScrewQcResult())).ToImmutableDictionary();
        }

        public void RecheckCertainResult(ScrewQcCheckerManager checkerManager, IEnumerable<Screw> screws)
        {
            foreach (var screw in screws)
            {
                if (_screwQcResultDatabase.TryGetValue(screw.Id, out var screwQcResults))
                {
                    var latestResults = checkerManager.Check(screw, out _);
                    var resultInDatabase = screwQcResults.ToList();
                    foreach (var latestResult in latestResults)
                    {
                        for (var i = 0; i < resultInDatabase.Count; i++)
                        {
                            if (resultInDatabase[i].GetScrewQcCheckName() == latestResult.GetScrewQcCheckName())
                            {
                                resultInDatabase[i] = latestResult;
                            }
                        }
                    }

                    _screwQcResultDatabase[screw.Id] = resultInDatabase.ToImmutableList();
                }
            }
        }
    }
}
