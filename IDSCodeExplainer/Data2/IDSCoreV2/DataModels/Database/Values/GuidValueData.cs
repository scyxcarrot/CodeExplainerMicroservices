using System;
using System.Collections.Generic;

namespace IDS.Core.V2.DataModels
{
    public class GuidValueData : GenericValueData<Guid>
    {
        public GuidValueData(Guid id, IEnumerable<Guid> parents, Guid value) :
            base(id, parents, value)
        {
        }
    }
}