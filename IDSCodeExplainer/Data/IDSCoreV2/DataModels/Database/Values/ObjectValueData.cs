using IDS.Interface.Geometry;
using System;
using System.Collections.Generic;

namespace IDS.Core.V2.DataModels
{
    public class ObjectValueData : GenericValueData<ObjectValue>
    {
        public ObjectValueData(Guid id, IEnumerable<Guid> parents, ObjectValue value) :
            base(id, parents, value)
        {
        }
    }

    public class ObjectValue
    {
        public Dictionary<string, object> Attributes { get; set; }
    }
}