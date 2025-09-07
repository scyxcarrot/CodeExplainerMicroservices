using IDS.Glenius.FileSystem;
using Rhino;
using System.IO;

namespace IDS.Glenius.Operations
{
    public static class GleniusProjectDirectories
    {



        public static string GenerateScrewQCExportDirectory(RhinoDoc doc)
        {
            var preOpDir = DirectoryStructure.GetDocumentDirectory(doc);
            preOpDir += "ScrewQCExport";
            return preOpDir;
        }


        public static void CreateScrewQCExportDirectory(RhinoDoc doc)
        {
            var dir = GenerateScrewQCExportDirectory(doc);
            Directory.CreateDirectory(dir);
        }

    }
}
