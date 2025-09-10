using IDS.PICMF.Drawing;

namespace IDS.PICMF.DrawingAction
{
    public interface IUndoableAction
    {
        bool Do(DrawImplantBaseState state);

        bool Undo(DrawImplantBaseState state);
    }
}
