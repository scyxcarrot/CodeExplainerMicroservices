using IDS.CMF;
using IDS.CMF.Utilities;
using IDS.CMF.V2.CasePreferences;
using IDS.Core.ImplantDirector;
using IDS.Core.SplashScreen;
using Moq;
using Rhino;

namespace IDS.Testing.UnitTests
{
#if (Rhino7Installed)
    public class ImplantDirectorHelper
    {
        public RhinoDoc RhinoDoc { get; private set; }
        public IImplantDirector Director { get; private set; }

        public void Initialize()
        {
            RhinoDoc = RhinoDoc.CreateHeadless(null);
            var mockDirector = new Mock<IImplantDirector>();
            mockDirector.Setup(x => x.Document).Returns(RhinoDoc);
            Director = mockDirector.Object;
        }

        public static CMFImplantDirector CreateActualCMFImplantDirector(EScrewBrand screwBrand, ESurgeryType surgeryType)
        {
            var rhinoDoc = RhinoDoc.CreateHeadless(null);
            RhinoDoc.ActiveDoc = rhinoDoc;
            var pluginInfo = new PluginInfoModel();
            var director = new CMFImplantDirector(rhinoDoc, pluginInfo, false);
            director.CasePrefManager.SurgeryInformation.ScrewBrand = screwBrand;
            director.CasePrefManager.SurgeryInformation.SurgeryType = surgeryType;
            director.ScrewBrandCasePreferences = CasePreferencesHelper.LoadScrewBrandCasePreferencesInfo(screwBrand);
            director.ScrewLengthsPreferences = CasePreferencesHelper.LoadScrewLengthData();
            return director;
        }
    }
#endif
}
