using System.Collections.Generic;

namespace IDS.Interface.Geometry
{
    public interface IMesh
    {
        IList<IVertex> Vertices { get; }
        IList<IFace> Faces { get; }
    }
}
