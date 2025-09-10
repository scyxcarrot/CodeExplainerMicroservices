using Rhino.UI;

namespace IDS.Common
{
    public delegate void IDSMouseEventHandler(MouseCallbackEventArgs e);

    public class IDSMouseCallback : MouseCallback
    {
        public event IDSMouseEventHandler MouseDown;
        public event IDSMouseEventHandler MouseUp;

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
    }
}