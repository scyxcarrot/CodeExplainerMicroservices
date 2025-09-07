using IDS.Core.V2.TreeDb.Interface;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace IDS.Testing
{
    public class DummyData : IData
    {
        private readonly List<Guid> _parents;

        public Guid Id { get; }

        public string Name { get; }

        public ImmutableList<Guid> Parents => _parents.ToImmutableList();

        public DummyData(string name)
        {
            Id = Guid.NewGuid();
            _parents = new List<Guid>();
            Name = name;
        }

        public DummyData(string name, IEnumerable<Guid> parents) : this(name)
        {
            _parents.AddRange(parents);
        }

        public void AddParent(Guid id)
        {
            if (!_parents.Contains(id))
            {
                _parents.Add(id);
            }
        }
    }
}
