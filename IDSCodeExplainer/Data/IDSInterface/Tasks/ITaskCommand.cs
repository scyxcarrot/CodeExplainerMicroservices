using System;

namespace IDS.Interface.Tasks
{
    public interface ITaskCommand<in TParams, out TResult>
    {
        int EstimateConsumption { get; }

        Guid Id { get; }

        string Description { get; }

        TResult Execute(TParams parameters);
    }
}
