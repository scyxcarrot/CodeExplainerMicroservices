using IDS.Core.V2.Geometry;
using IDS.Interface.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Core.V2.Utilities
{
    public class ImportUtilities
    {
        public static List<IMesh> ImportStl(string[] fileNames)
        {
            var stlWithFilenameDict = ImportStlWithFilenameDict(fileNames);
            return stlWithFilenameDict.Values.ToList();
        }

        public static Dictionary<string, IMesh> ImportStlWithFilenameDict(string[] fileNames)
        {
            var meshes = new Dictionary<string, IMesh>();

            foreach (var name in fileNames)
            {
                var imported = ImportStl(name);
                if (imported != null)
                {
                    meshes.Add(name, imported);
                }
            }

            return meshes;
        }

        public static IMesh ImportStl(string path)
        {
            return StlUtilitiesV2.StlBinaryToIDSMesh(path, out var mesh) ? mesh : null;
        }
    }
}