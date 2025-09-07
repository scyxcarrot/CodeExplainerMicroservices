using IDS.Amace;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace IDS
{
    public class ScrewDatabaseQuery
    {
        private readonly XmlDocument _document;

        public ScrewDatabaseQuery()
        {
            var resource = new AmaceResources();
            _document = new XmlDocument();
            _document.Load(resource.ScrewDatabaseXmlPath);
        }

        public ScrewDatabaseQuery(string filePath)
        {
            _document = new XmlDocument();
            _document.Load(filePath);
        }

        public string GetDefaultScrewBrand()
        {
            var node = _document.SelectSingleNode("/screws/brands/defaultBrand");
            if (node == null)
            {
                throw new Exception("Default brand not found.");
            }
            return node.InnerXml;
        }

        public IEnumerable<string> GetAvailableScrewBrands()
        {
            var nodes = _document.SelectNodes("/screws/brands/brand/name");
            if (nodes == null)
            {
                throw new Exception("Available screw brands not found.");
            }
            return nodes.Cast<XmlNode>().Select(node => node.InnerXml);
        }

        public string GetDefaultScrewType(string brand)
        {
            var node = _document.SelectSingleNode($"/screws/brands/brand[name='{brand}']/defaultType");
            if (node == null)
            {
                throw new Exception($"Default screw type for brand {brand} not found.");
            }
            return node.InnerXml;
        }

        public IEnumerable<string> GetAvailableScrewTypes(string brand)
        {
            var nodes = _document.SelectNodes($"/screws/brands/brand[name='{brand}']/types/type/name");
            if (nodes == null)
            {
                throw new Exception($"Available screw types for brand {brand} not found.");
            }
            return nodes.Cast<XmlNode>().Select(node => node.InnerXml);
        }

        public IEnumerable<double> GetAvailableScrewLengths(string brand, string screwType)
        {
            var node = _document.SelectSingleNode($"/screws/brands/brand[name='{brand}']/types/type[name='{screwType}']/availableLengths");
            if (node == null)
            {
                throw new Exception($"Available screw lengths for brand {brand}, screw type {screwType} is not defined.");
            }
            var lengthsName = node.InnerXml;
            var lengthsNode = _document.SelectSingleNode($"/screws/lengths/type[name='{lengthsName}']/availableLengths");
            if (lengthsNode == null)
            {
                throw new Exception($"Available screw lengths for brand {brand}, screw type {screwType} not found.");
            }
            var lengthNodes = lengthsNode.SelectNodes("./length");
            if (lengthNodes == null)
            {
                throw new Exception("No screw length defined.");
            }
            return lengthNodes.Cast<XmlNode>().Select(lengthNode => TryParseLengthValue(lengthNode.InnerXml));
        }

        private double TryParseLengthValue(string value)
        {
            double doubleValue;
            if (!double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out doubleValue))
            {
                throw new Exception($"Screw length {value} is invalid.");
            }
            return doubleValue;
        }
    }
}
