using IDS.CMF.CasePreferences;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Query
{
    public struct QcDocImplantGuideRelationshipData
    {
        public int MinIndex { get; private set; }
        public List<CasePreferenceDataModel> ImplantTypes { get; private set; }
        public List<GuidePreferenceDataModel> GuideTypes { get; private set; }

        public QcDocImplantGuideRelationshipData(int minIndex, List<CasePreferenceDataModel> implantTypes,
            List<GuidePreferenceDataModel> guideTypes)
        {
            MinIndex = minIndex;
            ImplantTypes = implantTypes;
            GuideTypes = guideTypes;
        }
    }

    public struct QcDocImplantGuideRelationshipModel
    {
        private QcDocImplantGuideRelationshipData _data;

        public QcDocImplantGuideRelationshipModel(QcDocImplantGuideRelationshipData data)
        {
            _data = data;
        }

        public int MinIndex => _data.MinIndex;
        public string ImplantTypes => string.Join("<br>", _data.ImplantTypes.Select(i => $"I{i.NCase}_{i.CasePrefData.ImplantTypeValue} - {i.CasePrefData.ScrewTypeValue}"));
        public string GuideTypes => string.Join("<br>", _data.GuideTypes.Select(g => $"G{g.NCase}_{g.GuidePrefData.GuideTypeValue} - {g.GuidePrefData.GuideScrewTypeValue}"));
    }

    public class QcDocImplantGuideRelationshipQuery
    {
        private readonly CMFImplantDirector director;

        public QcDocImplantGuideRelationshipQuery(CMFImplantDirector director)
        {
            this.director = director;
        }

        public List<QcDocImplantGuideRelationshipModel> GenerateImplantGuideRelationshipModels()
        {
            var res = new List<QcDocImplantGuideRelationshipModel>();

            var datas = GenerateImplantGuideRelationshipDatas();
            datas.ForEach(x =>
            {
                res.Add(new QcDocImplantGuideRelationshipModel(x));
            });

            return res;
        }

        public class ImplantGuideData
        {
            public CasePreferenceDataModel Implant { get; private set; }
            public GuidePreferenceDataModel Guide { get; private set; }

            public ImplantGuideData(CasePreferenceDataModel implant, GuidePreferenceDataModel guide)
            {
                Implant = implant;
                Guide = guide;
            }
        }

        public List<QcDocImplantGuideRelationshipData> GenerateImplantGuideRelationshipDatas()
        {
            var implantGuideList = new List<ImplantGuideData>();

            var implantList = director.CasePrefManager.CasePreferences.OrderBy(cp => cp.NCase);
            var guideList = director.CasePrefManager.GuidePreferences.OrderBy(gp => gp.NCase);

            var list = new List<QcDocImplantGuideRelationshipData>();

            //map by registered barrels
            foreach (var guide in guideList)
            {
                var implantNumbers = ImplantGuideLinkQuery.GetLinkedImplantNumbers(director, guide);

                if (!implantNumbers.Any())
                {
                    //add guide without linked implant
                    var data = new QcDocImplantGuideRelationshipData(guide.NCase, new List<CasePreferenceDataModel>(),
                        new List<GuidePreferenceDataModel> {guide});
                    list.Add(data);

                    continue;
                }

                foreach (var number in implantNumbers)
                {
                    var implant = implantList.First(cp => cp.NCase == number);
                    //add guide with linked implant
                    var data = new ImplantGuideData(implant, guide);

                    implantGuideList.Add(data);
                }
            }

            //no linked implants
            foreach (var implant in implantList)
            {
                if (implantGuideList.Any(ig => ig.Implant != null && ig.Implant.NCase == implant.NCase))
                {
                    continue;
                }

                //add implant without linked guide
                var data = new QcDocImplantGuideRelationshipData(implant.NCase,
                    new List<CasePreferenceDataModel> {implant}, new List<GuidePreferenceDataModel>());
                list.Add(data);
            }

            //remove duplicate implants (where a single implant in shared by multiple guides)
            var implantGroups = implantGuideList.Where(i => i.Implant != null).GroupBy(i => i.Implant.NCase);
            
            foreach (var implantGuideData in implantGroups)
            {
                var implantTypes = new List<CasePreferenceDataModel>
                    {implantGuideData.First(i => i.Implant.NCase == implantGuideData.Key).Implant};
                var guideTypes = implantGuideData.Where(i => i.Guide != null).Select(i => i.Guide).ToList();

                var data = new QcDocImplantGuideRelationshipData(implantGuideData.Key, implantTypes, guideTypes);
                list.Add(data);
            }            

            return list;
        }
    }
}
