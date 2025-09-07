using IDS.CMF.DataModel;
using Rhino.Input.Custom;
using System.Drawing;

namespace IDS.PICMF.Operations
{
    public interface ICurveGetter
    {
        void OnPreGetting(ref GetPoint getPoints, Color conduitColor);

        void OnCancel();

        void OnPointPicked(ref GetPoint getPoints);

        void OnUndo(ref GetPoint getPoints);

        bool OnFinalized(out ImplantTransitionInputCurveDataModel outputDataModel);

        void OnMouseMove(object sender, GetPointMouseEventArgs e);
    }
}
