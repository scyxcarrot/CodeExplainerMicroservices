using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;

namespace IDS.CMF.DataModel
{
    public class QCExportStlGroupDataModel
    {
        public int[] Color { get; set; }
        public List<Mesh> Meshes { get; set; }
    }
}
