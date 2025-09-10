using Rhino.Collections;
using System;

namespace IDS.CMF.DataModel
{
    public interface IGuideSurface : ICloneable, ISerializable<ArchivableDictionary>
    {
        double Diameter { get; set; }

        bool IsNegative { get; set; }
    }
}