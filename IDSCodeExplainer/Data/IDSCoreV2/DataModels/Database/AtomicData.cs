using IDS.Core.V2.TreeDb.Interface;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace IDS.Core.V2.DataModels
{
    public abstract class AtomicData: IData
    {
        public Guid Id { get; }
        public ImmutableList<Guid> Parents { get; }

        protected AtomicData(Guid id, IEnumerable<Guid> parents)
        {
            Id = id;
            Parents = parents.ToImmutableList();
        }
    }
}
