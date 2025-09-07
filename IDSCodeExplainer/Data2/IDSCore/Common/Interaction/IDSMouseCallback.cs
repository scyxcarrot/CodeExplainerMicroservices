using Rhino.UI;

namespace IDS.Core.Drawing
{
    public delegate void IDSMouseCallbackEventHandler(MouseCallbackEventArgs e);
    public delegate void IDSMouseRhinoViewEventHandler(MouseCallbackEventArgs e);

    public class IDSMouseCallback : MouseCallback
    {
        public event IDSMouseCallbackEventHandler MouseDown;
        public event IDSMouseCallbackEventHandler MouseUp;
        public event IDSMouseRhinoViewEventHandler MouseEnter;
        public event IDSMouseRhinoViewEventHandler MouseLeave;

        protected override void OnMouseDown(MouseCallbackEventArgs e)
        {
            base.OnMouseDown(e);
            MouseDown?.Invoke(e);
        }

        protected override void OnMouseUp(MouseCallbackEventArgs e)
        {
            base.OnMouseUp(e);
            MouseUp?.Invoke(e);
        }

        protected override void OnMouseEnter(MouseCallbackEventArgs e)
        {
            base.OnMouseEnter(e);
            MouseEnter?.Invoke(e);
        }

        protected override void OnMouseLeave(MouseCallbackEventArgs e)
        {
            base.OnMouseLeave(e);
            MouseLeave?.Invoke(e);
        }
    }
}