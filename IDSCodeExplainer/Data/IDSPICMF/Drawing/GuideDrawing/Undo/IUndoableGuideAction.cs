namespace IDS.PICMF.DrawingAction
{
    public interface IUndoableGuideAction
    {
        bool Do(DrawGuideUndoData data);

        bool Undo(DrawGuideUndoData data);
    }
}
