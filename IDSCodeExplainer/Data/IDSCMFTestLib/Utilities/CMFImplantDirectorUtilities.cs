using IDS.Core.SplashScreen;
using Rhino;

namespace IDS.CMF.TestLib.Utilities
{
    public static class CMFImplantDirectorUtilities
    {
        public static CMFImplantDirector CreateHeadlessCMFImplantDirector()
        {
            var rhinoDoc = RhinoDoc.CreateHeadless(null);
            RhinoDoc.ActiveDoc = rhinoDoc;
            var director = CreateCMFImplantDirector(rhinoDoc);
            return director;
        }

        public static CMFImplantDirector CreateCMFImplantDirector(RhinoDoc rhinoDoc)
        {
            var pluginInfo = new PluginInfoModel();
            var director = new CMFImplantDirector(rhinoDoc, pluginInfo, false);
            
            return director;
        }
    }
}
