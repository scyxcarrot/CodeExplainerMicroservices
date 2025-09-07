using System.IO;

namespace IDS.CMFImplantCreation.UnitTests
{
    public class RecursiveDirectoryReader
    {
        private readonly string _workDir;

        public RecursiveDirectoryReader(string workDir)
        {
            _workDir = workDir;
        }

        public string[] Search(string pattern)
        {
            return Directory.GetDirectories(_workDir, pattern, SearchOption.AllDirectories); ;
        }
    }
}
