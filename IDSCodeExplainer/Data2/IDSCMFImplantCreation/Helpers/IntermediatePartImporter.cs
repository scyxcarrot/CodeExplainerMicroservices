using IDS.Core.V2.Geometry;
using IDS.Core.V2.Utilities;
using IDS.Interface.Geometry;

namespace IDS.CMFImplantCreation.Helpers
{
    public static class IntermediatePartImporter
    {
        public static bool ImportMesh(string stlFilePath, out IMesh mesh)
        {
            return StlUtilitiesV2.StlBinaryToIDSMesh(stlFilePath, out mesh);
        }

        public static bool ImportJson<TType>(string jsonFilePath, out TType data)
        {
            try
            {
                data = JsonUtilities.DeserializeFile<TType>(jsonFilePath);
                return data != null;
            }
            catch 
            {
                data = default;
                return false;
            }
        }
    }
}
