using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Quality;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace IDS.CMF.ScrewQc
{
    public class ScrewAtOriginalPosOptimizer
    {
        private class OptimizerCacheInfo
        {
            public Screw ScrewAtOriginalPosition { get; set; }

            public Mesh OriginalBone { get; set; }

            public Mesh PlannedBone { get; set; }

            public Transform TransformationToOriginal { get; set; }
        }

        private Dictionary<Guid, OptimizerCacheInfo> _screwsAtOriginalPostionCache;

        private readonly CMFScrewAtOriginalPositionHelper _screwAtOriginalPositionHelper;

        private readonly ImmutableList<Mesh> _originalOsteotomyParts;

        public bool NoOsteotomy => _originalOsteotomyParts.Count == 0;

        public List<Mesh> OriginalOsteotomyParts => _originalOsteotomyParts.ToList();

        public ScrewAtOriginalPosOptimizer(PreImplantScrewQcInput preImplantScrewQcInput)
        {
            _screwsAtOriginalPostionCache = new Dictionary<Guid, OptimizerCacheInfo>();
            _screwAtOriginalPositionHelper =
                new CMFScrewAtOriginalPositionHelper(preImplantScrewQcInput.ScrewRegistration);
            _originalOsteotomyParts = preImplantScrewQcInput.OriginalOsteotomyParts;
        }

        public Screw GetScrewAtOriginalPosition(Screw screwOnPlanned, out Mesh originalBone,
            out Transform transformationToOriginal, out Mesh plannedBone)
        {
            if (_screwsAtOriginalPostionCache.TryGetValue(screwOnPlanned.Id, out var cache))
            {
                originalBone = cache.OriginalBone;
                transformationToOriginal = cache.TransformationToOriginal;
                plannedBone = cache.PlannedBone;
                return cache.ScrewAtOriginalPosition;
            }

            var screwAtOriginalPosition = _screwAtOriginalPositionHelper.GetScrewAtOriginalPosition(screwOnPlanned, out originalBone, out transformationToOriginal, out plannedBone);
            _screwsAtOriginalPostionCache.Add(screwOnPlanned.Id, new OptimizerCacheInfo()
            {
                ScrewAtOriginalPosition = screwAtOriginalPosition,
                OriginalBone = originalBone,
                TransformationToOriginal = transformationToOriginal,
                PlannedBone = plannedBone
            });

            return screwAtOriginalPosition;
        }

        public Screw GetScrewAtOriginalPosition(Screw screwOnPlanned)
        {
            return GetScrewAtOriginalPosition(screwOnPlanned, out _, out _, out _);
        }
    }
}
