using IDS.CMF.CasePreferences;
using IDS.CMF.Enumerators;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.V2.CasePreferences;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using System;
using System.Runtime.Serialization;

namespace IDS.CMF.Utilities
{
    public static class GeneralUtilities
    {
        public static string NullDisplayString(bool? parameter)
        {
            return parameter.HasValue ? parameter == true ? "Y" : "N" : "N/A";
        }

        public static string ScrewBrandEnumToDisplayString(EScrewBrand screwBrand)
        {
            switch (screwBrand)
            {
                case EScrewBrand.MtlsStandardPlus:
                    return "Materialise Standard+";
                case EScrewBrand.Synthes:
                    return "Synthes";
                case EScrewBrand.SynthesUsCanada:
                    return "Synthes (US/Canada)";
                default:
                    return null;
            }
        }

        public static string SurgeryTypeEnumToDisplayString(ESurgeryType surgeryType)
        {
            switch (surgeryType)
            {
                case ESurgeryType.Orthognathic:
                    return "Orthognathic";
                case ESurgeryType.Reconstruction:
                    return "Reconstruction";
                default:
                    throw new IDSException($"{surgeryType} is not a valid surgery type!");
            }
        }

        public static bool IsDraft(CMFImplantDirector director)
        {
            return director.documentType == DocumentType.PlanningQC ||
                   director.documentType == DocumentType.MetalQC ||
                   director.documentType == DocumentType.ApprovedQC;
        }

        public static DesignPhase GetDesignPhase(CMFImplantDirector director)
        {
            var designPhase = director.CurrentDesignPhase;
            if (IsDraft(director))
            {
                designPhase = DesignPhase.Draft;
            }

            return designPhase;
        }

        public static string CheckForTSGGuideTypeName(
            CMFImplantDirector director, 
            GuidePreferenceDataModel guidePreferenceDataModel)
        {
            var guideCaseComponent = new GuideCaseComponent();
            var objectManager = new CMFObjectManager(director);
            var teethBlockEIbb = guideCaseComponent.GetGuideBuildingBlock(
                IBB.TeethBlock, guidePreferenceDataModel);
            var guideTypeName = guidePreferenceDataModel.GuidePrefData.GuideTypeValue;
            if (objectManager.HasBuildingBlock(teethBlockEIbb))
            {
                guideTypeName += "TSG";
            }

            return guideTypeName;
        }
    }

    #region Backward_Compatibility
    public static class BackwardCompatibilityUtilities
    {

        public static string RenameScrewTypeFrom_Before_C3_dot_0(string screwType)
        {
            return screwType.StartsWith("HND") ? screwType.Replace("HND", "").Trim() : screwType;
        }

        public static string RemoveBarrelNameFromScrewType(string screwType, out string barrelType)
        {
            // we assume that it is Long barrel since its the default when changing screw types
            barrelType = "Standard";
            var outputScrewType = screwType;

            var screwTypeLowerCase = screwType.ToLower();
            if (screwTypeLowerCase.Substring(screwType.Length - 6) == "barrel")
            {
                // it can only be hex barrel or midface barrel
                if (screwTypeLowerCase.IndexOf("hex", StringComparison.Ordinal) >= 0)
                {
                    barrelType = "Short";
                    outputScrewType = screwType.Substring(0,
                        screwTypeLowerCase.IndexOf("hex", StringComparison.Ordinal) - 1);
                    
                } 
                else if (screwTypeLowerCase.IndexOf("midface", StringComparison.Ordinal) >= 0)
                {
                    barrelType = "Midface Standard";
                    outputScrewType = screwType.Substring(0,
                        screwTypeLowerCase.IndexOf("midface", StringComparison.Ordinal) - 1);
                }
                else
                {
                    throw new Exception(
                        $"Not recognized barrel type from old project. screwType = {screwTypeLowerCase}");
                }
            }

            // if not, it is probably long barrel, so do nothing
            return outputScrewType;
        }
    }
    #endregion
}
