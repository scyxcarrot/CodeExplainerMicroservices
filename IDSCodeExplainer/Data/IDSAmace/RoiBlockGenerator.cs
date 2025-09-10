using IDS.Core.Utilities;
using Rhino;
using Rhino.Geometry;
using System.Linq;

namespace IDS.Amace
{
    public class RoiBlockGenerator
    {
        private readonly Curve[] roiCurves;


        private RoiBlockGenerator()
        {
            //Just to prevent this from happening
        }

        public RoiBlockGenerator(Curve[] roiCurves)
        {
            this.roiCurves = roiCurves;
        }

        //Return null on failure
        public Brep GenerateRegionOfInterestBlock()
        {
            if (roiCurves.Any(c => !c.IsClosed || !c.IsPlanar(RhinoDoc.ActiveDoc.ModelAbsoluteTolerance)))
            {
                return null;
            }

            var roiBlock = BrepUtilities.CreatePlanarCurvesExtrude(roiCurves, 100, true, true);

            //It should not be more than one
            if (roiBlock == null || !roiBlock.Any() || roiBlock.Length > 1)
            {
                return null;
            }

            return roiBlock.FirstOrDefault();
        }

    }
}
