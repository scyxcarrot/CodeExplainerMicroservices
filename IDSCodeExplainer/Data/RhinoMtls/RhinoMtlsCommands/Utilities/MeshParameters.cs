using Rhino.Geometry;

namespace RhinoMatSDKOperations.Utilities
{
    /// <summary>
    /// MeshParameters provides general meshing parameters to get nice and accurate meshes from Breps
    /// </summary>
    public class MeshParameters
    {
        /// <summary>
        /// Definition of the IDS meshing parameters
        /// </summary>
        /// <returns></returns>
        public static MeshingParameters Ids()
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
    }
}