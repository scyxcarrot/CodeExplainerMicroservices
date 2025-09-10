using IDS.CMF.V2.Preferences;
using IDS.Core.Utilities;
using System.Xml;

namespace IDS.CMF.Preferences
{
    public static class CMFImplantSupportPreferences
    {
        private const string KeyRoIConnectionRadius = "RoIConnectionRadius";
        private const string KeyRoIPastilleRadius = "RoIPastilleRadius";
        private const string KeyRoILandmarkRadius = "RoILandmarkRadius";

        public static ImplantSupportParams GetImplantSupportParameters(XmlNode nodePath)
        {
            return new ImplantSupportParams()
            {
                RoIConnectionRadius = XmlDocumentUtilities.ExtractDouble(nodePath, KeyRoIConnectionRadius),
                RoIPastilleRadius = XmlDocumentUtilities.ExtractDouble(nodePath, KeyRoIPastilleRadius),
                RoILandmarkRadius = XmlDocumentUtilities.ExtractDouble(nodePath, KeyRoILandmarkRadius)
            };
        }
    }
}
