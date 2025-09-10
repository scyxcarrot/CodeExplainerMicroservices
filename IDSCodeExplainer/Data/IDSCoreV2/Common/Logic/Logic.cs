using IDS.Interface.Logic;
using IDS.Interface.Tools;
using System;

namespace IDS.Core.V2.Logic
{
    /// <summary>
    ///  Class <c>Logic</c> is a base class for all the business logic.
    /// </summary>
    /// <remarks>
    /// This class is expected to be able to run in multi-threading, so no static members allow to use in the class
    /// </remarks>
    /// <typeparam name="TParams"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    public abstract class Logic<TParams, TResult> where TParams: LogicParameters 
        where TResult : LogicResult
    {
        protected readonly IConsole console;
        protected readonly ILogicHelper<TParams, TResult> logicHelper;

        protected Logic(IConsole console, ILogicHelper<TParams, TResult> logicHelper)
        {
            this.console = console;
            this.logicHelper = logicHelper;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="result">result for custom process</param>
        /// <returns></returns>
        public LogicStatus Execute(out TResult result)
        {
            try
            {
                result = null;
                var status = logicHelper.PrepareLogicParameters(out var parameters);
                if (status != LogicStatus.Success)
                {
                    return status;
                }

                status = OnExecute(parameters, out result);
                return (status != LogicStatus.Success) ? status : logicHelper.ProcessLogicResult(result);
            }
            catch (Exception ex)
            {
                console.WriteErrorLine(ex.Message);
            }

            result = null;
            return LogicStatus.Failure;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        protected abstract LogicStatus OnExecute(TParams parameters, out TResult result);
    }
}
