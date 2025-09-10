using System;
using RhinoMatSDKOperations.Smooth;

namespace MatSDKOperationConsole
{
    public class SmoothEdgeConsoleHandler
    {
        private string[] CommandArguments { get; }
        public SmoothEdgeConsoleHandler(string[] args)
        {
            CommandArguments = args;
        }

        public bool Run()
        {
            if (CommandArguments.Length < 12 || CommandArguments.Length > 13)
            {
                return false;
            }

            var topMeshPath = CommandArguments[1];
            var sideMeshPath = CommandArguments[2];
            var bottomMeshPath = CommandArguments[3];
            var roundedMeshPath = CommandArguments[4];

            var topEdgeRadius = Convert.ToDouble(CommandArguments[5]);
            var bottomEdgeRadius = Convert.ToDouble(CommandArguments[6]);
            var topMinEdgeLength = Convert.ToDouble(CommandArguments[7]);
            var topMaxEdgeLength = Convert.ToDouble(CommandArguments[8]);
            var bottomMinEdgeLength = Convert.ToDouble(CommandArguments[9]);
            var bottomMaxEdgeLength = Convert.ToDouble(CommandArguments[10]);
            var iterations = Convert.ToInt32(CommandArguments[11]);

            var opParams = new MDCKSmoothImplantBorderParameters
            (
                topEdgeRadius,
                bottomEdgeRadius,
                topMinEdgeLength,
                topMaxEdgeLength,
                bottomMinEdgeLength,
                bottomMaxEdgeLength,
                iterations
            );

            //OperatorSmoothEdge(string topMesh, string sideMesh, string bottomMesh, out string rounded, MDCKSmoothImplantBorderParameters opParams)
            if (CommandArguments.Length == 12)
            {
                if (MDCKSmoothImplantBorder.OperatorSmoothEdge(topMeshPath, sideMeshPath, bottomMeshPath,
                    roundedMeshPath, opParams))
                {
                    return true;
                }
            }
            else
            {
                var applyFixes = Convert.ToBoolean(CommandArguments[12]);

                if (MDCKSmoothImplantBorder.OperatorSmoothEdge(topMeshPath, sideMeshPath, bottomMeshPath,
                    roundedMeshPath, opParams, applyFixes))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
