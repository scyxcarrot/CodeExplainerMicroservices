using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.V2.ScrewQc
{
    public class RelatedScrewQcCheckOptimizer<TContent>
    {
        public class RelationScrewQcResult
        {
            public Guid ScrewAGuid { get; set; }

            public Guid ScrewBGuid { get; set; }

            public TContent Content { get; set; }
        }

        private readonly List<RelationScrewQcResult> _results;

        public RelatedScrewQcCheckOptimizer()
        {
            _results = new List<RelationScrewQcResult>();
        }

        public void Add(Guid screwAGuid, Guid screwBGuid, TContent content)
        {
            _results.Add(new RelationScrewQcResult()
            {
                ScrewAGuid = screwAGuid,
                ScrewBGuid = screwBGuid,
                Content = content
            });
        }

        public bool Get(Guid screwAGuid, Guid screwBGuid, out TContent content)
        {
            var theResult = _results.FirstOrDefault(r => (r.ScrewAGuid == screwAGuid && r.ScrewBGuid == screwBGuid) || 
                (r.ScrewAGuid == screwBGuid && r.ScrewBGuid == screwAGuid));
            content = default;

            if (theResult == null)
            {
                return false;
            }

            content = theResult.Content;
            return true;
        }
    }
}
