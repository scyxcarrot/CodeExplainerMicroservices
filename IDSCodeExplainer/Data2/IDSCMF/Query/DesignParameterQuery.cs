using IDS.CMF.CasePreferences;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.Core.Operations;
using IDS.Core.PluginHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace IDS.CMF.Query
{
    public class DesignParameterQuery
    {
        private readonly CMFImplantDirector _director;
        private readonly CMFObjectManager _objectManager;
        public List<string> ErrorMessages { get; private set; }

        public DesignParameterQuery(CMFImplantDirector director)
        {
            _director = director;
            _objectManager = new CMFObjectManager(_director);
            ErrorMessages = new List<string>();
        }

        public XmlDocument GenerateXmlDocument()
        {
            ErrorMessages = new List<string>();

            var xmlDoc = new XmlDocument();

            try
            {
                var rootNode = xmlDoc.CreateElement("designparameters");
                xmlDoc.AppendChild(rootNode);

                AddMedicalCoordinateSystemNode(xmlDoc, rootNode);

                _director.CasePrefManager.CasePreferences.ForEach(cp =>
                {
                    AddCasePreferenceNode(xmlDoc, rootNode, cp);
                });
            }
            catch (Exception e)
            {
                ErrorMessages.Add(e.Message);
                Msai.TrackException(e, "CMF");
            }

            return xmlDoc;
        }

        public void AddMedicalCoordinateSystemNode(XmlDocument xmlDoc, XmlElement rootNode)
        {
            var medCs = _director.MedicalCoordinateSystem;
            var axialPlaneNode = XmlEntitiesCreator.CreatePlaneNode2(xmlDoc, medCs.AxialPlane, "AxialPlane");
            var coronalPlaneNode = XmlEntitiesCreator.CreatePlaneNode2(xmlDoc, medCs.CoronalPlane, "CoronalPlane");
            var sagittalPlaneNode = XmlEntitiesCreator.CreatePlaneNode2(xmlDoc, medCs.SagittalPlane, "SagittalPlane");

            rootNode.AppendChild(axialPlaneNode);
            rootNode.AppendChild(coronalPlaneNode);
            rootNode.AppendChild(sagittalPlaneNode);
        }

        public int GetImplantUniqueNumber(CasePreferenceDataModel casePref)
        {
            var allAvailableByImplantType = _director.CasePrefManager.CasePreferences
                .GroupBy(x => x.CasePrefData.ImplantTypeValue).FirstOrDefault(x => x.Key == casePref.CasePrefData.ImplantTypeValue)
                .OrderBy(x => x.NCase).ToList();

            return allAvailableByImplantType.IndexOf(casePref) + 1;
        }

        private void AddCasePreferenceNode(XmlDocument xmlDoc, XmlElement rootNode, CasePreferenceDataModel casePref)
        {
            var implantNode = xmlDoc.CreateElement("implant");

            var idAttr = xmlDoc.CreateAttribute("id");
            idAttr.Value = $"{casePref.CasePrefData.ImplantTypeValue}{GetImplantUniqueNumber(casePref)}";
            implantNode.Attributes.Append(idAttr);

            var nodeType = xmlDoc.CreateElement("type");
            nodeType.InnerText = casePref.CasePrefData.ImplantTypeValue;
            implantNode.AppendChild(nodeType);

            var scrManager = new ScrewManager(_director);
            var screws = scrManager.GetScrews(casePref, false);

            var groupedScrewByArticle = screws.GroupBy(x => FindArticleNumberForImplantScrew(x)).ToList();
            foreach (var group in groupedScrewByArticle)
            {
                var nodeArticle = xmlDoc.CreateElement($"N{group.Key}");
                nodeArticle.InnerText = group.Count().ToString();
                implantNode.AppendChild(nodeArticle);
            }

            var nodeNumber = xmlDoc.CreateElement("number");
            nodeNumber.InnerText = $"{casePref.NCase}";
            implantNode.AppendChild(nodeNumber);

            var nodeImplantThickness = xmlDoc.CreateElement("thickness");
            nodeImplantThickness.InnerText = $"{string.Format(CultureInfo.InvariantCulture, "{0:F1}", casePref.CasePrefData.PlateThicknessMm)}";
            implantNode.AppendChild(nodeImplantThickness);

            screws.OrderBy(x => x.Index).ToList().ForEach(x => { AddScrewNode(xmlDoc, implantNode, x); });

            rootNode.AppendChild(implantNode);
        }

        private string FindArticleNumber(string screwTypeValue, string screwStyle, double screwLength)
        {
            string articleNumber = string.Empty;
            var dictLengths = Queries.GetAvailableScrewLengthsDictionary(screwTypeValue, screwStyle);
            foreach (var l in dictLengths)
            {
                var key = l.Key;

                if (!(Math.Abs(key - screwLength) < 0.001))
                {
                    continue;
                }

                articleNumber = l.Value;
                break;
            }

            return articleNumber;
        }

        public string FindArticleNumberForGuideScrew(GuidePreferenceDataModel guidePref, Screw screw)
        {
            var articleNumber = FindArticleNumber(
                guidePref.GuidePrefData.GuideScrewTypeValue,
                guidePref.GuidePrefData.GuideScrewStyle,
                screw.Length);

            if (articleNumber == string.Empty)
            {
                throw new IDSException($"Article number for screw {screw.Index} with length {screw.Length} is not found!");
            }

            return articleNumber;
        }

        public string FindArticleNumberForImplantScrew(Screw screw)
        {
            var casePref = _objectManager.GetCasePreference(screw);

            var articleNumber = FindArticleNumber(
                casePref.CasePrefData.ScrewTypeValue,
                casePref.CasePrefData.ScrewStyle,
                screw.Length);

            if (articleNumber == string.Empty)
            {
                throw new IDSException($"Article number for screw {screw.Index} with length {screw.Length} is not found!");
            }

            return articleNumber;
        }

        private void AddScrewNode(XmlDocument xmlDoc, XmlElement implantNode, Screw screw)
        {
            var screwNode = xmlDoc.CreateElement("screw");

            var idAttr = xmlDoc.CreateAttribute("id");
            idAttr.Value = $"{screw.Index}";
            screwNode.Attributes.Append(idAttr);

            var casePref = _objectManager.GetCasePreference(screw);

            //Screw Type
            var screwType = casePref.CasePrefData.ScrewTypeValue;
            var nodeType = xmlDoc.CreateElement("type");
            nodeType.InnerText = Queries.GetScrewTypeForDesignParameter(screwType);
            screwNode.AppendChild(nodeType);

            //Screw Length
            var nodeLength = xmlDoc.CreateElement("length");
            nodeLength.InnerText = string.Format(CultureInfo.InvariantCulture, "{0:F1}", screw.Length);
            screwNode.AppendChild(nodeLength);

            //Screw Diamter
            var diameter = Queries.GetScrewDiameter(casePref.CasePrefData.ScrewTypeValue);
            var nodeDiameter = xmlDoc.CreateElement("diameter");
            nodeDiameter.InnerText = string.Format(CultureInfo.InvariantCulture, "{0:F2}", diameter);
            screwNode.AppendChild(nodeDiameter);

            //Screw Position
            var nodePosition = xmlDoc.CreateElement("position");
            nodePosition.InnerText = screw.GetPositionOnPlannedBone();
            screwNode.AppendChild(nodePosition);

            //Screw Article Number
            string articleNumber = FindArticleNumberForImplantScrew(screw);

            var nodeArticle = xmlDoc.CreateElement("articlenumber");
            nodeArticle.InnerText = articleNumber;
            screwNode.AppendChild(nodeArticle);

            implantNode.AppendChild(screwNode);
        }
    }
}