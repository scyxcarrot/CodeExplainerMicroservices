using IDS.Core.Utilities;
using System.Xml;

namespace IDS.CMF.Preferences
{
    public static class CMFAutoImplantPreferences
    {
        private const string KeyGenioWiderDistance = "GenioWiderDistance";
        private const string KeyGenioWideDistance = "GenioWideDistance";
        private const string KeyGenioNarrowDistance = "GenioNarrowDistance";
        private const string KeyMandibleWiderDistance = "MandibleWiderDistance";
        private const string KeyMandibleWideDistance = "MandibleWideDistance";
        private const string KeyMandibleNarrowDistance = "MandibleNarrowDistance";
        
        public static GenioAutoImplantParams GetGenioAutoImplantParameters(XmlNode nodePath)
        {
            var parameter = new GenioAutoImplantParams()
            {
                GenioWiderDistance = XmlDocumentUtilities.ExtractDouble(
                    nodePath, KeyGenioWiderDistance),
                GenioWideDistance = XmlDocumentUtilities.ExtractDouble(
                    nodePath, KeyGenioWideDistance),
                GenioNarrowDistance = XmlDocumentUtilities.ExtractDouble(
                    nodePath, KeyGenioNarrowDistance),
                MandibleWiderDistance = XmlDocumentUtilities.ExtractDouble(
                    nodePath, KeyMandibleWiderDistance),
                MandibleWideDistance = XmlDocumentUtilities.ExtractDouble(
                    nodePath, KeyMandibleWideDistance),
                MandibleNarrowDistance = XmlDocumentUtilities.ExtractDouble(
                    nodePath, KeyMandibleNarrowDistance),
            };
            return parameter;
        }
    }
}
