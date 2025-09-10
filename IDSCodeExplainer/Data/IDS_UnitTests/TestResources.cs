using System.IO;
using System.Reflection;

namespace IDS.Testing
{
    public static class TestResources
    {
        private static readonly string _executingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)?.ToString();

        public static readonly string DatabaseFilePath = Path.Combine(_executingPath, "sample.db");
    }
}
