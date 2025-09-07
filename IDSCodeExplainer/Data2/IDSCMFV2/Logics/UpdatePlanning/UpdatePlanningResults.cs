using IDS.Core.V2.Logic;
using IDS.Interface.Loader;
using System;
using System.Collections.Generic;

namespace IDS.CMF.V2.Logics
{
    public class UpdatePlanningResults : LogicResult
    {
        public List<IPreopLoadResult> PreLoadData;
        public List<IOsteotomyHandler> OsteotomyHandler;
        public IPreopLoader Loader;
        public TimeSpan PreLoadPlanningTime;
        public List<string> SelectedParts;
    }
}
