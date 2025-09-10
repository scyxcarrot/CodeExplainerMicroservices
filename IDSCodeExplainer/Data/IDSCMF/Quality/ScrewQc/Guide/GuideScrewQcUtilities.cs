using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Quality;
using IDS.CMF.V2.ScrewQc;
using IDS.CMF.Visualization;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Visualization;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace IDS.CMF.ScrewQc
{
    public static class GuideScrewQcUtilities
    {
        public static ScrewQcCheckerManager CreateScrewQcManager(CMFImplantDirector director,
            bool isLiveUpdate, ref PreGuideScrewQcInput preGuideScrewQcInput)
        {
            if (preGuideScrewQcInput == null)
            {
                var implantScrewAtOriginalPosWithRecords = GetImplantScrewAtOriginalPosAndRecords(director);
                preGuideScrewQcInput = new PreGuideScrewQcInput(implantScrewAtOriginalPosWithRecords);
            }

            // Arrange the order according the column of QC Doc
            return ScrewQcUtilities.CreateScrewQcManager(director, new List<IScrewQcChecker>()
            {
                new ClearanceVicinityChecker(director, isLiveUpdate),
                new GuideScrewAnatomicalObstacleChecker(director),
                new ImplantScrewIntersectChecker(director, preGuideScrewQcInput.ImplantScrewAtOriginalPosWithRecords),
                new GuideScrewIntersectChecker(director),
                new ImplantScrewGaugeIntersectChecker(director,
                    preGuideScrewQcInput.ImplantScrewAtOriginalPosWithRecords),
            });
        }

        private static ImmutableDictionary<Screw, ScrewInfoRecord> GetImplantScrewAtOriginalPosAndRecords(CMFImplantDirector director)
        {
            var screwManager = new ScrewManager(director);
            var allImplantScrews = screwManager.GetAllScrews(false);

            var helper = new OriginalPositionedScrewAnalysisHelper(director);
            var implantScrewAtOriginalPosWithRecords = GetImplantScrewAndNamesAtOriginalPlace(helper.GetAllOriginalOsteotomyParts(),
                allImplantScrews).ToDictionary(kv => kv.Key, kv => (ScrewInfoRecord)new ImplantScrewInfoRecord(kv.Value));

            return implantScrewAtOriginalPosWithRecords.ToImmutableDictionary();
        }

        public static IDictionary<Screw, Screw> GetImplantScrewAndNamesAtOriginalPlace(List<Mesh> originalOsteotomies,
            IEnumerable<Screw> allImplantScrews)
        {
            var screwAnalysis = new CMFOriginalPositionedScrewAnalysis(originalOsteotomies);

            var allImplantScrewsAtOriginalPosition = screwAnalysis.GetAllScrewsAtOriginalPosition(allImplantScrews, out var implantScrewsMap)
                .Select(s => s.Key).ToList();

            screwAnalysis.CleanUp();

            return allImplantScrewsAtOriginalPosition.ToDictionary(screw => screw, screw => implantScrewsMap[screw]);
        }

        public static bool PreScrewQcCheck(CMFImplantDirector director)
        {
            var screwManager = new ScrewManager(director);
            var allImplantScrews = screwManager.GetAllScrews(false);
            if (allImplantScrews.Exists(x => x.Index == -1))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Please ensure the implant screw number(s) are set!");
                return false;
            }

            var allIGuideScrews = screwManager.GetAllScrews(true);
            if (allIGuideScrews.Exists(x => x.Index == -1))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Please ensure the guide screw number(s) are set!");
                return false;
            }

            return true;
        }

        public static ScrewQcBubbleManager CreateScrewQcBubbleManager(CMFImplantDirector director)
        {
            var extraDisplays = new List<IDisplay>()
            {
                new DisplayAllGuideFixationScrewsGauges(director, AllGuideFixationScrewGaugesProxy.Instance),
                new DisplayAllImplantScrewsAtOriginalPosGauges(director,
                    AllScrewGaugesAtOriginalPositionProxy.Instance),
            };
            return new ScrewQcBubbleManager(extraDisplays.ToImmutableList());
        }

        public static ImmutableList<ScrewQcBubble> CreateScrewQcBubble(CMFImplantDirector director,
            ImmutableDictionary<Guid, ImmutableList<IScrewQcResult>> screwQcResults)
        {
            // Group shared guide screws and create conduit
            var screwManager = new ScrewManager(director);
            var allGuideScrews = screwManager.GetAllScrews(true);
            var groupedSharedGuideScrews = GroupScrewsInShared(allGuideScrews);

            var guideScrewQcBubbles = new List<ScrewQcBubble>();

            foreach (var sharedScrews in groupedSharedGuideScrews)
            {
                var screwInfoRecords = sharedScrews.Select(
                    s => new GuideScrewInfoRecord(s)).Cast<ScrewInfoRecord>().ToImmutableList();


                var screw = sharedScrews.ElementAt(0);
                var mergedResults = screwQcResults[screw.Id].ToList();
                for (var i = 1; i < sharedScrews.Count(); i++)
                {
                    var otherScrew = sharedScrews.ElementAt(i);
                    for (var j = 0; j < mergedResults.Count; j++)
                    {
                        if (mergedResults[j] is IContainNonSharedScrewCheckResult nonSharedScrewCheckResult)
                        {
                            var otherScrewResult = screwQcResults[otherScrew.Id][j];
                            var newMergedResult = nonSharedScrewCheckResult.Merge((IContainNonSharedScrewCheckResult)otherScrewResult);
                            mergedResults[j] = (IScrewQcResult)newMergedResult;
                        }
                    }
                }

                var messages = mergedResults.Select(r => r.GetQcBubbleMessage())
                    .Where((m => !string.IsNullOrWhiteSpace(m))).ToImmutableList();

                guideScrewQcBubbles.Add(new ScrewQcBubble(screwInfoRecords, messages));
            }

            return guideScrewQcBubbles.ToImmutableList();
        }

        public static IEnumerable<IEnumerable<Screw>> GroupScrewsInShared(IEnumerable<Screw> screws)
        {
            var groupedScrews = new List<List<Screw>>();

            foreach (var screw in screws)
            {
                var sharedScrews = screw.GetScrewItSharedWith();
                if (!sharedScrews.Any())
                {
                    groupedScrews.Add(new List<Screw>() { screw });
                }
                else
                {
                    if (groupedScrews.Any(l => l.Contains(screw)))
                    {
                        continue;
                    }

                    sharedScrews = sharedScrews.FindAll(ss => screws.Any(s => ss.Id == s.Id));

                    if (!sharedScrews.Contains(screw))
                    {
                        sharedScrews.Add(screw);
                    }

                    groupedScrews.Add(sharedScrews);
                }
            }

            return groupedScrews;
        }

        public static ImmutableList<Screw> FilteredOutSharedScrews(Screw screwForTest,
            ImmutableList<ImmutableList<Screw>> groupedGuideScrewsInShared)
        {
            return (from sharedScrews in groupedGuideScrewsInShared
                where sharedScrews.All(s => s != screwForTest)
                select sharedScrews.ElementAt(0)).ToImmutableList();
        }

        public static ImmutableList<Screw> GetSharedScrewGroup(Screw screw,
            ImmutableList<ImmutableList<Screw>> allGroupedSharedGuideScrews)
        {
            return allGroupedSharedGuideScrews.FirstOrDefault(sharedScrews => sharedScrews.Any(s => s.Id == screw.Id));
        }
    }
}
