using IDS.Amace.ImplantBuildingBlocks;
using Rhino.Geometry;
using System;

namespace IDS
{
    public class ScrewGuideCreator
    {
        private const double SphereRadius = 6.0;
        private const int TopCylinderRadius = 6;
        private const int TopCylinderHeight = 60;

        /// <summary>
        /// Gets the guide hole boolean for individual screw
        /// </summary>
        /// <returns></returns>
        public Brep GetGuideHoleBoolean(Screw screw, double drillBitRadius)
        {
            var screwAideManager = new ScrewAideManager(screw, screw.Director.ScrewDatabase);

            var bottomCylinderHeightFromSphereContour = Math.Round(screwAideManager.GetGuideCylinderHeight(screw.positioning),1) - 0.6;

            var guideHoleBoolean = screwAideManager.GetGuideHoleBooleanBrep(SphereRadius, drillBitRadius,
                TopCylinderRadius, TopCylinderHeight, bottomCylinderHeightFromSphereContour);
            return guideHoleBoolean;
        }

        public Brep GetGuideHoleBooleanSphere(Screw screw)
        {
            var screwAideManager = new ScrewAideManager(screw, screw.Director.ScrewDatabase);

            var guideHoleBoolean = screwAideManager.GetGuideHoleBooleanSphereBrep(SphereRadius);
            return guideHoleBoolean;
        }

        /// <summary>
        /// Gets the guide hole safety zone for individual screw
        /// </summary>
        /// <returns></returns>
        public Brep GetGuideHoleSafetyZone(Screw screw, double drillBitRadius)
        {
            var screwAideManager = new ScrewAideManager(screw, screw.Director.ScrewDatabase);
            return screwAideManager.GetGuideHoleSafetyZone(drillBitRadius);
        }
    }
}
