using IDS.CMF.V2.DataModel;
using IDS.Core.V2.Logic;
using IDS.Interface.Logic;
using IDS.Interface.Tools;
using System.Collections.Generic;

namespace IDS.CMF.V2.Logics
{
    public class ProPlanCheckLogic : Logic<ProPlanCheckParameters, ProPlanCheckResult>
    {
        private IProPlanCheckHelper _specificLogicHelper;

        public ProPlanCheckLogic(IConsole console, IProPlanCheckHelper logicHelper) 
            : base(console, logicHelper)
        {
            _specificLogicHelper = logicHelper;
        }

        private LogicStatus ExtractPartNamesFromProPlanFile(ProPlanCheckParameters parameters, out List<string> partNames)
        {
            partNames = ProPlanPartsUtilitiesV2.ExtractStlNamesFromSppc(parameters.ProPlanFilePath, console);
            if (partNames == null)
            {
                return LogicStatus.Failure;
            }

            return LogicStatus.Success;
        }

        private LogicStatus GroupParts(List<string> partNames, out List<DisplayStringDataModel> partsWithGroupings)
        {
            partsWithGroupings = ProPlanPartsUtilitiesV2.CreateProPlanPartsGrouping(partNames);
            return LogicStatus.Success;
        }

        protected override LogicStatus OnExecute(ProPlanCheckParameters parameters, out ProPlanCheckResult result)
        {
            result = new ProPlanCheckResult();

            var status = ExtractPartNamesFromProPlanFile(parameters, out var partNames);
            if (status != LogicStatus.Success)
            {
                return status;
            }

            status = GroupParts(partNames, out var partsWithGroupings);
            if (status != LogicStatus.Success)
            {
                return status;
            }

            result.Parts = partsWithGroupings;

            return LogicStatus.Success;
        }
    }
}
