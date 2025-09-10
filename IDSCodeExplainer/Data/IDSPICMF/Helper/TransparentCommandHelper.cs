using Rhino;
using Rhino.Input;
using Rhino.Input.Custom;

namespace IDS.PICMF.Helper
{
    public class TransparentCommandHelper
    {
        public void HandleTransparentCommands(GetBaseClass getBase)
        {
            if (getBase.Result() == GetResult.String)
            {
                var inputString = getBase.StringResult();
                if (inputString == "CMFToggleTransparency")
                {
                    RhinoApp.RunScript($"_-CMFToggleTransparency", false);
                }
            }
        }

        public void HandleGuideDrawingTransparentCommands(GetBaseClass getBase)
        {
            if (getBase.Result() == GetResult.String)
            {
                var inputString = getBase.StringResult();
                if (inputString == "ToggleGuideDrawingTransparency")
                {
                    RhinoApp.RunScript($"_-ToggleGuideDrawingTransparency", false);
                }
            }
        }
    }
}
