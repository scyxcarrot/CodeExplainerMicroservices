using Materialise.SDK.MDCK.Model.Enums;
using Materialise.SDK.MDCK.Model.Objects;
using Materialise.SDK.MDCK.Operators;
using RhinoMatSDKOperations.IO;
using System;

namespace RhinoMatSDKOperations.Remesh
{
    //UIOperationXPQPReduceTriangles.cpp
    public class MDCKQualityPreservingReduceTriangles
    {
        public static bool OperatorRemesh(string meshPath, MDCKQualityPreservingReduceTrianglesParameters opParams, string remeshedPath)
        {
            // Import the STL file
            Model model;
            var success = MDCKInputOutput.ModelFromStlPath(meshPath, out model);
            if (!success)
            {
                return false;
            }

            for (var j = 0; j < opParams.OperationCount; ++j)
            {
                // Perform operation
                using (var op = new TrianglesOptimize2())
                {
                    // Operator inputs
                    op.AddModel(model, false);

                    // parameters
                    op.CheckGrowth = false;
                    op.QualityMetric = TriangleQualityMetric.AREAS_RATIO; //Skewness (N)
                    op.QualityThreshold = opParams.QualityThreshold;
                    op.MaximalGeometricError = opParams.MaximalGeometricError;
                    op.MinimalEdgeLength = 0;
                    op.MaximalEdgeLength = opParams.MaximalEdgeLength;
                    op.CheckMaximalEdgeLength = opParams.CheckMaximalEdgeLength;
                    op.NumberOfIterations = 0;
                    op.PreserveSurfaceBorders = opParams.PreserveSurfaceBorders;
                    op.SkipBadEdges = opParams.SkipBadEdges;
                    if (opParams.PreserveSurfaceBorders)
                    {
                        op.BadEdgeWeight = opParams.MaximalGeometricError * 100.0;
                    }

                    // Perform operator
                    try
                    {
                        for (var i = 0; i < opParams.NumberOfIterations; ++i)
                        {
                            op.Operate();
                        }
                    }
                    catch (TrianglesOptimize2.Exception e)
                    {
                        Console.WriteLine(e.Message);
                        return false;
                    }
                }
            }
            
            if (!MDCKConversion.ExportMDCK2StlFile(model, remeshedPath))
            {
                return false;
            }

            // Clean dispose
            model.Dispose();
            model = null;

            return true;
        }
    }
}