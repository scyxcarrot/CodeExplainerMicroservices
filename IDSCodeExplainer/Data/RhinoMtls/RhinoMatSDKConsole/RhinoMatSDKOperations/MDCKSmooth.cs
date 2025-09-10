using Materialise.SDK.MDCK.Model.Objects;
using Materialise.SDK.MDCK.Operators;
using RhinoMatSDKOperations.IO;
using System;

namespace RhinoMatSDKOperations.Smooth
{
    public class MDCKSmooth
    {
        public static bool OperatorSmooth(string meshPath, MDCKSmoothParameters opParams, string smoothenPath)
        {
            // Import the STL file
            Model model;
            var success = MDCKInputOutput.ModelFromStlPath(meshPath, out model);
            if (!success)
            {
                return false;
            }

            // Perform operation
            using (var sop = new Smoothen())
            {
                // Operator inputs
                sop.AddModel(model);

                // parameters
                switch (opParams.SmoothenAlgorithm.ToUpper())
                {
                    case "CURVATURE":
                        sop.SmoothenAlgorithm = Smoothen.ESmoothAlgorithm.CURVATURE;
                        break;

                    case "SECOND_ORDER_LAPLACIAN":
                        sop.SmoothenAlgorithm = Smoothen.ESmoothAlgorithm.SECOND_ORDER_LAPLACIAN;
                        break;

                    case "FIRST_ORDER_LAPLACIAN":
                    default:
                        sop.SmoothenAlgorithm = Smoothen.ESmoothAlgorithm.FIRST_ORDER_LAPLACIAN;
                        break;
                }

                sop.Compensation = opParams.Compensation;
                sop.PreserveBadEdges = opParams.PreserveBadEdges;
                sop.PreserveSharpEdges = opParams.PreserveSharpEdges;
                sop.SharpEdgeAngle = opParams.SharpEdgeAngle;
                sop.SmoothenFactor = opParams.SmoothenFactor;
                sop.SmoothenIterations = (uint) opParams.SmoothenIterations;

                // Perform operator
                try
                {
                    sop.Operate();
                }
                catch (Smoothen.Exception e)
                {
                    Console.WriteLine(e.Message);
                    return false;
                }
            }
            
            if (!MDCKConversion.ExportMDCK2StlFile(model, smoothenPath))
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