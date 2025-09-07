using IDS.Core.Utilities;
using System.Xml;

namespace IDS.CMF.Preferences
{
    public static class CMFScrewAspectPreferences
    {
        private const string KeyStandardAngleInDegrees = "StandardAngleInDegrees";
        private const string KeyMaximumAngleInDegrees = "MaximumAngleInDegrees";

        private static ScrewAngulationParams GetScrewAngulationParameters(XmlNode nodePath)
        {
            return new ScrewAngulationParams()
            {
                StandardAngleInDegrees = XmlDocumentUtilities.ExtractDouble(nodePath, KeyStandardAngleInDegrees),
                MaximumAngleInDegrees = XmlDocumentUtilities.ExtractDouble(nodePath, KeyMaximumAngleInDegrees)
            };
        }

        public static ScrewAspectParams GetScrewAspectParameters(XmlNode nodePath)
        {
            return new ScrewAspectParams()
            {
                ScrewAngulationParams = GetScrewAngulationParameters(nodePath.SelectSingleNode("ScrewAngulation"))
            };
        }

    }
}