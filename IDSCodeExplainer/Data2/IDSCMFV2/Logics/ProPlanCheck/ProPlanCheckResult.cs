using IDS.CMF.V2.DataModel;
using IDS.Core.V2.Logic;
using System.Collections.Generic;

namespace IDS.CMF.V2.Logics
{
    public class ProPlanCheckResult : LogicResult
    {
        public List<DisplayStringDataModel> Parts { get; set; }
    }
}
