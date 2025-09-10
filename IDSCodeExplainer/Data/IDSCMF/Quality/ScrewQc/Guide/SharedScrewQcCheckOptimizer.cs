using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace IDS.CMF.ScrewQc
{
    public class SharedScrewQcCheckOptimizer<TResult> where TResult : ISharedScrewQcResult
    {
        public class SharedScrewQcCheckResult
        {
            public ImmutableList<Guid> SharedScrewsGuid { get; set; }

            public TResult Result { get; set; }
        }

        private readonly List<SharedScrewQcCheckResult> _results;

        public SharedScrewQcCheckOptimizer()
        {
            _results = new List<SharedScrewQcCheckResult>();
        }

        public void Add(ImmutableList<Guid> sharedScrewsGuid, TResult content)
        {
            _results.Add(new SharedScrewQcCheckResult()
            {
                SharedScrewsGuid = sharedScrewsGuid,
                Result = content
            });
        }

        public bool Get(Guid screwGuid,  out TResult clonedResult)
        {
            var theResult = _results.FirstOrDefault(r => r.SharedScrewsGuid.Any(s => s == screwGuid));
            clonedResult = default;

            if (theResult == null)
            {
                return false;
            }

            clonedResult = (TResult)theResult.Result.CloneSharedScrewRelatedResult();
            return true;
        }
    }
}