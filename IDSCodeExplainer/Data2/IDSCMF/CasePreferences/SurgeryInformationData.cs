using IDS.CMF.DataModel;
using IDS.CMF.Utilities;
using IDS.CMF.V2.CasePreferences;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino.Collections;
using System;
using System.Windows.Controls;

namespace IDS.CMF.CasePreferences
{
    [Flags]
    public enum ELeftRight
    {
        None = 0,
        Left = 1,
        Right = 2
    }

    public class SurgeryInformationData : ISerializable<ArchivableDictionary>
    {
        public static string SerializationLabelConst => "SurgeryInformationData";
        public string SerializationLabel { get; set; }

        private const string KeyScrewBrandType = "ScrewBrandTypeKey";
        private const string KeySurgeryType = "SurgeryTypeKey";
        private const string KeySurgeryInfoSurgeryApproach = "SurgeryInfoSurgeryApproachKey";
        private const string KeyConserveMandibleWisdomTooth = "MandibleWisdomToothKey";
        private const string KeyConserveMaxillaWisdomTooth = "MaxillaWisdomToothKey";
        private const string KeyInferiorAlveolarLeft = "InferiorAlveolarLeftKey";
        private const string KeyInferiorAlveolarRight = "InferiorAlveolarRightKey";
        private const string KeyInfraorbitalLeft = "InfraorbitalLeftKey";
        private const string KeyInfraorbitalRight = "InfraorbitalRightKey";
        private const string KeyConserveNervesLeftOthers = "ConserveNervesLeftOthersKey";
        private const string KeyConserveNervesRightOthers = "ConserveNervesRightOthersKey";
        private const string KeyIsFormerRemoval = "IsFormerRemovalKey";
        private const string KeyFormerRemovalImplantMetal = "FormerRemovalImplantMetalKey";
        private const string KeyIsSawThicknessSpecified = "IsSawThicknessSpecifiedKey";
        private const string KeySawBladeThicknessValue = "SawBladeThicknessValueKey";
        private const string KeySurgeryInfoRemarks = "SurgeryInfoRemarksKey";
        private const string KeyIsSurgeryInfoComplete = "IsSurgeryInfoCompleteKey";

        public EScrewBrand ScrewBrand { get; set; }

        public ESurgeryType SurgeryType { get; set; }

        public String SurgeryInfoSurgeryApproach { get; set; }

        public String ConserveMandibleWisdomTooth { get; set; }
        public String ConserveMaxillaWisdomTooth { get; set; }

        public bool? InferiorAlveolarLeft { get; set; }
        public bool? InferiorAlveolarRight { get; set; }
        public bool? InfraorbitalLeft { get; set; }
        public bool? InfraorbitalRight { get; set; }
        public string ConserveNervesLeftOthers { get; set; }
        public string ConserveNervesRightOthers { get; set; }
        public bool? IsFormerRemoval { get; set; }
        public string FormerRemovalImplantMetal { get; set; }
        public bool? IsSawThicknessSpecified { get; set; }
        public double SawBladeThicknessValue { get; set; }
        public byte[] SurgeryInfoRemarks { get; set; }
        public bool IsSurgeryInfoComplete { get; set; }

        public SurgeryInformationData()
        {
            SerializationLabel = SerializationLabelConst;
            ScrewBrand = EScrewBrand.Synthes;
            SurgeryType = ESurgeryType.Orthognathic;
            SurgeryInfoSurgeryApproach = null;
            ConserveMandibleWisdomTooth = "N/A";
            ConserveMaxillaWisdomTooth = "N/A";
            InferiorAlveolarLeft = null;
            InferiorAlveolarRight = null;
            InfraorbitalLeft = null;
            InfraorbitalRight = null;
            ConserveNervesLeftOthers = string.Empty;
            ConserveNervesRightOthers = string.Empty;
            IsFormerRemoval = null;
            FormerRemovalImplantMetal = string.Empty;
            IsSawThicknessSpecified = null;
            SawBladeThicknessValue = 1.0;
            IsSurgeryInfoComplete = false;
            SurgeryInfoRemarks = null;
        }


        public bool Serialize(ArchivableDictionary serializer)
        {
            var success = true;

            success &= serializer.Set(CMF.Constants.Serialization.KeySerializationLabel, SerializationLabel);
            success &= serializer.SetEnumValue(KeyScrewBrandType, ScrewBrand);
            success &= serializer.SetEnumValue(KeySurgeryType, SurgeryType);
            if (SurgeryInfoSurgeryApproach != null) //Setting a null value will return false
            {
                success &= serializer.Set(KeySurgeryInfoSurgeryApproach, SurgeryInfoSurgeryApproach);
            }
            success &= serializer.Set(KeyConserveMandibleWisdomTooth, ConserveMandibleWisdomTooth);
            success &= serializer.Set(KeyConserveMaxillaWisdomTooth, ConserveMaxillaWisdomTooth);
            success &= serializer.Set(KeyInferiorAlveolarLeft, InferiorAlveolarLeft.ToString());
            success &= serializer.Set(KeyInferiorAlveolarRight, InferiorAlveolarRight.ToString());
            success &= serializer.Set(KeyInfraorbitalLeft, InfraorbitalLeft.ToString());
            success &= serializer.Set(KeyInfraorbitalRight, InfraorbitalRight.ToString());
            success &= serializer.Set(KeyConserveNervesLeftOthers, ConserveNervesLeftOthers);
            success &= serializer.Set(KeyConserveNervesRightOthers, ConserveNervesRightOthers);
            success &= serializer.Set(KeyIsFormerRemoval, IsFormerRemoval.ToString());
            success &= serializer.Set(KeyFormerRemovalImplantMetal, FormerRemovalImplantMetal);
            success &= serializer.Set(KeyIsSawThicknessSpecified, IsSawThicknessSpecified.ToString());
            success &= serializer.Set(KeySawBladeThicknessValue, SawBladeThicknessValue);
            success &= serializer.Set(KeyIsSurgeryInfoComplete, IsSurgeryInfoComplete);
            success &= serializer.Set(KeySurgeryInfoRemarks, SurgeryInfoRemarks ?? ByteUtilities.ConvertRichTextBoxToBytes(new RichTextBox()));
            return success;
        }

        #region Backward Compatibility with older cases

        public enum ERegionType
        {
            RoW = 0,
            France = 1,
            UsCanada
        }

        private EScrewBrand ConvertOldRegionTypeToScrewBrand(ERegionType region)
        {
            switch (region)
            {
                case ERegionType.France:
                    return EScrewBrand.MtlsStandardPlus;
                case ERegionType.RoW:
                    return EScrewBrand.Synthes;
                case ERegionType.UsCanada:
                    return EScrewBrand.SynthesUsCanada;
                default:
                    throw new IDSException("ConvertOldRegionTypeToScrewBrand not compatible!");
            }
        }

        private EScrewBrand DoBackwardCompatibilityForOldRegionType(ArchivableDictionary serializer)
        {
            if (serializer.ContainsKey(KeyScrewBrandType))
            {
                return serializer.GetEnumValue<EScrewBrand>(KeyScrewBrandType);
            }

            const string keyRegionType = "RegionTypeKey";

            try
            {
                return serializer.GetEnumValue<EScrewBrand>(keyRegionType);
            }
            catch (Exception)
            {
                Msai.TrackDevEvent("Old Region Type in use!", "CMF");
                return ConvertOldRegionTypeToScrewBrand(serializer.GetEnumValue<ERegionType>(keyRegionType));
            }
        }

        #endregion

        public bool DeSerialize(ArchivableDictionary serializer)
        {
            try
            {
                SerializationLabel = RhinoIOUtilities.GetStringValue(serializer, Constants.Serialization.KeySerializationLabel);

                ScrewBrand = DoBackwardCompatibilityForOldRegionType(serializer);

                SurgeryType = serializer.GetEnumValue<ESurgeryType>(KeySurgeryType);

                if (serializer.ContainsKey(KeySurgeryInfoSurgeryApproach))
                {
                    SurgeryInfoSurgeryApproach = serializer.GetString(KeySurgeryInfoSurgeryApproach);
                }
                //SurgeryInfoSurgeryApproach should remain null if it is not available

                ConserveMandibleWisdomTooth = RhinoIOUtilities.GetStringValue(serializer, KeyConserveMandibleWisdomTooth);
                ConserveMaxillaWisdomTooth = RhinoIOUtilities.GetStringValue(serializer, KeyConserveMaxillaWisdomTooth);

                InferiorAlveolarLeft = string.IsNullOrEmpty(RhinoIOUtilities.GetStringValue(serializer, KeyInferiorAlveolarLeft)) ? (bool?)null :
                    Convert.ToBoolean(RhinoIOUtilities.GetStringValue(serializer, KeyInferiorAlveolarLeft));

                InferiorAlveolarRight = string.IsNullOrEmpty(RhinoIOUtilities.GetStringValue(serializer, KeyInferiorAlveolarRight)) ? (bool?)null :
                    Convert.ToBoolean(RhinoIOUtilities.GetStringValue(serializer, KeyInferiorAlveolarRight));

                InfraorbitalLeft = string.IsNullOrEmpty(RhinoIOUtilities.GetStringValue(serializer, KeyInfraorbitalLeft)) ? (bool?)null :
                    Convert.ToBoolean(RhinoIOUtilities.GetStringValue(serializer, KeyInfraorbitalLeft));

                InfraorbitalRight = string.IsNullOrEmpty(RhinoIOUtilities.GetStringValue(serializer, KeyInfraorbitalRight)) ? (bool?)null :
                    Convert.ToBoolean(RhinoIOUtilities.GetStringValue(serializer, KeyInfraorbitalRight));

                ConserveNervesLeftOthers = RhinoIOUtilities.GetStringValue(serializer, KeyConserveNervesLeftOthers);
                ConserveNervesRightOthers = RhinoIOUtilities.GetStringValue(serializer, KeyConserveNervesRightOthers);

                IsFormerRemoval = string.IsNullOrEmpty(RhinoIOUtilities.GetStringValue(serializer, KeyIsFormerRemoval)) ? (bool?)null :
                    Convert.ToBoolean(RhinoIOUtilities.GetStringValue(serializer, KeyIsFormerRemoval));

                FormerRemovalImplantMetal = RhinoIOUtilities.GetStringValue(serializer, KeyFormerRemovalImplantMetal);

                IsSawThicknessSpecified = string.IsNullOrEmpty(RhinoIOUtilities.GetStringValue(serializer, KeyIsSawThicknessSpecified)) ? (bool?)null :
                    Convert.ToBoolean(RhinoIOUtilities.GetStringValue(serializer, KeyIsSawThicknessSpecified));

                SawBladeThicknessValue = serializer.GetDouble(KeySawBladeThicknessValue);
                SurgeryInfoRemarks = serializer.GetBytes(KeySurgeryInfoRemarks);
                IsSurgeryInfoComplete = serializer.GetBool(KeyIsSurgeryInfoComplete);
            }
            catch (Exception e)
            {
                //TODO fail the import!
                Msai.TrackException(e, "CMF");
                return false;
            }

            return true;
        }
    }
}
