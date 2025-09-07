using System.Collections.Generic;
using System.Linq;
using IDS.Glenius.ImplantBuildingBlocks;
using Rhino.Geometry;
using IDS.Glenius.Query;

namespace IDS.Glenius.Visualization
{
    public class ScrewMantleTrimmedVisualizer
    {
        private readonly List<ClippedBrepConduit> screwMantleConduits;

        public ScrewMantleTrimmedVisualizer(GleniusImplantDirector director)
        {
            var query = new HeadQueries(director);
            var clippingPlane = query.QueryCylinderHatMedialLateralPlane(false);

            var objectManager = new GleniusObjectManager(director);
            var screwMantles = objectManager.GetAllBuildingBlocks(IBB.ScrewMantle).Select(x => x as ScrewMantle).ToList();
            screwMantleConduits = new List<ClippedBrepConduit>();
            screwMantles.ForEach(
                x => screwMantleConduits.Add(new ClippedBrepConduit(x.Geometry as Brep, clippingPlane)));
        }

        public void DisplayConduit(bool visible)
        {
            screwMantleConduits.ForEach(x => x.Enabled = visible);
        }

    }
}
