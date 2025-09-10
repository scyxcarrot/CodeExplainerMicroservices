using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Runtime.InProcess;

namespace IDS.Testing.UnitTests
{
#if (Rhino7Installed)
    [TestClass]
    public class RhinoInsideStartupHandler
    {
        private static RhinoCore _rhinoCore;

        #region Program static constructor
        // It must initialize in static constructor
        static RhinoInsideStartupHandler()
        {
            RhinoInside.Resolver.Initialize();
        }
        #endregion

        [AssemblyInitialize]
        public static void StartSession(TestContext context)
        {
            if (_rhinoCore == null)
            {
                _rhinoCore = new RhinoCore();
            }
        }

        [AssemblyCleanup]
        public static void EndSession()
        {
            _rhinoCore?.Dispose();
            _rhinoCore = null;
        }
    }
#endif
}
