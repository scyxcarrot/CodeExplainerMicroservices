using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Collections.Immutable;

namespace IDS.CMF.Visualization
{
    public class ImplantScrewTrajectoryCylinderConduit : DisplayConduit, IDisposable
    {
        private readonly DisplayMaterial _displayMaterial = new DisplayMaterial(Colors.GeneralGrey, 0.65);
        private readonly Brep _trajectoryCylinder;

        public ImplantScrewTrajectoryCylinderConduit(Brep trajectoryCylinder)
        {
            _trajectoryCylinder = trajectoryCylinder;
        }

        protected override void PostDrawObjects(DrawEventArgs e)
        {
            base.PostDrawObjects(e);
            e.Display.DrawBrepShaded(_trajectoryCylinder, _displayMaterial);
        }

        protected override void CalculateBoundingBox(CalculateBoundingBoxEventArgs e)
        {
            base.CalculateBoundingBox(e);
            var boundingBox = _trajectoryCylinder.GetBoundingBox(false);
            if (boundingBox.IsValid)
            {
                e.IncludeBoundingBox(boundingBox);
            }
        }

        public void Dispose()
        {
            _displayMaterial.Dispose();
        }
    }
}
