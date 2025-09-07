using IDS.CMF.CasePreferences;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.Core.ImplantBuildingBlocks;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IDS.CMF.GuideBuildingBlocks
{
    public class GuideCaseComponent
    {
        public ExtendedImplantBuildingBlock GetGuideBuildingBlock(IBB block, ICaseData data)
        {
            AssertBlockIsNotGuideComponent(block);
            var staticBlock = BuildingBlocks.Blocks[block];
            return new ExtendedImplantBuildingBlock
            {
                Block = new ImplantBuildingBlock
                {
                    ID = staticBlock.ID,
                    Name = GetGuideBuildingBlockName(block, data),
                    GeometryType = staticBlock.GeometryType,
                    Layer = string.Format(staticBlock.Layer, $"Guide {data.NCase}"),
                    Color = GetColor(block, data)
                },
                PartOf = block
            };
        }

        public List<ExtendedImplantBuildingBlock> GetGuideBuildingBlockList(ICaseData data)
        {
            var blocks = new List<ExtendedImplantBuildingBlock>();
            var allComponent = GetGuideComponents();
            foreach (var component in allComponent)
            {
                var guideBuildingBlock = GetGuideBuildingBlock(component, data);
                blocks.Add(guideBuildingBlock);
            }
            return blocks;
        }

        public void AssertBlockIsNotGuideComponent(IBB block)
        {
            if (!IsGuideComponent(block))
            {
                throw new Exception($"{block} is not an guide component!");
            }
        }

        private bool IsGuideComponent(IBB block)
        {
            var components = GetGuideComponents();
            return components.Contains(block);
        }

        /// <summary>
        /// Get all IBB guide components
        /// </summary>
        /// <returns>return a list of IBB enums that is related to guide</returns>
        public IEnumerable<IBB> GetGuideComponents()
        {
            // as of REQ1118759, the coarse guide preview (IBB.GuidePreview) is removed because we dont generate it anymore
            return new List<IBB>
            {
                IBB.GuideFixationScrew,
                IBB.GuideFixationScrewEye,
                IBB.GuideFlange,
                IBB.GuideBridge,
                IBB.GuideFixationScrewLabelTag,
                IBB.PositiveGuideDrawings,
                IBB.NegativeGuideDrawing,
                IBB.GuideSurface,
                IBB.GuideLinkSurface,
                IBB.GuideSolidSurface,
                IBB.GuidePreviewSmoothen,
                IBB.ActualGuide,
                IBB.GuideBaseWithLightweight,
                IBB.SmoothGuideBaseSurface,
                IBB.ActualGuideImprintSubtractEntity,
                IBB.GuideScrewIndentationSubtractEntity,
                IBB.TeethBlock,
                IBB.TeethBaseRegion,
                IBB.TeethBaseExtrusion
            };
        }

        public string GetGuideBuildingBlockName(IBB block, ICaseData data)
        {
            var staticBlock = BuildingBlocks.Blocks[block];
            return string.Format(staticBlock.Name, data.CaseGuid);
        }

        public bool TryGetCaseGuid(string blockName, out Guid caseGuid)
        {
            var extract = blockName.Split('_').Last();
            return Guid.TryParse(extract, out caseGuid);
        }

        public Color GetColor(IBB block, ICaseData data)
        {
            var staticBlock = BuildingBlocks.Blocks[block];
            var color = staticBlock.Color;
            switch (block)
            {
                case IBB.GuidePreviewSmoothen:
                case IBB.ActualGuide:
                case IBB.GuideBaseWithLightweight:
                case IBB.GuideFixationScrew:
                case IBB.GuideFlange:
                case IBB.GuideBridge:
                case IBB.GuideSurface:
                case IBB.GuideFixationScrewLabelTag:
                case IBB.GuideFixationScrewEye:
                case IBB.TeethBlock:
                    color = CasePreferencesHelper.GetColor(data.NCase);
                    break;
            }
            return color;
        }
    }
}
