using RhinoMatSDKOperations.Smooth;
using System;

namespace MatSDKOperationConsole
{
    public class SmoothConsoleHandler
    {
        private string[] CommandArguments { get; }
        public SmoothConsoleHandler(string[] args)
        {
            CommandArguments = args;
        }

        public bool Run()
        {
            if (CommandArguments.Length != 10)
            {
                return false;
            }

            var meshPath = CommandArguments[1];
            var smoothenMeshPath = CommandArguments[2];

            var smoothenAlgorithm = CommandArguments[3];
            var compensation = Convert.ToBoolean(CommandArguments[4]);
            var preserveBadEdges = Convert.ToBoolean(CommandArguments[5]);
            var preserveSharpEdges = Convert.ToBoolean(CommandArguments[6]);
            var sharpEdgeAngle = Convert.ToDouble(CommandArguments[7]);
            var smoothenFactor = Convert.ToDouble(CommandArguments[8]);
            var iterations = Convert.ToInt32(CommandArguments[9]);

            var opParams = new MDCKSmoothParameters
            { 
                SmoothenAlgorithm = smoothenAlgorithm,
                Compensation = compensation,
                PreserveBadEdges = preserveBadEdges,
                PreserveSharpEdges = preserveSharpEdges,
                SharpEdgeAngle = sharpEdgeAngle,
                SmoothenFactor = smoothenFactor,
                SmoothenIterations = iterations
            };
            
            return MDCKSmooth.OperatorSmooth(meshPath, opParams, smoothenMeshPath);
        }
    }
}
