namespace IDS.PICMF.Drawing
{
    public interface IUndoableSurfaceAction
    {
        bool Do(DrawSurfaceUndoData data);

        bool Undo(DrawSurfaceUndoData data);
    }
}
