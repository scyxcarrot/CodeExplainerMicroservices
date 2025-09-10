using IDS.Amace;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Core.Operations;
using IDS.Core.Visualization;
using Rhino.Geometry;

namespace IDS.Common.Operations
{
    public class AmaceAnalysisMeshMaker : AnalysisMeshMaker
    {
        /// <summary>
        /// Create a mesh difference figure between defect pelvis and design pelvis
        /// </summary>
        /// <param name="director">The director.</param>
        /// <param name="designMeshDifference">The design mesh difference.</param>
        /// <returns></returns>
        public static bool CreateOriginalPelvisDifference(ImplantDirector director, out Mesh designMeshDifference)
        {
            // Get defect and design pelvis
            var objManager = new AmaceObjectManager(director);
            Mesh preopPelvis = objManager.GetBuildingBlock(IBB.PreopPelvis).Geometry as Mesh;
            Mesh originalPelvis = objManager.GetBuildingBlock(IBB.DefectPelvis).Geometry as Mesh;

            // Perform analysis
            designMeshDifference = CreateDistanceMesh(originalPelvis, preopPelvis, 0.01, 1.0, ColorMap.MeshDifference);

            // Success
            return designMeshDifference != null;
        }

    }
}
