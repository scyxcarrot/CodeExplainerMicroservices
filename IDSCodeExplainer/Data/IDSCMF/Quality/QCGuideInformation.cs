using IDS.CMF.FileSystem;
using IDS.Core.Enumerators;
using IDS.Core.Quality;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace IDS.CMF.Quality
{
    public class QCGuideInformation
    {
        private readonly CMFImplantDirector _director;
        public List<string> GuideNameList { get; private set; }

        public QCGuideInformation(CMFImplantDirector director)
        {
            this._director = director;
        }

        public string FillInGuideQC(ref Dictionary<string, string> valueDictionary, DocumentType docType)
        {
            var cmfResources = new CMFResources();
            var guideListName = new List<string>();
            var guideListSub = new List<string>();
            var guideDynamicList = new List<string>();
            var guideSect = new QCGuideSection(_director);

            GuideNameList = new List<string>();
            var guideHTML = new StringBuilder();
            var dynamicHtml = File.ReadAllText(cmfResources.qcDocumentGuideDynamicScriptFile);
            var guidePreferencesList = _director.CasePrefManager.GuidePreferences.OrderBy(guideP => guideP.NCase).ToList();
            var numbCase = guidePreferencesList.Count;
            for (int i = 0; i < numbCase; i++)
            {
                var guidePref = guidePreferencesList[i];
                var guideDict = new Dictionary<string, string>();

                var guideName = guidePref.CaseName;
                var guideSub = guidePref.CaseName + "_sub";
                var guideSubDynamic = "dynamic_" + guideSub;
                guideListName.Add(guideName);
                guideListSub.Add(guideSub);
                guideDynamicList.Add(guideSubDynamic);
                guideDict.Add("GUIDE_NAME", guideName);
                guideDict.Add("DYNAMIC_GUIDE", guideSubDynamic);
                guideDict.Add("DYNAMIC_GUIDE_SUB", guideSub);
                
                var guideHtml = string.Copy(dynamicHtml);

                guideSect.FillGuideInfo(ref guideDict, guidePref, docType);

                var guideInfo = QCReportUtilities.FormatFromDictionary(guideHtml, guideDict);
                guideHTML.Append(guideInfo);
            }

            var dropdownList = GenerateDropdownItem(guideListName, guideListSub, guideDynamicList);
            valueDictionary.Add("DYNAMIC_GUIDE_DROPDOWN", dropdownList);
            return guideHTML.ToString();
        }

        private string GenerateDropdownItem(List<string> guideNames, List<string> guideSubs, List<string> guideDynamic)
        {
            var dropdownListItem = new StringBuilder();
            for (int i = 0; i < guideNames.Count; i++)
            {
                dropdownListItem.Append("<a class=\"dropdown-item\" href=\"javascript: expand(\'" + guideDynamic[i] + "', '" + guideSubs[i] + "')\">" +
                                        guideNames[i] + "</a>");
            }

            return dropdownListItem.ToString();
        }
    }
}
