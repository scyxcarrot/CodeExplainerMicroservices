using RhinoMatSDKOperations.Remesh;
using System;

namespace MatSDKOperationConsole
{
    public class QualityPreservingReduceTrianglesHandler
    {
        private string[] CommandArguments { get; }
        public QualityPreservingReduceTrianglesHandler(string[] args)
        {
            CommandArguments = args;
        }

        public bool Run()
        {
            if (CommandArguments.Length != 11)
            {
                return false;
            }

            var meshPath = CommandArguments[1];
            var remeshedMeshPath = CommandArguments[2];

            var qualityThreshold = Convert.ToDouble(CommandArguments[3]);
            var maximalGeometricError = Convert.ToDouble(CommandArguments[4]);
            var checkMaximalEdgeLength = Convert.ToBoolean(CommandArguments[5]);
            var maximalEdgeLength = Convert.ToDouble(CommandArguments[6]);
            var numberOfIterations = Convert.ToInt32(CommandArguments[7]);
            var skipBadEdges = Convert.ToBoolean(CommandArguments[8]);
            var preserveSurfaceBorders = Convert.ToBoolean(CommandArguments[9]);
            var operationCount = Convert.ToInt32(CommandArguments[10]);

            var opParams = new MDCKQualityPreservingReduceTrianglesParameters
            {
                QualityThreshold = qualityThreshold,
                MaximalGeometricError = maximalGeometricError,
                CheckMaximalEdgeLength = checkMaximalEdgeLength,
                MaximalEdgeLength = maximalEdgeLength,
                NumberOfIterations = numberOfIterations,
                SkipBadEdges = skipBadEdges,
                PreserveSurfaceBorders = preserveSurfaceBorders,
                OperationCount = operationCount
            };

            return MDCKQualityPreservingReduceTriangles.OperatorRemesh(meshPath, opParams, remeshedMeshPath);
        }
    }
}
