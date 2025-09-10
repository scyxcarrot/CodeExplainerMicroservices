using System;
using System.Collections.Generic;

namespace IDS.Core.V2.DataModels
{
    public abstract class GenericValueData<TValue>: 
        AtomicData
    {
        public TValue Value { get; }

        protected GenericValueData(Guid id, IEnumerable<Guid> parents, TValue value) : 
            base(id, parents)
        {
            Value = value;
        }
    }
}
