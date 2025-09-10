using IDS.CMF.V2.Loader;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDS.Testing.UnitTests
{
#if (Rhino7Installed)

    [TestClass]
    public class PreopLoaderTests
    {
        [TestMethod]
        public void Factory_Can_Return_Correct_Loader_For_SPPC_File()
        {
            var console = new TestConsole();

            var factory = new PreopLoaderFactory();
            var sppcLoader = factory.GetLoader(console, "dummy.sppc");

            Assert.IsNotNull(sppcLoader, "Returned loader is null!");
            Assert.IsTrue(sppcLoader is ProplanLoader);
        }

        [TestMethod]
        public void Factory_Can_Return_Correct_Loader_For_MCS_File()
        {
            var console = new TestConsole();

            var factory = new PreopLoaderFactory();
            var mcsLoader = factory.GetLoader(console, "dummy.mcs");

            Assert.IsNotNull(mcsLoader, "Returned loader is null!");
            Assert.IsTrue(mcsLoader is EnlightCMFLoader);
        }

        [TestMethod]
        public void SPPC_Loader_Can_Return_PartNames()
        {
            var resource = new TestResources();
            var console = new TestConsole();

            var factory = new PreopLoaderFactory();
            var sppcLoader = factory.GetLoader(console, resource.SPPCFilePath);
            var partInfos = sppcLoader.GetPartInfos();

            Assert.IsNotNull(partInfos, "Returned list is null!");
            Assert.IsTrue(partInfos.Count > 0);
        }

        [Ignore]
        [TestMethod]
        public void MCS_Loader_Can_Return_PartNames()
        {
            var resource = new TestResources();
            var console = new TestConsole();

            var factory = new PreopLoaderFactory();
            var mcsLoader = factory.GetLoader(console, resource.CompleteWorkflowEnlightCmfFilePath);
            var partInfos = mcsLoader.GetPartInfos();

            Assert.IsNotNull(partInfos, "Returned list is null!");
            Assert.IsTrue(partInfos.Count > 0);
        }
    }

#endif
}
