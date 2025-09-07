using IDS.CMFImplantCreation.Helpers;
using IDS.Core.V2.MTLS.Operation;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using System.IO;

namespace IDS.CMFImplantCreation.UnitTests
{
    internal class Importer
    {
        protected TType ImportJson<TType>(string path)
        {
            if (IntermediatePartImporter.ImportJson<TType>(path, out var data))
            {
                return data;
            }
            throw new FileLoadException($"Failed to import \"{path}\"");
        }

        protected IMesh ImportMesh(string path)
        {
            if (IntermediatePartImporter.ImportMesh(path, out var mesh))
            {
                return mesh;
            }
            throw new FileLoadException($"Failed to import \"{path}\"");
        }

        protected IMesh ImportMeshWithoutIdenticalVertices(string path, IConsole console)
        {
            if (IntermediatePartImporter.ImportMesh(path, out var mesh))
            {
                return TrianglesV2.CombineAndCompactIdenticalVertices(console, mesh);
            }
            throw new FileLoadException($"Failed to import \"{path}\"");
        }
    }
}
