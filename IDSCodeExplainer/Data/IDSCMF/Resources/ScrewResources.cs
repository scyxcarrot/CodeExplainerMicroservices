using IDS.CMF.Utilities;
using System.Collections.Generic;

namespace IDS.CMF.FileSystem
{
    public class ScrewResources : CMFResources
    {
        public IEnumerable<string> GetGaugesFilePath(string screwType)
        {
            return ScrewEntityImportPathHelper.GetGaugesFilePath(screwType);
        }
    }
}