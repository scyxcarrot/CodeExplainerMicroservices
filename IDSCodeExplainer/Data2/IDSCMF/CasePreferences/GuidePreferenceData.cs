using IDS.CMF.DataModel;
using IDS.CMF.Query;
using IDS.CMF.Utilities;
using IDS.Core.Utilities;
using Rhino.Collections;
using System.Windows.Controls;

namespace IDS.CMF.CasePreferences
{
    public class GuidePreferenceData : ISerializable<ArchivableDictionary>
    {
        public string SerializationLabel => "GuidePreferenceData";
       
        private readonly string KeySelectedGuideType = "SelectedGuideType";
        private readonly string KeyGuideFlange = "GuideFlangeType";
        private readonly string KeyGuideScrewType = "GuideScrewType";
        private readonly string KeyGuideCutSlotType = "GuideCutSlotType";
        private readonly string KeyGuideConnectionsType = "GuideConnectionsType";
        private readonly string KeyGuideInfoRemarks = "GuideInfoRemarksType";
        private readonly string KeyGuideScrewStyle = "GuideScrewStyle";

        public string GuideTypeValue { get; set; }
        public bool GuideFlange { get; set; }
        public string GuideScrewTypeValue { get; set; }
        public string GuideCutSlotValue { get; set; }
        public string GuideConnectionsValue { get; set; }
        public byte[] GuideInfoRemarks { get; set; }
        public string GuideScrewStyle { get; set; }
        public bool NeedsScrewTypeBackwardsCompatibility { get; set; }

        public GuidePreferenceData()
        {
            GuideTypeValue = null;                  
            GuideFlange = false;
            GuideScrewTypeValue = null;
            GuideCutSlotValue = null ;
            GuideConnectionsValue = null;
            GuideInfoRemarks = null;
            GuideScrewStyle = null;
            NeedsScrewTypeBackwardsCompatibility = false;
        }

        public bool Serialize(ArchivableDictionary serializer)
        {                     
            serializer.Set(KeySelectedGuideType, GuideTypeValue);           
            serializer.Set(KeyGuideFlange, GuideFlange);
            serializer.Set(KeyGuideScrewType, GuideScrewTypeValue);
            serializer.Set(KeyGuideCutSlotType, GuideCutSlotValue);
            serializer.Set(KeyGuideConnectionsType, GuideConnectionsValue);
            serializer.Set(KeyGuideInfoRemarks, GuideInfoRemarks ?? ByteUtilities.ConvertRichTextBoxToBytes(new RichTextBox()));
            serializer.Set(KeyGuideScrewStyle, GuideScrewStyle);
            return true;
        }

        public bool DeSerialize(ArchivableDictionary serializer)
        {            
            GuideTypeValue = RhinoIOUtilities.GetStringValue(serializer,KeySelectedGuideType);
            GuideFlange = serializer.GetBool(KeyGuideFlange);
            GuideScrewTypeValue = RhinoIOUtilities.GetStringValue(serializer, KeyGuideScrewType);

            #region Backward_Compatibility

            GuideScrewTypeValue = BackwardCompatibilityUtilities.RenameScrewTypeFrom_Before_C3_dot_0(GuideScrewTypeValue);

            #endregion

            GuideCutSlotValue = RhinoIOUtilities.GetStringValue(serializer, KeyGuideCutSlotType);
            GuideConnectionsValue = RhinoIOUtilities.GetStringValue(serializer, KeyGuideConnectionsType);
            GuideInfoRemarks = serializer.GetBytes(KeyGuideInfoRemarks);
            GuideScrewStyle = RhinoIOUtilities.GetStringValue(serializer, KeyGuideScrewStyle);

            #region Backward_Compatibility

            if (string.IsNullOrEmpty(GuideScrewStyle))
            {
                GuideScrewStyle = Queries.GetDefaultScrewStyleName(GuideScrewTypeValue);
            }

            #endregion

            return true;
        }
    }
}
