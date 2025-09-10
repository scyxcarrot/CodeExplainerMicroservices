using IDS.Core.PluginHelper;
using Rhino;
using Rhino.FileIO;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Utilities
{
    public static class StepsFileIOUtilities
    {
        public static List<GeometryBase> ImportObjectVia3dm(string filePath)
        {
            var file = File3dm.Read(filePath);
            return file.Objects.Select(x => x.Geometry).ToList();
        }

        public static Brep GetBrep(Guid guid)
        {
            Brep brepTmp = null;
            if (guid != Guid.Empty)
            {
                //Any imported .Stp files will be added in the document, and layer will be created.
                //Have to delete it once a copy is made into the memory
                var rhObj = RhinoDoc.ActiveDoc.Objects.Find(guid);

                if (rhObj.Geometry is Brep brep)
                {
                    brepTmp = brep;
                }
                else
                {
                    throw new IDSException("Rhino object is not a Brep!");
                }
            }
            
            if (brepTmp != null)
            {
                Brep brep = new Brep();
                brep.Append(brepTmp);
                RhinoDoc.ActiveDoc.Objects.Delete(guid, true);
                return brep;
            }
            else
            {
                return null;
            }

        }      

        public static Curve GetCurve(Guid guid)
        {
            Curve curveTemp = null;
            if (guid != Guid.Empty)
            {
                //Any imported .Stp files will be added in the document, and layer will be created.
                //Have to delete it once a copy is made into the memory
                var rhObj = RhinoDoc.ActiveDoc.Objects.Find(guid);

                if (rhObj.Geometry is Curve curve)
                {
                    curveTemp = curve;
                }
                else
                {
                    throw new IDSException("Rhino objects is not a Curve!");
                }
            }

            if (curveTemp != null)
            {
                var curve = curveTemp.DuplicateCurve();               
                RhinoDoc.ActiveDoc.Objects.Delete(guid, true);
                return curve;
            }
            else
            {
                return null;
            }
        }
    }
}
