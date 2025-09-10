using IDS.Core.Utilities;
using Rhino.Display;
using Rhino.Geometry;

namespace IDS.Glenius.Visualization
{
    public class ClippedBrepConduit : DisplayConduit
    {
        private readonly Brep _trimmedBrep;

        public ClippedBrepConduit(Brep brep, Plane clippingPlane)
        {
            _trimmedBrep = BrepUtilities.Trim(brep.DuplicateBrep(), clippingPlane, false); 
        }

        protected override void PreDrawObjects(DrawEventArgs e)
        {
            base.PreDrawObjects(e);

            if (_trimmedBrep != null)//because it can be null if no trimming possible
            {
                e.Display.DrawBrepShaded(_trimmedBrep, new DisplayMaterial(Colors.Metal, 0));
            }
        }

    }
}
