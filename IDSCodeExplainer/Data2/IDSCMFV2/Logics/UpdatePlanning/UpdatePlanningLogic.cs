using IDS.Core.V2.Logic;
using IDS.Interface.Geometry;
using IDS.Interface.Loader;
using IDS.Interface.Logic;
using IDS.Interface.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace IDS.CMF.V2.Logics
{
    public class UpdatePlanningLogic : Logic<UpdatePlanningParameters, UpdatePlanningResults>
    {
        private readonly IConsole _console;

        public UpdatePlanningLogic(IConsole console, ILogicHelper<UpdatePlanningParameters, UpdatePlanningResults> logicHelper) : base(console, logicHelper)
        {
            _console = console;
        }

        protected override LogicStatus OnExecute(UpdatePlanningParameters parameters, out UpdatePlanningResults result)
        {
            result = new UpdatePlanningResults();
            result.Loader = parameters.Loader;

            var success = UpdatePlanning(parameters.Loader, out result.PreLoadData, out result.PreLoadPlanningTime, out result.OsteotomyHandler);

            if (!success)
            {
                return LogicStatus.Failure;
            }

            return LogicStatus.Success;
        }

        private bool UpdatePlanning(IPreopLoader loader, out List<IPreopLoadResult> preLoadData, out TimeSpan preLoadPlanningTime, out List<IOsteotomyHandler> osteotomyHandler)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            preLoadData = loader.PreLoadPreop();
            loader.GetOsteotomyHandler(out osteotomyHandler);

            stopwatch.Stop();

            preLoadPlanningTime = stopwatch.Elapsed;
            return preLoadData != null;
        }
    }
}
