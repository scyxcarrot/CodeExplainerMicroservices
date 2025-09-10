using IDS.CMF.V2.Loader;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDS.Testing.UnitTests
{
#if (Rhino7Installed)

    [TestClass]
    public class EnlightCMFLoaderTests
    {
        [Ignore]
        [TestMethod]
        public void Loader_Can_Dispose_Properly()
        {
            var resource = new TestResources();

            //Act: 
            //Load multiple times
            Load_A_Case(resource.CompleteWorkflowEnlightCmfFilePath, false);
            Load_A_Case(resource.NoPlannedPartEnlightCmfFilePath, true);
            Load_A_Case(resource.CompleteWorkflowEnlightCmfFilePath, false);
            Load_A_Case(resource.CompleteWorkflowEnlightCmfFilePath, true);
            Load_A_Case(resource.NoPlannedPartEnlightCmfFilePath, false);
        }

        private void Load_A_Case(string caseFilePath, bool import)
        {
            var loader = new EnlightCMFLoader(new TestConsole(), caseFilePath);
            loader.PreLoadPreop();
            if (import)
            {
                loader.ImportPreop();
            }
            loader.CleanUp();
        }
    }

#endif
}
