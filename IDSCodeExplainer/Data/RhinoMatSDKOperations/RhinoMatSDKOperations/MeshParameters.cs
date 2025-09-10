using Rhino.Geometry;

namespace RhinoMatSDKOperations.Meshes
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
        public static MeshingParameters IDS()
        {
            MeshingParameters meshparameters = new MeshingParameters();
            meshparameters.ComputeCurvature = false;
            meshparameters.GridAmplification = 0;
            meshparameters.GridAspectRatio = 0;
            meshparameters.GridMaxCount = 0;
            meshparameters.GridMinCount = 16;
            meshparameters.JaggedSeams = false;
            meshparameters.MaximumEdgeLength = 5;
            meshparameters.MinimumEdgeLength = 0.001;
            meshparameters.MinimumTolerance = 0;
            meshparameters.RefineAngle = 0.349065850398865;
            meshparameters.RefineGrid = true;
            meshparameters.RelativeTolerance = 0;
            meshparameters.SimplePlanes = false;
            meshparameters.Tolerance = 0.01;

            return meshparameters;
        }
    }
}