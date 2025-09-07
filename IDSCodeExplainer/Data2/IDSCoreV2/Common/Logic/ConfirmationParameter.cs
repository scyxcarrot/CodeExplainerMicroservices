using IDS.Interface.Logic;

namespace IDS.Core.V2.Common.Logic
{
    public class ConfirmationParameter<TParameter> : IConfirmationParameter<TParameter>
    {
        public LogicStatus Status { get; }

        public TParameter Parameter { get; }

        public ConfirmationParameter(LogicStatus status, TParameter parameter)
        {
            Status = status;
            Parameter = parameter;
        }
    }
}
