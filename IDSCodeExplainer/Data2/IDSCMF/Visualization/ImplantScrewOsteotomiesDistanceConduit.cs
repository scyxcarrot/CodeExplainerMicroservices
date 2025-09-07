using System;
using Rhino.Display;
using Rhino.Geometry;
using System.Drawing;

namespace IDS.CMF.Visualization
{
    public class ImplantScrewOsteotomiesDistanceConduit: DisplayConduit, IDisposable
    {
        private readonly LinearDimension _linearDimension;

        public ImplantScrewOsteotomiesDistanceConduit(LinearDimension linearDimension)
        {
            _linearDimension = linearDimension;
        }

        protected override void PostDrawObjects(DrawEventArgs e)
        {
            base.PostDrawObjects(e);
            e.Display.DrawAnnotation(_linearDimension, Color.BlueViolet);
        }

        protected override void CalculateBoundingBox(CalculateBoundingBoxEventArgs e)
        {
            base.CalculateBoundingBox(e);
            var boundingBox = _linearDimension.GetBoundingBox(false);
            if (boundingBox.IsValid)
            {
                e.IncludeBoundingBox(boundingBox);
            }
        }

        public void Dispose()
        {
            _linearDimension?.Dispose();
        }
    }
}
