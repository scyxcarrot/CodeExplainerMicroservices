using System;
using MDCK = Materialise.SDK.MDCK;

namespace RhinoMatSDKOperations.IO
{
    public class MDCKConversion
    {
        public static bool ExportMDCK2StlFile(MDCK.Model.Objects.Model inmodel, string path)
        {
            // Write temporary STL
            //string filepath = System.IO.Path.GetTempPath() + "IDS_" + Guid.NewGuid().ToString() + ".stl";

            using (var writer = new MDCK.Operators.ModelExportToStl())
            {
                writer.Model = inmodel;
                writer.MmPerUnit = 1.0;
                writer.ExportAsAscii = false;
                writer.ExportAsMultipleSurfaces = false;
                writer.ExportIncludeColor = false;
                writer.FileName = path;
                try
                {
                    writer.Operate();
                }
                catch (MDCK.Operators.ModelExportToStl.Exception e)
                {
                    Console.WriteLine(e.Message);
                    return false;
                }
            }

            return true;
        }

    }
}