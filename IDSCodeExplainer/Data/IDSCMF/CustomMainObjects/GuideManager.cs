using IDS.CMF.CasePreferences;
using IDS.CMF.DataModel;
using IDS.CMF.Factory;
using IDS.CMF.V2.CasePreferences;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino.Collections;

namespace IDS.CMF.ImplantBuildingBlocks
{
    public class GuideManager
    {
        private const string KeyGuideSupportRoICreationInformation = "GuideSupportRoICreationInformation";
        private const string KeyGuideSupportCreationInformation = "GuideSupportCreationInformation";

        //information of drawn roi is parked under each rhinoobject's attribute
        private GuideSupportRoICreationInformation SupportRoICreationInformation { get; set; }
        private GuideSupportCreationInformation SupportCreationInformation { get; set; }
        private CasePreferenceManager CasePreferenceManager { get; set; }

        public GuideManager(CasePreferenceManager casePreferenceManager)
        {
            SupportRoICreationInformation = new GuideSupportRoICreationInformation(); 
            SupportCreationInformation = new GuideSupportCreationInformation();
            CasePreferenceManager = casePreferenceManager;
        }

        public GuideSupportRoICreationData GetGuideSupportRoICreationDataModel()
        {
            return new GuideSupportRoICreationData
            {
                HasMetalIntegration = SupportRoICreationInformation.HasMetalIntegration,
                ResultingOffsetForMetal = SupportRoICreationInformation.ResultingOffsetForMetal,
                HasTeethIntegration = SupportRoICreationInformation.HasTeethIntegration,
                ResultingOffsetForTeeth = SupportRoICreationInformation.ResultingOffsetForTeeth,
            };
        }

        public void SetGuideSupportRoICreationInformation(GuideSupportRoICreationData dataModel)
        {
            SupportRoICreationInformation.HasMetalIntegration = dataModel.HasMetalIntegration;
            SupportRoICreationInformation.ResultingOffsetForMetal = dataModel.ResultingOffsetForMetal;
            SupportRoICreationInformation.HasTeethIntegration = dataModel.HasTeethIntegration;
            SupportRoICreationInformation.ResultingOffsetForTeeth = dataModel.ResultingOffsetForTeeth;
        }

        public void ResetGuideSupportRoICreationInformation()
        {
            SupportRoICreationInformation = new GuideSupportRoICreationInformation();
        }

        public SupportCreationDataModel GetGuideSupportCreationDataModel()
        {
            return new SupportCreationDataModel()
            {
                GapClosingDistanceForWrapRoI1 = SupportCreationInformation.GapClosingDistanceForWrapRoI1
            };
        }

        public void SetGuideSupportCreationInformation(SupportCreationDataModel dataModel)
        {
            SupportCreationInformation.GapClosingDistanceForWrapRoI1 = dataModel.GapClosingDistanceForWrapRoI1;
        }

        public bool SaveGuideInformationTo3Dm(ArchivableDictionary dict)
        {
            var savedGSR = SaveGuideSupportRoICreationInformationTo3Dm(dict); 
            var savedGS = SaveGuideSupportCreationInformationTo3Dm(dict);
            return savedGSR & savedGS;
        }

        public bool LoadGuideInformationFrom3Dm(ArchivableDictionary dict)
        {
            if (dict.ContainsKey(KeyGuideSupportRoICreationInformation))
            {
                if (!LoadGuideSupportRoICreationInformationFrom3Dm(dict))
                {
                    IDSPluginHelper.WriteLine(LogCategory.Warning, "Failed to load GuideSupportRoICreation information! Default will be used.");
                }
            }
            else
            {
                IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "No GuideSupportRoICreation information found! Default will be used.");
            }

            if (dict.ContainsKey(KeyGuideSupportCreationInformation))
            {
                if (!LoadGuideSupportCreationInformationFrom3Dm(dict))
                {
                    IDSPluginHelper.WriteLine(LogCategory.Warning, "Failed to load GuideSupportCreation information! Default will be used.");
                }
            }
            else
            {
                IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "No GuideSupportCreation information found! Default will be used.");
            }

            return true;
        }

        public static bool HandleGuideSupportRemovedMetalIntegrationRoIBackwardCompatibility(CMFImplantDirector director)
        {
            if (!director.NeedToIntroduceGuideSupportRemovedMetalIntegrationRoI)
            {
                return false;
            }

            var handled = false;

            if (director.GuideManager.GetGuideSupportRoICreationDataModel().HasMetalIntegration)
            {
                var objectManager = new CMFObjectManager(director);
                var existingGuideSupportRoI = objectManager.GetBuildingBlock(IBB.GuideSupportRoI);
                if (existingGuideSupportRoI != null)
                {
                    objectManager.DeleteObject(existingGuideSupportRoI.Id);
                }

                director.GuideManager.ResetGuideSupportRoICreationInformation();
                
                handled = true;
            }

            director.NeedToIntroduceGuideSupportRemovedMetalIntegrationRoI = false;

            return handled;
        }

        private bool SaveGuideSupportRoICreationInformationTo3Dm(ArchivableDictionary dict)
        {
            var gsrciArc = SerializationFactory.CreateSerializedArchive(SupportRoICreationInformation);
            return dict.Set(KeyGuideSupportRoICreationInformation, gsrciArc);
        }

        private bool LoadGuideSupportRoICreationInformationFrom3Dm(ArchivableDictionary dict)
        {
            var loadedData = new GuideSupportRoICreationInformation();

            if (!loadedData.DeSerialize((ArchivableDictionary)dict[KeyGuideSupportRoICreationInformation]))
            {
                return false;
            }

            SupportRoICreationInformation = loadedData;

            return true;
        }

        private bool SaveGuideSupportCreationInformationTo3Dm(ArchivableDictionary dict)
        {
            var gsciArc = SerializationFactory.CreateSerializedArchive(SupportCreationInformation);
            return dict.Set(KeyGuideSupportCreationInformation, gsciArc);
        }

        private bool LoadGuideSupportCreationInformationFrom3Dm(ArchivableDictionary dict)
        {
            var loadedData = new GuideSupportCreationInformation();

            if (!loadedData.DeSerialize((ArchivableDictionary)dict[KeyGuideSupportCreationInformation]))
            {
                return false;
            }

            SupportCreationInformation = loadedData;

            return true;
        }

        private bool CheckIsFrance() //[AH] TODO rename to IsCanHaveTeethIntegration, as France is no longer an option anymore.
        {
            return CasePreferenceManager.SurgeryInformation.ScrewBrand == EScrewBrand.MtlsStandardPlus;
        }
    }
}
