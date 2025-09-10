namespace IDS.PICMF.Drawing
{
    interface IUiImplantManipulator : IImplantManipulator
    {
        void SetState(EDrawImplantState state);
        bool Draw();
    }
}
