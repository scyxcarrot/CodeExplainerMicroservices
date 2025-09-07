using Rhino;
using Rhino.Display;
using System.Linq;

namespace IDS.Core.Visualization
{
    public class View
    {
        public static bool SetViewAndDisplayMode(RhinoDoc doc, string viewName, string displayModeName)
        {
            var view = doc.Views.ToDictionary(v => v.ActiveViewport.Name, v => v)[viewName];
            if (null == view)
            {
                return false;
            }

            var desc = DisplayModeDescription.FindByName(displayModeName);
            view.ActiveViewport.DisplayMode = desc;
            return true;
        }
    }
}
