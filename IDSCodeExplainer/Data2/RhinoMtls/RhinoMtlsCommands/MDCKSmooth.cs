using Materialise.SDK.MDCK.Model.Objects;
using Materialise.SDK.MDCK.Operators;
using Rhino.Geometry;
using RhinoMatSDKOperations.IO;
using System.Collections.Generic;
using System.Linq;

namespace RhinoMatSDKOperations.Smooth
{
    public class MDCKSmooth
    {
        public static bool OperatorSmooth(IEnumerable<Mesh> rhmeshes, MDCKSmoothParameters opParams, out Mesh smoothen)
        {
            // Import the STL file
            using (var mdck_in = new Model())
            {
                bool rc = MDCKConversion.Rhino2MDCKSurfacesStl(mdck_in, rhmeshes.ToArray());
                if (!rc)
                {
                    smoothen = null;
                    return false;
                }

                // Perform operation
                using (var sop = new Smoothen())
                {
                    // Operator inputs
                    sop.AddModel(mdck_in);

                    // parameters
                    if (opParams.SmoothenAlgorithm.HasValue)
                    {
                        switch (opParams.SmoothenAlgorithm.Value)
                        {
                            case SmoothenAlgorithm.Curvature:
                                sop.SmoothenAlgorithm = Smoothen.ESmoothAlgorithm.CURVATURE;
                                break;

                            case SmoothenAlgorithm.FirstOrderLaplacian:
                                sop.SmoothenAlgorithm = Smoothen.ESmoothAlgorithm.FIRST_ORDER_LAPLACIAN;
                                break;

                            case SmoothenAlgorithm.SecondOrderLaplacian:
                                sop.SmoothenAlgorithm = Smoothen.ESmoothAlgorithm.SECOND_ORDER_LAPLACIAN;
                                break;
                        }
                    }

                    if (opParams.Compensation.HasValue)
                    {
                        sop.Compensation = opParams.Compensation.Value;
                    }

                    if (opParams.PreserveBadEdges.HasValue)
                    {
                        sop.PreserveBadEdges = opParams.PreserveBadEdges.Value;
                    }

                    if (opParams.PreserveSharpEdges.HasValue)
                    {
                        sop.PreserveSharpEdges = opParams.PreserveSharpEdges.Value;
                    }

                    if (opParams.SharpEdgeAngle.HasValue)
                    {
                        sop.SharpEdgeAngle = opParams.SharpEdgeAngle.Value;
                    }

                    if (opParams.SmoothenFactor.HasValue)
                    {
                        sop.SmoothenFactor = opParams.SmoothenFactor.Value;
                    }

                    if (opParams.SmoothenIterations.HasValue)
                    {
                        sop.SmoothenIterations = (uint)opParams.SmoothenIterations.Value;
                    }

                    // Perform operator
                    try
                    {
                        sop.Operate();
                    }
                    catch (Smoothen.Exception)
                    {
                        smoothen = null;
                        return false;
                    }
                }

                // Convert to Rhino mesh via STL file
                return MDCKConversion.MDCK2RhinoMeshStl(mdck_in, out smoothen);
            }
        }
    }
}