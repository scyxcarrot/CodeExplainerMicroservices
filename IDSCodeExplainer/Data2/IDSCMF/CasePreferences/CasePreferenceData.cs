using IDS.CMF.DataModel;
using IDS.CMF.Query;
using IDS.CMF.Utilities;
using IDS.Core.Utilities;
using Rhino.Collections;
using System.Globalization;
using System.Windows.Controls;

namespace IDS.CMF.CasePreferences
{
    public class CasePreferenceData : ISerializable<ArchivableDictionary>
    {
        public const string naString = "N/A";
        public const string wIDefaultsString = "WI defaults";
        public const string seeCommentsString = "See Comments";
        public string SerializationLabel => "CasePreferenceData";
       
        private readonly string KeySurgicalApproach = "SurgicalApproach";
        private readonly string KeyNumberOfImplants = "NumberOfImplants";
        private readonly string KeyNumberOfGuides = "NumberOfGuides";
        private readonly string KeySelectedImplantType = "SelectedImplantType";
        private readonly string KeyPlateThicknessMm = "PlateThicknessMm";
        private readonly string KeyPlateWidthMm = "PlateWidthMm";
        private readonly string KeyLinkWidthMm = "LinkWidthMm";
        private readonly string KeySelectedScrewType = "SelectedScrewType";
        private readonly string KeySelectedScrewLengthMm = "SelectedScrewLengthMm";
        private readonly string KeyPastilleDiameter = "PastilleDiameter";

        private readonly string KeyScrewFixationType = "ScrewFixationType";
        private readonly string KeyScrewFixationSkullRemainingType = "ScrewFixationSkullRemainingType";
        private readonly string KeyScrewFixationSkullGraftType = "ScrewFixationSkullGraftType";
        private readonly string KeyCaseInfoRemarks = "CaseInfoRemarksType";
             
        private readonly string KeySkullRemainingScrewPerFixationPoint = "SkullRemainingScrewPerFixationPoint";
        private readonly string KeyMaxillaScrewPerFixationPoint = "MaxillaScrewPerFixationPoint";
        private readonly string KeyPlateClearanceinCriticalJunction = "PlateClearanceinCriticalJunction";

        private readonly string KeySelectedScrewStyle = "SelectedScrewStyle";
        private readonly string KeySelectedBarrelType = "SelectedBarrelType";

        public string SurgicalApproach { get; set; }
        public int NumberOfImplants { get; set; }
        public int NumberOfGuides { get; set; }

        public string ImplantTypeValue { get; set; }
        public double PlateThicknessMm { get; set; }
        public double PlateWidthMm { get; set; }
        public double LinkWidthMm { get; set; }

        public string ScrewTypeValue { get; set; }
        public double PastilleDiameter { get; set; }
        public double ScrewLengthMm { get; set; }
        public string ScrewFixationTypeValue { get; set; }
        public string ScrewFixationSkullRemainingTypeValue { get; set; }
        public string ScrewFixationSkullGraftTypeValue { get; set; }

        //ONLY USE FOR LOADIND/SAVING
        public bool IsScrewFixationTypeValueNA { get; set; }
        public bool IsScrewFixationSkullRemainingTypeValueNA { get; set; }
        public bool IsScrewFixationSkullGraftTypeValueNA { get; set; }
        public bool IsScrewLengthValueNA { get;  set; }
        public bool IsScrewLengthValueSeeComments { get; set; }
        public bool NeedsScrewTypeBackwardsCompatibility { get; set; }
        
        public byte[] CaseInfoRemarks { get; set; }        
        
        public string SkullRemainingScrewPerFixationPoint { get; set; }
        public string MaxillaScrewPerFixationPoint { get; set; }
        public string PlateClearanceinCriticalJunction { get; set; }
        
        public string ScrewStyle { get; set; }
        public string BarrelTypeValue { get; set; }

        public CasePreferenceData()
        {
            SurgicalApproach = null;
            NumberOfImplants = 0;
            NumberOfGuides = 0;
            ImplantTypeValue = null;
            PlateThicknessMm = 0.00;
            PlateWidthMm = 0.00;
            LinkWidthMm = 0.00;
            ScrewTypeValue = null;
            PastilleDiameter = 0.00;

            NeedsScrewTypeBackwardsCompatibility = false;
            IsScrewLengthValueNA = false;
            IsScrewLengthValueSeeComments = false;
            ScrewLengthMm = 0.00;
            IsScrewFixationTypeValueNA = false;
            ScrewFixationTypeValue = null;
            IsScrewFixationSkullRemainingTypeValueNA = false;
            ScrewFixationSkullRemainingTypeValue = null;
            IsScrewFixationSkullGraftTypeValueNA = false;
            ScrewFixationSkullGraftTypeValue = null;
            
            CaseInfoRemarks = null;

            SkullRemainingScrewPerFixationPoint = "2 Linear";
            MaxillaScrewPerFixationPoint = "2";
            PlateClearanceinCriticalJunction = naString;

            ScrewStyle = null;
            BarrelTypeValue = null;
        }

        public bool Serialize(ArchivableDictionary serializer)
        {          
            serializer.Set(KeySurgicalApproach, SurgicalApproach);
            serializer.Set(KeyNumberOfImplants, NumberOfImplants);
            serializer.Set(KeyNumberOfGuides, NumberOfGuides);
            serializer.Set(KeySelectedImplantType, ImplantTypeValue);
            serializer.Set(KeyPlateThicknessMm, PlateThicknessMm);
            serializer.Set(KeyPlateWidthMm, PlateWidthMm);
            serializer.Set(KeyLinkWidthMm, LinkWidthMm);
            serializer.Set(KeySelectedScrewType, ScrewTypeValue);
            serializer.Set(KeyPastilleDiameter, PastilleDiameter);

            serializer.Set(KeySelectedScrewLengthMm, GetSelectedScrewLengthMm(false));
            serializer.Set(KeyScrewFixationType, (IsScrewFixationTypeValueNA) ? naString : ScrewFixationTypeValue);
            serializer.Set(KeyScrewFixationSkullRemainingType, (IsScrewFixationSkullRemainingTypeValueNA) ? naString : ScrewFixationSkullRemainingTypeValue);
            serializer.Set(KeyScrewFixationSkullGraftType, (IsScrewFixationSkullGraftTypeValueNA) ? naString : ScrewFixationSkullGraftTypeValue);
            
            serializer.Set(KeyCaseInfoRemarks, CaseInfoRemarks ?? ByteUtilities.ConvertRichTextBoxToBytes(new RichTextBox()));

            serializer.Set(KeySkullRemainingScrewPerFixationPoint, SkullRemainingScrewPerFixationPoint);
            serializer.Set(KeyMaxillaScrewPerFixationPoint, MaxillaScrewPerFixationPoint);
            serializer.Set(KeyPlateClearanceinCriticalJunction, PlateClearanceinCriticalJunction);

            serializer.Set(KeySelectedScrewStyle, ScrewStyle);
            serializer.Set(KeySelectedBarrelType, BarrelTypeValue);

            return true;
        }

        public bool DeSerialize(ArchivableDictionary serializer)
        {            
            SurgicalApproach = RhinoIOUtilities.GetStringValue(serializer, KeySurgicalApproach);
            NumberOfImplants = serializer.GetInteger(KeyNumberOfImplants);
            NumberOfGuides = serializer.GetInteger(KeyNumberOfGuides);
            ImplantTypeValue = RhinoIOUtilities.GetStringValue(serializer,KeySelectedImplantType);

#if (INTERNAL)
            //TODO: Remove before release
            //temporary fixes for existing file due to changes to implant type name
            ImplantTypeValue = ImplantTypeValue.Replace("Lefort1", "Lefort");
            ImplantTypeValue = ImplantTypeValue.Replace("BSSO Single", "BSSOSingle");
            ImplantTypeValue = ImplantTypeValue.Replace("BSSO Double", "BSSODouble");
            ImplantTypeValue = ImplantTypeValue.Replace("Short Osteotomy", "ShortOsteotomy");
            ImplantTypeValue = ImplantTypeValue.Replace("Small Mandible", "MandibleSmall");
            ImplantTypeValue = ImplantTypeValue.Replace("Large Mandible", "MandibleLarge");
#endif

            PlateThicknessMm = serializer.GetDouble(KeyPlateThicknessMm);
            PlateWidthMm = serializer.GetDouble(KeyPlateWidthMm);
            LinkWidthMm = serializer.GetDouble(KeyLinkWidthMm);
            ScrewTypeValue = RhinoIOUtilities.GetStringValue(serializer, KeySelectedScrewType);

#region Backward_Compatibility
            ScrewTypeValue = BackwardCompatibilityUtilities.RenameScrewTypeFrom_Before_C3_dot_0(ScrewTypeValue);
            ScrewTypeValue =
                BackwardCompatibilityUtilities.RemoveBarrelNameFromScrewType(ScrewTypeValue, out var barrelTypeValue);
            BarrelTypeValue = RhinoIOUtilities.GetStringValue(serializer, KeySelectedBarrelType);
            if (BarrelTypeValue == string.Empty)
            {
                BarrelTypeValue = barrelTypeValue;
            }
#endregion

            PastilleDiameter = serializer.GetDouble(KeyPastilleDiameter);
            if (RhinoIOUtilities.GetStringValue(serializer, KeySelectedScrewLengthMm) == naString || RhinoIOUtilities.GetStringValue(serializer, KeySelectedScrewLengthMm) == wIDefaultsString)
            {
                IsScrewLengthValueNA = true;
                ScrewLengthMm = 0.0;
            }
            else if (RhinoIOUtilities.GetStringValue(serializer, KeySelectedScrewLengthMm) == seeCommentsString)
            {
                IsScrewLengthValueSeeComments = true;
                ScrewLengthMm = 0.0;
            }
            else
            {
                IsScrewLengthValueNA = false;
                IsScrewLengthValueSeeComments = false;
                ScrewLengthMm = double.Parse(RhinoIOUtilities.GetStringValue(serializer, KeySelectedScrewLengthMm), CultureInfo.InvariantCulture);
            }

            if (RhinoIOUtilities.GetStringValue(serializer, KeyScrewFixationType) == naString)
            {
                IsScrewFixationTypeValueNA = true;
                ScrewFixationTypeValue = string.Empty;
            }
            else
            {
                IsScrewFixationTypeValueNA = false;
                ScrewFixationTypeValue = RhinoIOUtilities.GetStringValue(serializer, KeyScrewFixationType);
            }

            if (RhinoIOUtilities.GetStringValue(serializer, KeyScrewFixationSkullRemainingType) == naString)
            {
                IsScrewFixationSkullRemainingTypeValueNA = true;
                ScrewFixationSkullRemainingTypeValue = string.Empty;
            }
            else
            {
                IsScrewFixationSkullRemainingTypeValueNA = false;
                ScrewFixationSkullRemainingTypeValue = RhinoIOUtilities.GetStringValue(serializer, KeyScrewFixationSkullRemainingType);
            }

            if (RhinoIOUtilities.GetStringValue(serializer, KeyScrewFixationSkullGraftType) == naString)
            {
                IsScrewFixationSkullGraftTypeValueNA = true;
                ScrewFixationSkullGraftTypeValue = string.Empty;
            }
            else
            {
                IsScrewFixationSkullGraftTypeValueNA = false;
                ScrewFixationSkullGraftTypeValue = RhinoIOUtilities.GetStringValue(serializer, KeyScrewFixationSkullGraftType);
            }
            
            CaseInfoRemarks = serializer.GetBytes(KeyCaseInfoRemarks);  

            if (serializer.ContainsKey(KeySkullRemainingScrewPerFixationPoint))
            {
                SkullRemainingScrewPerFixationPoint = RhinoIOUtilities.GetStringValue(serializer, KeySkullRemainingScrewPerFixationPoint);
            }

            if (serializer.ContainsKey(KeyMaxillaScrewPerFixationPoint))
            {
                MaxillaScrewPerFixationPoint = RhinoIOUtilities.GetStringValue(serializer, KeyMaxillaScrewPerFixationPoint);
            }

            if (serializer.ContainsKey(KeyPlateClearanceinCriticalJunction))
            {
                PlateClearanceinCriticalJunction = RhinoIOUtilities.GetStringValue(serializer, KeyPlateClearanceinCriticalJunction);
            }

            ScrewStyle = RhinoIOUtilities.GetStringValue(serializer, KeySelectedScrewStyle);
            #region Backward_Compatibility
            if (string.IsNullOrEmpty(ScrewStyle))
            {
                ScrewStyle = Queries.GetDefaultScrewStyleName(ScrewTypeValue);
            }
            #endregion

            return true;
        }

        public string GetSelectedScrewLengthMm(bool forceDecimals)
        {
            return GetSelectedScrewLengthMm(1, forceDecimals);
        }

        public string GetSelectedScrewLengthMm(int precision, bool forceDecimals)
        {
            return (IsScrewLengthValueNA) ? wIDefaultsString : (IsScrewLengthValueSeeComments) ? seeCommentsString : StringUtilities.DoFormat(ScrewLengthMm, precision, forceDecimals);
        }
    }
}
