using IDS.CMF.CasePreferences;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.ScrewQc;
using IDS.CMF.V2.ScrewQc;
using IDS.Core.PluginHelper;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace IDS.CMF.Query
{
    public class QcDocImplantScrewQuery
    {
        private readonly CMFImplantDirector _director;
        private readonly Dictionary<Guid, Dictionary<string, long>> _totalTimeTracker;
        private readonly ImmutableDictionary<Guid, ImmutableList<IScrewQcResult>> _screwResults;

        public QcDocImplantScrewQuery(CMFImplantDirector director)
        {
            var timerComponent = new Stopwatch();
            timerComponent.Start();
            var timeRecorded = new Dictionary<string, string>();

            _director = director;

            PreImplantScrewQcInput preImplantScrewQc = null;
            var screwQcCheckerManager = ImplantScrewQcUtilities.CreateScrewQcManager(director, ref preImplantScrewQc);

            timerComponent.Stop();
            timeRecorded.Add($"Construct QcDocImplantScrewQuery", $"{ (timerComponent.ElapsedMilliseconds * 0.001)}");
            Msai.TrackDevEvent($"QCDoc Implant Screw QC - Construct QcDocGuideScrewQuery", "CMF", timeRecorded);

            if (director.ImplantScrewQcLiveUpdateHandler == null)
            {
                var screwInfoTracker = new ScrewInfoRecordTracker(false);
                director.ImplantScrewQcLiveUpdateHandler = new ScrewQcLiveUpdateHandler(screwInfoTracker, director, screwQcCheckerManager, out _totalTimeTracker);
            }
            else
            {
                director.ImplantScrewQcLiveUpdateHandler.Update(director, screwQcCheckerManager, out _totalTimeTracker);

            }

            _screwResults = director.ImplantScrewQcLiveUpdateHandler.GetTrackScrewResults();

        }

        public List<QcDocScrewAndResultsInfoModel> GenerateScrewInfoModels(CasePreferenceDataModel casePrefData)
        {
            var screwManager = new ScrewManager(_director);
            var screws = screwManager.GetScrews(casePrefData, false);

            return GenerateScrewInfoModels(screws);
        }

        private ImmutableList<IScrewQcResult> SearchResult(Screw screw, out Dictionary<string, long> timeTracker)
        {
            // If the screw skip due to cache, no tracking
            timeTracker = _totalTimeTracker.TryGetValue(screw.Id, out var value)? 
                value:
                new Dictionary<string, long>();

            return _screwResults[screw.Id];
        }

        public List<QcDocScrewAndResultsInfoModel> GenerateScrewInfoModels(List<Screw> screws)
        {
            var res = new List<QcDocScrewAndResultsInfoModel>();
            var timeRecorded = new Dictionary<string, string>();
            long totalTime = 0;

            screws.ForEach(screw =>
            {
                var qcDocBaseScrewInfoData =
                    QcDocScrewQueryUtilities.GetQcDocImplantScrewInfoData(_director, screw, out var timeRecordedScrewBasicInfo);

                foreach (var timeRecordedBasicInfo in timeRecordedScrewBasicInfo)
                {
                    timeRecorded.Add($"{timeRecordedBasicInfo.Key} {screw.Index}", $"{timeRecordedBasicInfo.Value * 0.001}");
                    totalTime += timeRecordedBasicInfo.Value;
                }

                var screwQcResults = SearchResult(screw, out var timeRecordedScrewQcChecks);

                foreach (var timeRecordedScrewQcCheck in timeRecordedScrewQcChecks)
                {
                    timeRecorded.Add($"{timeRecordedScrewQcCheck.Key} {screw.Index}", $"{timeRecordedScrewQcCheck.Value * 0.001}");
                    totalTime += timeRecordedScrewQcCheck.Value;
                }

                res.Add(QcDocScrewQueryUtilities.GetQcDocScrewAndResultsInfoModel(qcDocBaseScrewInfoData, screwQcResults));
            });

            timeRecorded.Add($"Total Time", $"{ (totalTime * 0.001)}");
            Msai.TrackDevEvent($"QCDoc Implant Screw QC - SCREWDATA GenerateScrewInfoDatas", "CMF", timeRecorded);

            return res;
        }
    }
}
