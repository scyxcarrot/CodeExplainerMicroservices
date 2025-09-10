using IDS.CMF.CasePreferences;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.V2.ScrewQc;
using IDS.Core.Utilities;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace IDS.CMF.ScrewQc
{
    public class ClearanceVicinityChecker: GuideScrewQcChecker<ClearanceVicinityResult>
    {
        private readonly bool _isLiveUpdate;

        private readonly CMFObjectManager _objectManager;

        private readonly ImmutableDictionary<GuidePreferenceDataModel, ImmutableDictionary<ScrewInfoRecord, Brep>> _allImplantScrewRecordsAndBarrels;

        public override string ScrewQcCheckTrackerName => "Clearance";

        public ClearanceVicinityChecker(CMFImplantDirector director, bool isLiveUpdate) :
            base(director, GuideScrewQcCheck.ClearanceVicinity)
        {
            _isLiveUpdate = isLiveUpdate;

            _objectManager = new CMFObjectManager(director);

            _allImplantScrewRecordsAndBarrels = ConstructAllImplantScrewRecordsAndBarrels(director);
        }

        private ImmutableDictionary<GuidePreferenceDataModel, ImmutableDictionary<ScrewInfoRecord, Brep>> ConstructAllImplantScrewRecordsAndBarrels(CMFImplantDirector director)
        {
            var allImplantScrewRecordsAndBarrels = new Dictionary<GuidePreferenceDataModel, ImmutableDictionary<ScrewInfoRecord, Brep>>();
            foreach (var guidePreferenceDataModel in director.CasePrefManager.GuidePreferences)
            {
                var implantScrewRecordsAndBarrels = new Dictionary<ScrewInfoRecord, Brep>();
                  var linkedImplantScrewsIds = guidePreferenceDataModel.LinkedImplantScrews;
                linkedImplantScrewsIds.ForEach(id =>
                {
                    var implantScrew = director.Document.Objects.Find(id) as Screw;
                    if (implantScrew != null && !implantScrew.ScrewGuideAidesInDocument.ContainsKey(IBB.RegisteredBarrel))
                    {
                        return;
                    }

                    var selectedBarrelId = implantScrew.ScrewGuideAidesInDocument[IBB.RegisteredBarrel];
                    var selectedBarrel = director.Document.Objects.Find(selectedBarrelId).Geometry as Brep;
                    implantScrewRecordsAndBarrels.Add(new ImplantScrewInfoRecord(implantScrew), selectedBarrel);
                });
                allImplantScrewRecordsAndBarrels.Add(guidePreferenceDataModel, implantScrewRecordsAndBarrels.ToImmutableDictionary());
            }

            return allImplantScrewRecordsAndBarrels.ToImmutableDictionary();
        }

        protected override ClearanceVicinityResult CheckForSharedScrew(Screw screw)
        {
            var vicinatedGuideScrews = CheckClearanceVicinityWithSharedScrew(screw);
            var otherGuideScrewsVicinated =  new List<ScrewInfoRecord>();
            var sharedScrews = new List<ScrewInfoRecord>();

            if (_isLiveUpdate)
            {
                otherGuideScrewsVicinated = CheckClearanceVicinityToSharedScrew(screw);
                sharedScrews = GetItSharedScrews(screw).Select(s => new GuideScrewInfoRecord(s)).Cast<ScrewInfoRecord>().ToList();
            }

            var content = new ClearanceVicinityContent()
            {
                ClearanceVicinityGuideScrews = vicinatedGuideScrews.ToList(),
                OtherGuideScrewsHadClearanceVicinity = otherGuideScrewsVicinated,
                SharedScrews = sharedScrews
            };

            return new ClearanceVicinityResult(ScrewQcCheckName, content);
        }


        protected override void CheckAndUpdateForNonSharedScrew(Screw screw, ClearanceVicinityResult result)
        {
            base.CheckAndUpdateForNonSharedScrew(screw, result);

            var vicinatedBarrels = CheckClearanceVicinityWithSelectedBarrels(screw).ToList();

            result.UpdateResult(vicinatedBarrels);
        }

        #region Vicinity Check for Shared Guide Screw

        private ImmutableList<ScrewInfoRecord> CheckClearanceVicinityWithSharedScrew(Screw screw)
        {
            var clearance = ScrewQcUtilities.CreateVicinityClearance(screw);
            var targetedScrews = GuideScrewQcUtilities.FilteredOutSharedScrews(screw, groupedSharedGuideScrews);
            var clearanceVicinityScrews = CheckClearanceVicinity(screw, clearance, targetedScrews);
            var vicinatedGroupedSharedGuideScrews = groupedSharedGuideScrews.Where(g => g.Any(ts => clearanceVicinityScrews.Any(s => s == ts.Id)));
            return vicinatedGroupedSharedGuideScrews.SelectMany(sharedScrew => 
                sharedScrew.Select(s => new GuideScrewInfoRecord(s)).Cast<ScrewInfoRecord>()).ToImmutableList();
        }

        private List<ScrewInfoRecord> CheckClearanceVicinityToSharedScrew(Screw screw)
        {
            var targetedScrews = GuideScrewQcUtilities.FilteredOutSharedScrews(screw, groupedSharedGuideScrews);
            var sharedScrews = GetItSharedScrews(screw);
            var screwBeenAffected = new List<ScrewInfoRecord>();

            foreach (var targetedScrew in targetedScrews)
            {
                var clearance = ScrewQcUtilities.CreateVicinityClearance(targetedScrew);
                if (CheckClearanceVicinity(targetedScrew, clearance, sharedScrews).Any())
                {
                    var targetedSharedScrewInfoRecord =
                        GetItSharedScrews(targetedScrew).Select(s => new GuideScrewInfoRecord(s));
                    screwBeenAffected.AddRange(targetedSharedScrewInfoRecord);
                }
            }

            return screwBeenAffected;
        }

        private ImmutableList<Guid> CheckClearanceVicinity(Screw screw, Brep clearance, ImmutableList<Screw> targetScrews)
        {
            var clearanceVicinityScrews = new List<Guid>();

            foreach (var targetScrew in targetScrews)
            {
                if (screw.Id == targetScrew.Id)
                {
                    continue;
                }

                var targetScrewAide = screwManager.GetGuideScrewEyeOrLabelTagGeometry(targetScrew);
                if (BrepUtilities.CheckBrepIntersectionBrep(clearance, targetScrew.BrepGeometry)
                    || BrepUtilities.CheckBrepIntersectionBrep(clearance, targetScrewAide))
                {
                    clearanceVicinityScrews.Add(targetScrew.Id);
                }
            }

            return clearanceVicinityScrews.ToImmutableList();
        }

        #endregion

        #region Vicinity Check for Barrel

        private ImmutableList<ScrewInfoRecord> CheckClearanceVicinityWithSelectedBarrels(Screw screw)
        {
            var clearance = ScrewQcUtilities.CreateVicinityClearance(screw);
            var guidePreferenceDataModel = _objectManager.GetGuidePreference(screw);
            var implantScrewsAndBarrels = _allImplantScrewRecordsAndBarrels[guidePreferenceDataModel];
            return CheckClearanceVicinity(clearance, implantScrewsAndBarrels.ToImmutableDictionary());
        }

        private ImmutableList<ScrewInfoRecord> CheckClearanceVicinity(Brep clearance, 
            ImmutableDictionary<ScrewInfoRecord, Brep> targetImplantScrewRecordsAndBarrels)
        {
            var clearanceVicinityScrews = new List<ScrewInfoRecord>();

            foreach (var targetImplantScrewRecordAndBarrel in targetImplantScrewRecordsAndBarrels)
            {
                var screwInfoRecord = targetImplantScrewRecordAndBarrel.Key;
                var barrel = targetImplantScrewRecordAndBarrel.Value;
                if (BrepUtilities.CheckBrepIntersectionBrep(clearance, barrel))
                {
                    clearanceVicinityScrews.Add(screwInfoRecord);
                }
            }

            return clearanceVicinityScrews.ToImmutableList();
        }

        #endregion
    }
}
