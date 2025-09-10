using IDS.Core.V2.Geometries;
using System.Collections.Generic;

namespace IDS.CMF.DataModel
{
    public class SmartDesignPartTransformationMatrix
    {
        public string ExportedPartName { get; set; }
        public IDSTransform TransformationMatrix { get; set; }
    }

    public class SmartDesignPartTransformationMatrixList
    {
        public List<SmartDesignPartTransformationMatrix> ExportedParts { get; set; }

        public SmartDesignPartTransformationMatrixList()
        {
            ExportedParts = new List<SmartDesignPartTransformationMatrix>();
        }
    }
}
