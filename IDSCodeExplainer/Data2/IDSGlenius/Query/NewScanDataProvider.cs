using System.IO;
using System.Linq;

namespace IDS.Glenius.Operations
{
    public class NewScanDataProvider : PreopDataProvider
    {
        public string GetAxialPlanePath(string folderPath)
        {
            var directory = new DirectoryInfo(folderPath);
            var files = directory.GetFiles("*_RegisteredAxialPlane.xml", SearchOption.TopDirectoryOnly).Select(file => file.FullName);
            return files.First();
        }
    }
}