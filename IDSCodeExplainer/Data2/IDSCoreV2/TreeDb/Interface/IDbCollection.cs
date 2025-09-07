using System;
using System.Collections.Generic;

namespace IDS.Core.V2.TreeDb.Interface
{
    public interface IDbCollection
    {
        string Name { get; }

        Type DataType { get; }

        bool Create(IData data);

        IData Read(Guid id);

        IData Delete(Guid id);

        IList<IData> ReadAll();

        void MapObject(IVersion savedVersion);
    }
}
