using Rhino;
using Rhino.Geometry;
using System.Collections.Generic;

namespace IDS.Core.Importer
{
    public interface IReadScrewXmlComponent<T>
    {
        //For each screw node found in XML file, this will be invoked and screw will be created.
        void OnReadScrewNodeXml(RhinoDoc doc, List<string> screwNameParts, int screwIndex,
            string screwRadius, Point3d screwHead, Point3d screwTip, out T screw);
    }
}
