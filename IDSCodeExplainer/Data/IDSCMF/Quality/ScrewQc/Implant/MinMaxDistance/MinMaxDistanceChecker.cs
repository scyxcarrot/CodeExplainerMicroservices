using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Quality;
using IDS.CMF.V2.ScrewQc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.ScrewQc
{
    public class MinMaxDistancesChecker : ImplantScrewQcProxyChecker
    {
        public class MinMaxResult
        {
            public bool IsTooClose { get; set; }
            public bool IsTooFar { get; set; }

            public MinMaxResult()
            {
                IsTooClose = false;
                IsTooFar = false;
            }
        }

        private readonly CMFImplantDirector _director;

        public override string ScrewQcCheckTrackerName => "Min Max Distance Check";

        private readonly RelatedScrewQcCheckOptimizer<MinMaxResult> _relatedScrewQcCheckOptimizer;

        public MinMaxDistancesChecker(CMFImplantDirector director) :
            base(ImplantScrewQcCheck.MinMaxDistance)
        {
            _director = director;
            _relatedScrewQcCheckOptimizer = new RelatedScrewQcCheckOptimizer<MinMaxResult>();
        }

        public override IScrewQcResult Check(Screw screw)
        {
            var content = MinMaxDistanceCheck(screw);
            return new MinMaxDistanceResult(ScrewQcCheckName, content);
        }

        public MinMaxDistanceContent MinMaxDistanceCheck(Screw screw)
        {
            var objectManager = new CMFObjectManager(_director);
            var casePreference = objectManager.GetCasePreference(screw);

            var screwManager = new ScrewManager(_director);
            var allScrews = screwManager.GetScrews(casePreference, false);

            foreach (var otherScrew in allScrews)
            {
                if (otherScrew.Id == screw.Id || _relatedScrewQcCheckOptimizer.Get(screw.Id, otherScrew.Id, out _))
                {
                    continue;
                }

                var screwAnalysis = new CMFScrewAnalysis(_director);
                screwAnalysis.PerformMinMaxDistanceCheck(screw, out var tooCloseDistanceProblems, out var tooFarDistanceProblems, false);
                screwAnalysis.Dispose();

                UpdateTestedResult(screw, allScrews, tooCloseDistanceProblems, tooFarDistanceProblems);
            }

            var content = ConstructScrewQcResultContentFromOptimizer(allScrews, screw);

            return content;
        }

        private void UpdateTestedResult(Screw screw, List<Screw> allScrews, List<Screw> tooCloseDistanceProblems, List<Screw> tooFarDistanceProblems)
        {
            foreach (var otherScrew in allScrews)
            {
                if (otherScrew.Id == screw.Id || _relatedScrewQcCheckOptimizer.Get(screw.Id, otherScrew.Id, out _))
                {
                    continue;
                }

                var content = new MinMaxResult();

                if (tooCloseDistanceProblems.Contains(otherScrew))
                {
                    content.IsTooClose = true;
                }

                if (tooFarDistanceProblems.Contains(otherScrew))
                {
                    content.IsTooFar = true;
                }

                _relatedScrewQcCheckOptimizer.Add(screw.Id, otherScrew.Id, content);
            }
        }

        private MinMaxDistanceContent ConstructScrewQcResultContentFromOptimizer(List<Screw> allScrews, Screw screw)
        {
            var tooClosedScrews = new List<Screw>();
            var tooFarScrews = new List<Screw>();

            foreach (var screwB in allScrews)
            {
                if (screw.Id == screwB.Id)
                {
                    continue;
                }

                if (_relatedScrewQcCheckOptimizer.Get(screw.Id, screwB.Id, out var result))
                {
                    if (result.IsTooClose)
                    {
                        tooClosedScrews.Add(screwB);
                    }

                    if (result.IsTooFar)
                    {
                        tooFarScrews.Add(screwB);
                    }
                }
                else
                {
                    throw new Exception("Optimizer for Implant Screw MinMaxDistance Checker could not find result!");
                }
            }

            var content = new MinMaxDistanceContent
            {
                TooCloseScrews = tooClosedScrews.Select(s =>
                    (ScrewInfoRecord)new ImplantScrewInfoRecord(s)).ToList(),
                TooFarScrews = tooFarScrews.Select(s =>
                    (ScrewInfoRecord)new ImplantScrewInfoRecord(s)).ToList()
            };

            return content;
        }
    }
}
