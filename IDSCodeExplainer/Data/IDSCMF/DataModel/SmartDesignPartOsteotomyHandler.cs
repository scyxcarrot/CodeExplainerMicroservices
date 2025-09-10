using System;
using System.Collections.Generic;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;

namespace IDS.CMF.DataModel
{
    public class SmartDesignPartOsteotomyHandler
    {
        public string OsteotomyPartName { get; set; }
        public string OsteotomyType { get; set; }
        public double OsteotomyThickness { get; set; }
        public Dictionary<string, double[]> OsteotomyHandler { get; set; }
    }

    public class SmartDesignPartOsteotomyHandlerList
    {
        public List<SmartDesignPartOsteotomyHandler> ExportedParts { get; set; }

        public SmartDesignPartOsteotomyHandlerList()
        {
            ExportedParts = new List<SmartDesignPartOsteotomyHandler>();
        }
    }
}
