using Materialise.SDK.MDCK.Operators;
using Rhino.Geometry;
using RhinoMatSDKOperations.IO;
using System.Collections.Generic;
using System.Linq;
using MDCK = Materialise.SDK.MDCK;
using WPoint3d = System.Windows.Media.Media3D.Point3D;
using WVector3d = System.Windows.Media.Media3D.Vector3D;

namespace RhinoMatSDKOperations.Smooth
{
    public class MDCKSmoothEdge
    {
        /**
         * MDCK Smooth edge operation (similar to fillet/round).
         *
         * @param surfStartIdx      The starting indices in the mesh face list where
         *                          each new surface starts.
         */

        public static bool OperatorSmoothEdge(IEnumerable<Mesh> rhmeshes, out Mesh rounded, Point3d edgePoint, MDCKSmoothEdgeParameters opParams)
        {
            // Import the STL file
            using (var mdck_in = new MDCK.Model.Objects.Model())
            {
                bool rc = MDCKConversion.Rhino2MDCKSurfacesStl(mdck_in, rhmeshes.ToArray());
                if (!rc)
                {
                    rounded = null;
                    return false;
                }

                var geo = new GeometryCurveCreateAngle();
                geo.AddModel(mdck_in);
                geo.Operate();

                // Find the target curve
                var target_point = new WPoint3d(edgePoint.X, edgePoint.Y, edgePoint.Z);
                MDCK.Model.Objects.Curve target_curve = null;
                const double dist_thresh = 0.001;
                bool found_curve = false;
                foreach (var curve in mdck_in.Curves)
                {
                    if (found_curve)
                        break;

                    IEnumerable<WPoint3d> crv_pts = curve.Polyline3D.Points;
                    foreach (WPoint3d cpt in crv_pts)
                    {
                        WVector3d diff = target_point - cpt;
                        if (diff.Length < dist_thresh)
                        {
                            target_curve = curve;
                            found_curve = true;
                            break;
                        }
                    }
                }
                if (!found_curve)
                {
                    rounded = null;
                    return false;
                }

                // Perform operation
                using (var sop = new MDCK.Operators.ModelDeformationCurveSmooth())
                {
                    // Operator inputs
                    sop.Model = mdck_in;
                    sop.Curve = target_curve;

                    // Smooth along polyline parameters
                    if (opParams.USE_RegionOfInfluence)
                        sop.RegionOfInfluence = opParams.RegionOfInfluence;
                    if (opParams.USE_PointWeight)
                        sop.PointWeight = opParams.PointWeight;

                    // Deformation parameters
                    if (opParams.USE_Iteration)
                        sop.Iteration = opParams.Iteration;
                    if (opParams.USE_AutoSubdivide)
                        sop.AutoSubdivide = opParams.AutoSubdivide;

                    // Subdivide parameters
                    if (opParams.USE_RemeshLowQuality)
                        sop.RemeshLowQuality = opParams.RemeshLowQuality;
                    if (opParams.USE_SkipBorder)
                        sop.SkipBorder = opParams.SkipBorder;
                    if (opParams.USE_IgnoreSurfaceInfo)
                        sop.IgnoreSurfaceInfo = opParams.IgnoreSurfaceInfo;
                    if (opParams.USE_MaxEdgeLength)
                        sop.MaxEdgeLength = opParams.MaxEdgeLength;
                    if (opParams.USE_MinEdgeLength)
                        sop.MinEdgeLength = opParams.MinEdgeLength;
                    if (opParams.USE_BadThreshold)
                        sop.BadThreshold = opParams.BadThreshold;
                    if (opParams.USE_FastCollapse)
                        sop.FastCollapse = opParams.FastCollapse;
                    if (opParams.USE_FlipEdges)
                        sop.FlipEdges = opParams.FlipEdges;
                    if (opParams.USE_SubdivisionMethod)
                    {
                        switch (opParams.SubdivisionMethod)
                        {
                            case SubdivisionMethod.Cubic:
                                sop.SubdivisionMethod = MDCK.Operators.ModelDeformationCurveSmooth.ESubdivisionMethod.CUBIC;
                                break;

                            case SubdivisionMethod.Linear:
                                sop.SubdivisionMethod = MDCK.Operators.ModelDeformationCurveSmooth.ESubdivisionMethod.LINEAR;
                                break;

                            case SubdivisionMethod.FourPoint:
                                sop.SubdivisionMethod = MDCK.Operators.ModelDeformationCurveSmooth.ESubdivisionMethod.FOUR_POINT;
                                break;

                            default:
                                goto case SubdivisionMethod.Cubic;
                        }
                    }

                    // Perform operator
                    try
                    {
                        sop.Operate();
                    }
                    catch (MDCK.Operators.ModelDeformationCurveSmooth.Exception)
                    {
                        rounded = null;
                        return false;
                    }
                }

                // Convert to Rhino mesh via STL file
                bool ok = MDCKConversion.MDCK2RhinoMeshStl(mdck_in, out rounded);
                // Dispose of all variables that hold reference to model
                target_curve.Dispose();
                target_curve = null;
                //GC.Collect();
                //GC.WaitForPendingFinalizers();
                return ok;
            }
        }
    }
}