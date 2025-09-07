namespace IDS.Interface.Logic
{
    public interface IConfirmationParameter<TParameter>
    {
        LogicStatus Status { get; }

        TParameter Parameter { get; }
    }
}
