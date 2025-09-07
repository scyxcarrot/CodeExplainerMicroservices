using IDS.Core.V2.Logic;

namespace IDS.CMF.V2.Logics
{
    public interface IImportBaseHelper<TParams, in TResult> :
        ILogicHelper<TParams, TResult>
        where TParams : LogicParameters
        where TResult : LogicResult
    {
    }
}