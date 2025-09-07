using System;
using System.Globalization;
using System.Xml;
using System.Collections.Generic;

namespace IDS.Core.Utilities
{
    public static class XmlDocumentUtilities
    {
        public static double ExtractDouble(XmlNode nodePath, string key)
        {
            return Convert.ToDouble(nodePath.SelectSingleNode(key)?.InnerText, CultureInfo.InvariantCulture);
        }

        public static bool ExtractBoolean(XmlNode nodePath, string key)
        {
            return Convert.ToBoolean(nodePath?.SelectSingleNode(key)?.InnerText);
        }

        public static int ExtractInteger(XmlNode nodePath, string key)
        {
            return Convert.ToInt32(nodePath.SelectSingleNode(key)?.InnerText, CultureInfo.InvariantCulture);
        }

        public static List<string> ExtractValueFromXml(string xmlPath, string key)
        {
            List<string> resultList = new List<string>();
            var document = new XmlDocument();
            document.Load(xmlPath);
            var nodeList = document.GetElementsByTagName(key);

            for (var index = 0; index < nodeList.Count; index++)
            {
                resultList.Add(nodeList[index].InnerXml);
            }
           
            return resultList;
        }
    }
}