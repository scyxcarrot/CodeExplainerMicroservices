using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino.Geometry;
using System.IO;

namespace IDS.CMF.Operations
{
    public class BasePartsImporter
    {
        protected readonly CMFImplantDirector director;
        protected readonly CMFObjectManager objectManager;

        public BasePartsImporter(CMFImplantDirector director)
        {
            this.director = director;
            objectManager = new CMFObjectManager(director);
        }

        protected string GetPartName(string filePath)
        {
            return Path.GetFileNameWithoutExtension(filePath);
        }

        protected bool ImportMeshWithFilePath(string filePath, out Mesh mesh)
        {
            var read = StlUtilities.StlBinary2RhinoMesh(filePath, out mesh);
            if (read)
            {
                return true;
            }

            IDSPluginHelper.WriteLine(LogCategory.Error, $"Something went wrong while reading the STL file: {GetPartName(filePath)}");
            return false;
        }
    }
}
