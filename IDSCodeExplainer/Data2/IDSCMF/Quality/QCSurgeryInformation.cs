using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using System.Collections.Generic;
using System.Globalization;

namespace IDS.CMF.Quality
{
    public class QCSurgeryInformation
    {
        private readonly CMFImplantDirector _director;
        public QCSurgeryInformation(CMFImplantDirector director)
        {
            this._director = director;
        }

        public void AssignQcSurgeryInformation(ref Dictionary<string, string> valueDictionary)
        {
            var surgeryInformationData = _director.CasePrefManager.SurgeryInformation;
            valueDictionary.Add("INFO_SCREW_BRAND", GeneralUtilities.ScrewBrandEnumToDisplayString(surgeryInformationData.ScrewBrand));
            valueDictionary.Add("INFO_SURGERY_TYPE", surgeryInformationData.SurgeryType.ToString());

            valueDictionary.Add("TEETH_INTEG_IMPLANT_SUPPORT", GetImplantSupportTeethIntegrationDisplayString());
            valueDictionary.Add("REMOVED_METAL_INTEG_IMPLANT_SUPPORT", GetImplantSupportRemovedMetalIntegrationDisplayString());
            valueDictionary.Add("REMAINED_METAL_INTEG_IMPLANT_SUPPORT", GetImplantSupportRemainedMetalIntegrationDisplayString());

            valueDictionary.Add("TEETH_INTEG_GUIDE_SUPPORT", GetGuideSupportTeethIntegrationDisplayString());
            valueDictionary.Add("REMOVED_METAL_INTEG_GUIDE_SUPPORT", GetGuideSupportRemovedMetalIntegrationDisplayString());
            valueDictionary.Add("REMAINED_METAL_INTEG_GUIDE_SUPPORT", GetGuideSupportRemainedMetalIntegrationDisplayString());
        }

        private string GetImplantSupportTeethIntegrationDisplayString()
        {
            var dataModel = _director.ImplantManager.GetImplantSupportRoICreationDataModel();
            return GetSupportTeethIntegrationDisplayString(dataModel);
        }

        private string GetImplantSupportRemovedMetalIntegrationDisplayString()
        {
            var dataModel = _director.ImplantManager.GetImplantSupportRoICreationDataModel();
            return dataModel.HasMetalIntegration && HasBuildingBlock(IBB.ImplantSupportRemovedMetalIntegrationRoI) ? string.Format(CultureInfo.InvariantCulture, "{0:F1}", dataModel.ResultingOffsetForRemovedMetal) : "N/A";
        }

        private string GetImplantSupportRemainedMetalIntegrationDisplayString()
        {
            var dataModel = _director.ImplantManager.GetImplantSupportRoICreationDataModel();
            return dataModel.HasMetalIntegration && HasBuildingBlock(IBB.ImplantSupportRemainedMetalIntegrationRoI) ? string.Format(CultureInfo.InvariantCulture, "{0:F1}", dataModel.ResultingOffsetForRemainedMetal) : "N/A";
        }

        private string GetGuideSupportTeethIntegrationDisplayString()
        {
            var dataModel = _director.GuideManager.GetGuideSupportRoICreationDataModel();
            return GetSupportTeethIntegrationDisplayString(dataModel);
        }

        private string GetGuideSupportRemovedMetalIntegrationDisplayString()
        {
            //For Guide Support, Metal Integration is Removed Metal Integration
            var dataModel = _director.GuideManager.GetGuideSupportRoICreationDataModel();
            return dataModel.HasMetalIntegration ? string.Format(CultureInfo.InvariantCulture, "{0:F1}", dataModel.ResultingOffsetForMetal) : "N/A";
        }

        private string GetGuideSupportRemainedMetalIntegrationDisplayString()
        {
            //Not supported
            return "N/A";
        }

        private string GetSupportTeethIntegrationDisplayString(SupportRoICreationData dataModel)
        {
            return dataModel.HasTeethIntegration ? string.Format(CultureInfo.InvariantCulture, "{0:F1}", dataModel.ResultingOffsetForTeeth) : "N/A";
        }

        private bool HasBuildingBlock(IBB buildingBlock)
        {
            var objeckManager = new CMFObjectManager(_director);
            return objeckManager.HasBuildingBlock(buildingBlock);
        }
    }
}
