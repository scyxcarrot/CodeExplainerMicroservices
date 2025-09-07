using Rhino;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IDS.CMF.Utilities
{
    public static class ConduitUtilities
    {
        public static void RefeshConduit()
        {
            //Refresh Conduit Workaround
            RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.
                SetCameraLocations(RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraTarget,
                    RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraLocation);
            RhinoDoc.ActiveDoc.Views.ActiveView.Redraw();
        }
    }
}
