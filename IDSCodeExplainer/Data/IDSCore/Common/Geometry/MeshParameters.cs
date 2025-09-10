using Rhino.Geometry;

namespace IDS.Core.Utilities
{
    /*
     * MeshParameters provides general meshing parameters to get nice and accurate meshes from Breps
     */

    public static class MeshParameters
    {
        /**
         * Definition of the IDS meshing parameters
         **/

        public static MeshingParameters IDS()
        {
            var meshparameters = new MeshingParameters
            {
                ComputeCurvature = false,
                GridAmplification = 0,
                GridAspectRatio = 0,
                GridMaxCount = 0,
                GridMinCount = 16,
                JaggedSeams = false,
                MaximumEdgeLength = 5,
                MinimumEdgeLength = 0.001,
                MinimumTolerance = 0,
                RefineAngle = 0.349065850398865,
                RefineGrid = true,
                RelativeTolerance = 0,
                SimplePlanes = false,
                Tolerance = 0.01
            };

            return meshparameters;
        }

        public static MeshingParameters IDS(double minEdgeLength, double maxEdgeLength)
        {
            var meshparameters = new MeshingParameters
            {
                ComputeCurvature = false,
                GridAmplification = 0,
                GridAspectRatio = 0,
                GridMaxCount = 0,
                GridMinCount = 16,
                JaggedSeams = false,
                MaximumEdgeLength = maxEdgeLength,
                MinimumEdgeLength = minEdgeLength,
                MinimumTolerance = 0,
                RefineAngle = 0.349065850398865,
                RefineGrid = true,
                RelativeTolerance = 0,
                SimplePlanes = false,
                Tolerance = 0.01
            };

            return meshparameters;
        }

        public static MeshingParameters GetForScrewMinDistanceCheck()
        {
            var maxEd = 0.1;
            return IDS(0.001, maxEd);
        }
    }
}