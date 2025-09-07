using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using System;

namespace IDS.CMF.Utilities
{
    public class BuildingBlockRestorer
    {
        private readonly CMFImplantDirector _director;

        public BuildingBlockRestorer(CMFImplantDirector director)
        {
            this._director = director;
        }

        public ExtendedImplantBuildingBlock GetExtendedBuildingBlock(IBB block, string blockName)
        {
            ExtendedImplantBuildingBlock eblock = null;

            switch (block)
            {
                case IBB.ProPlanImport:
                    eblock = GetProPlanImportExtendedBuildingBlock(blockName);
                    break;
                case IBB.GuideFixationScrew:
                case IBB.GuideFixationScrewEye:
                case IBB.GuideFlange:
                case IBB.GuideBridge:
                case IBB.GuidePreviewSmoothen:
                case IBB.ActualGuide:
                case IBB.GuideBaseWithLightweight:
                case IBB.GuideFixationScrewLabelTag:
                case IBB.GuideSurface:
                case IBB.PositiveGuideDrawings:
                case IBB.NegativeGuideDrawing:
                case IBB.GuideLinkSurface:
                case IBB.GuideSolidSurface:
                case IBB.SmoothGuideBaseSurface:
                case IBB.ActualGuideImprintSubtractEntity:
                case IBB.GuideScrewIndentationSubtractEntity:
                case IBB.TeethBlock:
                case IBB.TeethBaseRegion:
                case IBB.TeethBaseExtrusion:
                    eblock = GetGuideExtendedBuildingBlock(block, blockName);
                    break;
                default:
                    eblock = GetImplantCaseExtendedBuildingBlock(block, blockName);
                    break;
            }

            if (eblock == null)
            {
                throw new Exception($"Unable to get building block: {block} with name: {blockName}!");
            }

            return eblock;
        }

        private ExtendedImplantBuildingBlock GetProPlanImportExtendedBuildingBlock(string blockName)
        {
            var proPlanImportComponent = new ProPlanImportComponent();
            var partName = proPlanImportComponent.GetPartName(blockName);
            return proPlanImportComponent.GetProPlanImportBuildingBlock(partName);
        }

        private ExtendedImplantBuildingBlock GetGuideExtendedBuildingBlock(IBB block, string blockName)
        {
            var guideCaseComponent = new GuideCaseComponent();
            Guid caseGuid;
            if (!guideCaseComponent.TryGetCaseGuid(blockName, out caseGuid))
            {
                return null;
            }
            var casePreferenceData = _director.CasePrefManager.GetGuideCase(caseGuid);
            return guideCaseComponent.GetGuideBuildingBlock(block, casePreferenceData);
        }

        private ExtendedImplantBuildingBlock GetImplantCaseExtendedBuildingBlock(IBB block, string blockName)
        {
            var implantCaseComponent = new ImplantCaseComponent();
            Guid caseGuid;
            if (!implantCaseComponent.TryGetCaseGuid(blockName, out caseGuid))
            {
                return null;
            }
            var casePreferenceData = _director.CasePrefManager.GetCase(caseGuid);
            return implantCaseComponent.GetImplantBuildingBlock(block, casePreferenceData);
        }
    }
}
