using IDS.Amace.ImplantBuildingBlocks;
using IDS.Core.Fea;
using Rhino.Geometry;
using System.IO;

namespace IDS.Amace.Operations
{
    public static class FeaMaker
    {
        /// <summary>
        /// Does the fea.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <param name="boundaryConditionNoiseShellThreshold"></param>
        /// <param name="feaDirectory">The fea directory.</param>
        /// <param name="material"></param>
        /// <param name="targetEdgeLength"></param>
        /// <param name="loadMagnitude"></param>
        /// <param name="loadMeshDegreesThreshold"></param>
        /// <param name="boundaryConditionDistanceThreshold"></param>
        /// <returns></returns>
        public static Fea.AmaceFea DoFea(ImplantDirector director, Material material, double targetEdgeLength,
            double loadMagnitude, double loadMeshDegreesThreshold, double boundaryConditionDistanceThreshold,
            double boundaryConditionNoiseShellThreshold, string feaDirectory)
        {
            var fea = SetUpFea(director, material, targetEdgeLength, loadMagnitude, loadMeshDegreesThreshold,
                boundaryConditionDistanceThreshold, boundaryConditionNoiseShellThreshold, feaDirectory);
            var succesfulSimulation = fea.PerformFea();
            return succesfulSimulation ? fea : null;
        }

        ///// <summary>
        ///// Creates the inp simulation.
        ///// </summary>
        ///// <param name="director">The director.</param>
        ///// <param name="feaDirectory">The fea directory.</param>
        ///// <returns></returns>
        //public static Amace.Fea.AmaceFea CreateInpSimulation(ImplantDirector director, Material material, double targetEdgeLength, double loadMagnitude, double loadMeshDegreesThreshold, double boundaryConditionDistanceThreshold, double boundaryConditionNoiseShellThreshold, string feaDirectory)
        //{
        //    Amace.Fea.AmaceFea fea = SetUpFea(director, material, targetEdgeLength, loadMagnitude, loadMeshDegreesThreshold, boundaryConditionDistanceThreshold, boundaryConditionNoiseShellThreshold, feaDirectory);
        //    fea.PrepareFEA();
        //    return fea;
        //}

        /// <summary>
        /// Sets up fea.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <param name="boundaryConditionsNoiseShellThreshold"></param>
        /// <param name="feaDirectory">The fea directory.</param>
        /// <param name="material"></param>
        /// <param name="targetEdgeLength"></param>
        /// <param name="loadMagnitude"></param>
        /// <param name="loadMeshDegreesThreshold"></param>
        /// <param name="boundaryConditionsDistanceThreshold"></param>
        /// <returns></returns>
        private static Fea.AmaceFea SetUpFea(ImplantDirector director, Material material,
            double targetEdgeLength, double loadMagnitude, double loadMeshDegreesThreshold,
            double boundaryConditionsDistanceThreshold, double boundaryConditionsNoiseShellThreshold, string feaDirectory)
        {
            Directory.CreateDirectory(feaDirectory);
            var objectManager = new AmaceObjectManager(director);

            var implantBottomMesh = (Mesh)objectManager.GetBuildingBlock(IBB.SolidPlateBottom).Geometry;
            var reamedPelvis = (Mesh)objectManager.GetBuildingBlock(IBB.OriginalReamedPelvis).Geometry;
            var implantMesh = PlateWithTransitionForExportCreator.CreateForImplantQc(director);

            var fea = new Amace.Fea.AmaceFea(implantMesh, implantBottomMesh, reamedPelvis, director.cup, material, targetEdgeLength, BoundaryConditionsType.DistanceThreshold, LoadVectorType.FDAConstruct, loadMagnitude, loadMeshDegreesThreshold, boundaryConditionsDistanceThreshold, boundaryConditionsNoiseShellThreshold, feaDirectory);
            return fea;
        }
    }
}
