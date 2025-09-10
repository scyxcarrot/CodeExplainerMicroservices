using IDS.CMF.DataModel;
using IDS.CMF.Factory;
using IDS.CMF.Graph;
using IDS.Core.Utilities;
using Rhino.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace IDS.CMF.CasePreferences
{
    public class GuidePreferenceDataModel : ICaseData, ISerializable<ArchivableDictionary>
    {
        public string SerializationLabel => "GuidePreferenceDataModel";

        private readonly string KeyCaseGuid = "CaseGuid";
        private readonly string KeyPositiveSurfaces = "PositiveSurfaces";
        private readonly string KeyNegativeSurfaces = "NegativeSurfaces";
        private readonly string KeyLinkSurfaces = "LinkSurfaces";
        private readonly string KeySolidSurfaces = "SolidSurfaces";
        private readonly string KeyNCase = "NCase";
        private readonly string KeyCaseName = "CaseName";
        private readonly string KeyLinkedRegisteredBarrels = "LinkedRegisteredBarrels";
        private readonly string KeyLinkedImplantScrews = "LinkedImplantScrews";

        //Guid should be the same as the one in guide preference panel.
        public Guid CaseGuid { get; protected set; }

        public List<PatchData> PositiveSurfaces { get; set; }
        public List<PatchData> NegativeSurfaces { get; set; }
        public List<PatchData> LinkSurfaces { get; set; }
        public List<PatchData> SolidSurfaces { get; set; }

        public List<Guid> LinkedImplantScrews { get; set; }

        public int NCase { get; set; }
        public string CaseName { get; set; }

        public virtual void SetCaseNumber(int number)
        {

        }

        public CMFGraph Graph { get; set; }
        
        private ScrewAideDataModel _guideScrewAideData;
        public ScrewAideDataModel GuideScrewAideData
        {
            get
            {
                if (_guideScrewAideData == null)
                {
                    _guideScrewAideData = new ScrewAideDataModel(GuidePrefData.GuideScrewTypeValue);
                    _guideScrewAideData.Update();
                }
                return _guideScrewAideData;
            }
            protected set
            {
                _guideScrewAideData = value;
            }
        }

        public GuidePreferenceData GuidePrefData { get; set; }

        public GuidePreferenceDataModel(Guid guid)
        {
            CaseGuid = guid;
            PositiveSurfaces = new List<PatchData>();
            NegativeSurfaces = new List<PatchData>();
            LinkSurfaces = new List<PatchData>();
            SolidSurfaces = new List<PatchData>();
            LinkedImplantScrews = new List<Guid>();
        }

        public void InvalidateGraph(CMFImplantDirector director)
        {
            if (Graph != null)
            {
                Graph.UnsubscribeForGraphInvalidation();
                Graph = null;
            }

            Graph = new CMFGraph(director, this);
            Graph.SubscribeForGraphInvalidation();
        }

        public void UpdateGuideFixationScrewAide()
        {
            //screw aide brep might be null when load existing file because import command been blocked at that moment.
            GuideScrewAideData = new ScrewAideDataModel(GuidePrefData.GuideScrewTypeValue);
            GuideScrewAideData.Update();
        }

        public bool Serialize(ArchivableDictionary serializer)
        {
            serializer.Set(KeyCaseGuid, CaseGuid);

            var counter = 0;
            foreach (var positiveSurface in PositiveSurfaces)
            {
                var dict = SerializationFactory.CreateSerializedArchive(positiveSurface);
                serializer.Set(KeyPositiveSurfaces + $"_{counter}", dict);
                counter++;
            }

            counter = 0;

            foreach (var negativeSurface in NegativeSurfaces)
            {
                var dict = SerializationFactory.CreateSerializedArchive(negativeSurface);
                serializer.Set(KeyNegativeSurfaces + $"_{counter}", dict);
                counter++;
            }

            counter = 0;

            foreach (var linkSurface in LinkSurfaces)
            {
                var dict = SerializationFactory.CreateSerializedArchive(linkSurface);
                serializer.Set(KeyLinkSurfaces + $"_{counter}", dict);
                counter++;
            }

            counter = 0;

            foreach (var solidSurface in SolidSurfaces)
            {
                var dict = SerializationFactory.CreateSerializedArchive(solidSurface);
                serializer.Set(KeySolidSurfaces + $"_{counter}", dict);
                counter++;
            }

            serializer.Set(KeyNCase, NCase);
            serializer.Set(KeyCaseName, CaseName);
            if (GuidePrefData != null)
            {
                GuidePrefData.Serialize(serializer);
            }

            serializer.Set(KeyLinkedImplantScrews, LinkedImplantScrews);

            return true;
        }

        public bool DeSerialize(ArchivableDictionary serializer)
        {
            PositiveSurfaces = new List<PatchData>();
            NegativeSurfaces = new List<PatchData>();
            LinkSurfaces = new List<PatchData>();
            SolidSurfaces = new List<PatchData>();
            CaseGuid = serializer.GetGuid(KeyCaseGuid);

            foreach (var d in serializer)
            {
                var patchData = new PatchData();

                if (Regex.IsMatch(d.Key, KeyPositiveSurfaces + "_\\d+"))
                {
                    patchData.DeSerialize(serializer.GetDictionary(d.Key));
                    PositiveSurfaces.Add(patchData);
                }

                if (Regex.IsMatch(d.Key, KeyNegativeSurfaces + "_\\d+"))
                {
                    patchData.DeSerialize(serializer.GetDictionary(d.Key));
                    NegativeSurfaces.Add(patchData);
                }

                if (Regex.IsMatch(d.Key, KeyLinkSurfaces + "_\\d+"))
                {
                    patchData.DeSerialize(serializer.GetDictionary(d.Key));
                    LinkSurfaces.Add(patchData);
                }

                if (Regex.IsMatch(d.Key, KeySolidSurfaces + "_\\d+"))
                {
                    patchData.DeSerialize(serializer.GetDictionary(d.Key));
                    SolidSurfaces.Add(patchData);
                }
            }

            NCase = serializer.GetInteger(KeyNCase);
            CaseName = RhinoIOUtilities.GetStringValue(serializer, KeyCaseName);

            GuidePrefData = new GuidePreferenceData();
            GuidePrefData.DeSerialize(serializer);

            if (serializer.ContainsKey(KeyLinkedImplantScrews))
            {
                LinkedImplantScrews = ((Guid[])serializer[KeyLinkedImplantScrews]).ToList();
            }
            else if (serializer.ContainsKey(KeyLinkedRegisteredBarrels))
            {
                //backward compatibility
                LinkedImplantScrews = ((Guid[])serializer[KeyLinkedRegisteredBarrels]).ToList();
            }

            return true;
        }
    }
}
