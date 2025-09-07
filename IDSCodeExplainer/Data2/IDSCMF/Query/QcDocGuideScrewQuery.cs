using IDS.CMF.CasePreferences;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.ScrewQc;
using IDS.Core.PluginHelper;
using System.Collections.Generic;
using System.Diagnostics;

namespace IDS.CMF.Query
{

    public class QcDocGuideScrewQuery
    {
        private readonly CMFImplantDirector _director;
        private readonly ScrewQcCheckerManager _screwQcCheckerManager;
        private readonly PreGuideScrewQcInput _preGuideScrewQcInput;

        public QcDocGuideScrewQuery(CMFImplantDirector director)
        {
            var timerComponent = new Stopwatch();
            timerComponent.Start();
            var timeRecorded = new Dictionary<string, string>();

            _director = director;

            _screwQcCheckerManager = GuideScrewQcUtilities.CreateScrewQcManager(director, false, ref _preGuideScrewQcInput);

            timerComponent.Stop();
            timeRecorded.Add($"Construct QcDocGuideScrewQuery", $"{ (timerComponent.ElapsedMilliseconds * 0.001)}");
            Msai.TrackDevEvent($"QCDoc Guide Screw QC - Construct QcDocGuideScrewQuery", "CMF", timeRecorded);
        }
        
        public List<QcDocScrewAndResultsInfoModel> GenerateScrewInfoModels(GuidePreferenceDataModel guidePrefData)
        {
            var screwManager = new ScrewManager(_director);
            var screws = screwManager.GetScrews(guidePrefData, true);

            return GenerateScrewInfoModels(screws);
        }

        public List<QcDocScrewAndResultsInfoModel> GenerateScrewInfoModels(List<Screw> screws)
        {
            var res = new List<QcDocScrewAndResultsInfoModel>();
            var timeRecorded = new Dictionary<string, string>();
            long totalTime = 0;

            screws.ForEach(screw =>
            {
                var qcDocBaseScrewInfoData =
                    QcDocScrewQueryUtilities.GetQcDocGuideScrewInfoData(_director, screw, out var timeRecordedScrewBasicInfo);

                foreach (var timeRecordedBasicInfo in timeRecordedScrewBasicInfo)
                {
                    timeRecorded.Add($"{timeRecordedBasicInfo.Key} {screw.Index}", $"{timeRecordedBasicInfo.Value * 0.001}");
                    totalTime += timeRecordedBasicInfo.Value;
                }

                var screwQcResults = _screwQcCheckerManager.Check(screw, out var timeRecordedScrewQcChecks);

                foreach (var timeRecordedScrewQcCheck in timeRecordedScrewQcChecks)
                {
                    timeRecorded.Add($"{timeRecordedScrewQcCheck.Key} {screw.Index}", $"{timeRecordedScrewQcCheck.Value * 0.001}");
                    totalTime += timeRecordedScrewQcCheck.Value;
                }

                res.Add(QcDocScrewQueryUtilities.GetQcDocScrewAndResultsInfoModel(qcDocBaseScrewInfoData, screwQcResults));
            });
            
            timeRecorded.Add($"Total Time", $"{ (totalTime * 0.001)}");
            Msai.TrackDevEvent($"QCDoc Guide Screw QC - SCREWDATA GenerateScrewInfoDatas", "CMF", timeRecorded);

            return res;
        }
    }
}
