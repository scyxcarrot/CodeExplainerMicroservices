using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IDS.CMF.DataModel
{
    public interface ISerializable<T>
    {
        string SerializationLabel { get; }

        bool Serialize(T serializer);
        bool DeSerialize(T serializer);
    }
}
