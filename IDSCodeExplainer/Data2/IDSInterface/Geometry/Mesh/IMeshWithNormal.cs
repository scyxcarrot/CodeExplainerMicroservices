using System.Collections.Generic;

namespace IDS.Interface.Geometry
{
    public interface IMeshWithNormal : IMesh
    {
        IList<IVector3D> VerticesNormal { get; }
        IList<IVector3D> FacesNormal { get; }
    }
}
