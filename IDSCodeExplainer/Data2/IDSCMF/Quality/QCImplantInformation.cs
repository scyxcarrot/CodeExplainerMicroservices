using IDS.CMF.FileSystem;
using IDS.CMF.Query;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Quality;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace IDS.CMF.Quality
{
    public class QCImplantInformation
    {
        private readonly CMFImplantDirector _director;
        public List<string> ImplantNameList { get; private set; }
        public List<QCImplantImplantSection.ImageData> ImplantFrontImagesBase64JpegByteString { get; set; } = new List<QCImplantImplantSection.ImageData>();

        public QCImplantInformation(CMFImplantDirector director)
        {
            this._director = director;
        }

        public string FillInImplantQC(ref Dictionary<string, string> valueDictionary, DocumentType docType, QcDocBoneThicknessMapQuery boneThicknessMapQuery)
        {
            ImplantFrontImagesBase64JpegByteString = new List<QCImplantImplantSection.ImageData>();

            var cmfResources = new CMFResources();
            var implantListName = new List<string>();
            var implantListSub = new List<string>();
            var implantDynamicList = new List<string>();
            var implantImplantSect = new QCImplantImplantSection(_director);

            ImplantNameList = new List<string>();
            var implantHTML = new StringBuilder();
            var dynamicHtml = File.ReadAllText(cmfResources.qcDocumentImplantDynamicScriptFile);
            var casePreferencesList = _director.CasePrefManager.CasePreferences.OrderBy(caseP => caseP.NCase).ToList();
            var numbCase = casePreferencesList.Count;
            for (var i = 0; i < numbCase; i++)
            {
                var casePref = casePreferencesList[i];
                var implantDict = new Dictionary<string, string>();

                var implantType = casePref.CasePrefData.ImplantTypeValue;

                var implantName = casePref.CaseName;
                var implantSub = casePref.CaseName + "_sub";
                var implantSubDynamic = "dynamic_" + implantSub;
                implantListName.Add(implantName);
                implantListSub.Add(implantSub);
                implantDynamicList.Add(implantSubDynamic);
                implantDict.Add("IMPLANT_NAME", implantName);
                implantDict.Add("IMPLANT_TYPE", casePref.CasePrefData.ImplantTypeValue);
                implantDict.Add("DYNAMIC_IMPLANT", implantSubDynamic);
                implantDict.Add("DYNAMIC_IMPLANT_SUB", implantSub);
                
                string implantHtml = string.Copy(dynamicHtml);

                var timerComponent = new Stopwatch();
                timerComponent.Start();
                var timeRecorded = new Dictionary<string, string>();
                //planning
                QCImplantPlanningSection.ImplantPlanningInfo(ref implantDict, casePref);

                timerComponent.Stop();
                timeRecorded.Add($"FillInImplantQC-ImplantPlanningInfo {implantType}", $"{ (timerComponent.ElapsedMilliseconds * 0.001)}");
                timerComponent.Restart();

                //implant
                implantImplantSect.ImplantImplantInfo(ref implantDict, casePref, docType, boneThicknessMapQuery);
                ImplantFrontImagesBase64JpegByteString.AddRange(implantImplantSect.ImplantFrontImagesBase64JpegByteString);

                timerComponent.Stop();
                timeRecorded.Add($"FillInImplantQC-ImplantImplantInfo {implantType}", $"{ (timerComponent.ElapsedMilliseconds * 0.001)}");
                Msai.TrackDevEvent($"QCDoc Implant Info Section-FillInImplantQC {implantType}", "CMF", timeRecorded);
                Msai.PublishToAzure();

                var implantInfo = QCReportUtilities.FormatFromDictionary(implantHtml, implantDict);
                implantHTML.Append(implantInfo);
            }

            var dropdownList = GenerateDropdownItem(implantListName, implantListSub, implantDynamicList);
            valueDictionary.Add("DYNAMIC_IMPLANT_DROPDOWN", dropdownList);
            return implantHTML.ToString();
        }

        private string GenerateDropdownItem(List<string> implantNames, List<string> implantSubs, List<string> implantDynamic)
        {
            var dropdownListItem = new StringBuilder();
            for (var i = 0; i < implantNames.Count; i++)
            {
                dropdownListItem.Append("<a class=\"dropdown-item\" href=\"javascript: expand(\'" + implantDynamic[i] + "', '" + implantSubs[i] + "')\">" +
                    implantNames[i] + "</a>");
            }

            return dropdownListItem.ToString();
        }
    }
}
