using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino;

namespace IDSPIGlenius.Commands.Shared
{
    public static class SolidWallHelper
    {
        private static Rhino.ApplicationSettings.OsnapModes _cachedOsnapModes;
        private static bool _cachedIsOsnapEnabled;

        public static void SaveCurrentOsnapSettings()
        {
            _cachedOsnapModes = Rhino.ApplicationSettings.ModelAidSettings.OsnapModes;
            _cachedIsOsnapEnabled = Rhino.ApplicationSettings.ModelAidSettings.Osnap;
        }

        public static void SetForCurveManipulation(bool saveCurrentSettings)
        {
            if (saveCurrentSettings)
            {
                SaveCurrentOsnapSettings();
            }

            SetForCurveManipulation();
        }

        public static void SetForCurveManipulation()
        {
            Rhino.ApplicationSettings.ModelAidSettings.Osnap = true;
            Rhino.ApplicationSettings.ModelAidSettings.OsnapModes =
                Rhino.ApplicationSettings.OsnapModes.Near | Rhino.ApplicationSettings.OsnapModes.Point;
        }

        public static void LoadSavedOsnapSettings()
        {
            Rhino.ApplicationSettings.ModelAidSettings.OsnapModes = _cachedOsnapModes;
            Rhino.ApplicationSettings.ModelAidSettings.Osnap = _cachedIsOsnapEnabled;
        }
    }
}
