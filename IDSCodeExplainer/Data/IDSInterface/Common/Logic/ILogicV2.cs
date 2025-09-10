namespace IDS.Interface.Logic
{
    public interface ILogicV2<in TContext>
    {
        LogicStatus Execute(TContext context);
    }
}
