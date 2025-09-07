using IDS.Interface.Logic;

namespace IDS.Core.V2.Logic
{
    public interface ILogicHelper<TParams, in TResult> where TParams : LogicParameters
        where TResult : LogicResult
    {
        LogicStatus PrepareLogicParameters(out TParams param);
        LogicStatus ProcessLogicResult(TResult result);
    }
}