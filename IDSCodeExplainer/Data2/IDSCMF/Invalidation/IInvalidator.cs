using IDS.CMF.ImplantBuildingBlocks;
using System;
using System.Collections.Generic;

namespace IDS.CMF.Invalidation
{
    public class PartProperties
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public IBB Block { get; set; }

        public PartProperties(Guid id, string name, IBB block)
        {
            Id = id;
            Name = name;
            Block = block;
        }
    }

    public interface IInvalidator
    {
        void SetInternalGraph();
        List<PartProperties> Invalidate(List<PartProperties> partsThatChanged);
    }
}
