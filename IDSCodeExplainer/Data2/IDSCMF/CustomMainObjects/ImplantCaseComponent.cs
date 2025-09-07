using IDS.CMF.CasePreferences;
using IDS.CMF.Utilities;
using IDS.Core.ImplantBuildingBlocks;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IDS.CMF.ImplantBuildingBlocks
{
    public class ImplantCaseComponent
    {
        public ExtendedImplantBuildingBlock GetImplantBuildingBlock(IBB block, ICaseData data)
        {
            AssertBlockIsNotImplantComponent(block);

            var staticBlock = BuildingBlocks.Blocks[block];
            return new ExtendedImplantBuildingBlock
            {
                Block = new ImplantBuildingBlock
                {
                    ID = staticBlock.ID,
                    Name = GetImplantBuildingBlockName(block, data),
                    GeometryType = staticBlock.GeometryType,
                    Layer = string.Format(staticBlock.Layer, data.CaseName),
                    Color = GetColor(block, data)
                },
                PartOf = block
            };
        }

        public string GetImplantBuildingBlockName(IBB block, ICaseData data)
        {
            var staticBlock = BuildingBlocks.Blocks[block];
            return string.Format(staticBlock.Name, data.CaseGuid);
        }

        public bool TryGetCaseGuid(string blockName, out Guid caseGuid)
        {
            var extract = blockName.Split('_').Last();
            return Guid.TryParse(extract, out caseGuid);
        }

        public void AssertBlockIsNotImplantComponent(IBB block)
        {
            if (!IsImplantComponent(block))
            {
                throw new Exception($"{block} is not an implant component!");
            }
        }

        public bool IsImplantComponent(IBB block)
        {
            var components = GetImplantComponents();
            return components.Contains(block);
        }

        public IEnumerable<IBB> GetImplantComponents()
        {
            return new List<IBB>
            {
                IBB.PlanningImplant,
                IBB.ImplantPreview,
                IBB.Connection,
                IBB.Screw,
                IBB.Landmark,
                IBB.RegisteredBarrel,
                IBB.ActualImplant,
                IBB.ActualImplantWithoutStampSubtraction,
                IBB.ActualImplantSurfaces,
                IBB.PastillePreview,
                IBB.ActualImplantImprintSubtractEntity,
                IBB.ImplantScrewIndentationSubtractEntity,
                IBB.ImplantSupport,
                IBB.ConnectionPreview,
                IBB.PatchSupport
            };
        }

        private Color GetColor(IBB block, ICaseData data)
        {
            var staticBlock = BuildingBlocks.Blocks[block];
            var color = staticBlock.Color;
            switch (block)
            {
                case IBB.ImplantPreview:
                case IBB.ActualImplant:
                case IBB.ActualImplantWithoutStampSubtraction:
                case IBB.ActualImplantSurfaces:
                case IBB.PastillePreview:
                case IBB.ConnectionPreview:
                    color = CasePreferencesHelper.GetColor(data.NCase);
                    break;
            }
            return color;
        }
    }
}
