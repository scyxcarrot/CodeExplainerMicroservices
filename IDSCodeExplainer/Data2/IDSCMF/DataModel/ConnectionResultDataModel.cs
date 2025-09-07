using IDS.Interface.Implant;
using Rhino.Geometry;
using System.Collections.Generic;

namespace IDSCMF.DataModel
{
    public class ConnectionResultDataModel
    {
        public Mesh ConnectionMesh { get; set; }
        public List<IDot> Dots { get; set; }
        public bool Success { get; set; }
        public List<string> ErrorMessages { get; set; }
    }
}
