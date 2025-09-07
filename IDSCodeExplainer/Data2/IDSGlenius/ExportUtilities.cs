using IDS.Core.PluginHelper;
using IDS.Glenius.Operations;
using Rhino;
using Rhino.Geometry;
using System.Collections.Generic;

namespace IDS.Glenius
{
    public static class ExportUtilities
    {

        public static void ExportBrep(Brep brep, string fileName, string outputDirectory, RhinoDoc doc)
        {
            var expt = new ObjectExporter(doc)
            {
                ExportDirectory = outputDirectory
            };

            if (!expt.ExportStp(brep, fileName))
            {
                throw new IDSException($"Export Brep {fileName} has failed");
            }
        }

        public static void ExportBreps(Dictionary<string, Brep> breps, string outputDirectory, RhinoDoc doc)
        {
            foreach (var intermediate in breps)
            {
                ExportUtilities.ExportBrep(intermediate.Value, intermediate.Key, outputDirectory, doc);
            }
        }

    }
}
