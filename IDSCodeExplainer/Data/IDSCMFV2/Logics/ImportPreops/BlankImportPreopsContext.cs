using IDS.CMF.V2.CasePreferences;
using IDS.Core.V2.Common.Logic;
using IDS.Core.V2.ExternalTools;
using IDS.Interface.Geometry;
using IDS.Interface.Loader;
using IDS.Interface.Logic;
using IDS.Interface.Tools;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.V2.Logics
{
    public class BlankImportPreopsContext
    {
        #region Parameters
        protected readonly IConsole console;

        public MsaiTrackingInfo TrackingInfo { get; }

        public virtual string FilePath { get; }

        public virtual ConfirmationParameter<ScrewBrandSurgeryParameter> ConfirmationScrewBrandSurgery { get; }

        public IPlane SagittalPlane { get; set; }

        public IPlane AxialPlane { get; set; }

        public IPlane CoronalPlane { get; set; }

        public IPlane MidSagittalPlane { get; set; }
        #endregion

        public BlankImportPreopsContext(IConsole console, string defaultFilePath, 
            EScrewBrand defaultScrewBrand, ESurgeryType defaultSurgeryType)
        {
            this.console = console;
            TrackingInfo = new MsaiTrackingInfo(console);
            FilePath = defaultFilePath;
            ConfirmationScrewBrandSurgery = new ConfirmationParameter<ScrewBrandSurgeryParameter>(
                LogicStatus.Success, new ScrewBrandSurgeryParameter(defaultScrewBrand, defaultSurgeryType));
        }

        #region Virtual Function
        public virtual void ShowErrorMessage(string errorTitle, string errorMessage)
        {
            console.WriteErrorLine($"{errorTitle}: {errorMessage}");
        }

        public virtual void UpdateScrewBrandSurgery(EScrewBrand screwBrand, ESurgeryType surgeryType)
        {
        }

        public virtual bool AskConfirmationToProceed(List<IPreopLoadResult> preLoadData)
        {
            return true;
        }

        public virtual void AddProPlanParts(List<IPreopLoadResult> preopData)
        {
        }

        public virtual LogicStatus PostProcessData()
        {
            return LogicStatus.Success;
        }

        public virtual void DuplicateOriginalToPlannedPart(string partName)
        {
            
        }

        public virtual void AddOsteotomyHandlerToBuildingBlock(
            List<IOsteotomyHandler> osteotomyHandler)
        {

        }
        #endregion

        protected List<IPreopLoadResult> FilterPreopData(List<IPreopLoadResult> preopData)
        {
            var filteredPreopData = preopData.ToList();
            FilterPreopData(ref filteredPreopData, "00SKU_comp", "00SKU");
            FilterPreopData(ref filteredPreopData, "00MAN_comp", "00MAN");
            return filteredPreopData;
        }

        private void FilterPreopData(ref List<IPreopLoadResult> preopData, string partToKeep, string partToRemove)
        {
            //filter out partToRemove if partToKeep exist
            if (preopData.Any(item => item.Name.ToLower() == partToKeep.ToLower()) &&
                preopData.Any(item => item.Name.ToLower() == partToRemove.ToLower()))
            {
                preopData.RemoveAll(item => item.Name.ToLower() == partToRemove.ToLower());
            }
        }
    }
}
