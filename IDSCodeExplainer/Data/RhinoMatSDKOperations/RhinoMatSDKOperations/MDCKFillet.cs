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
    public class MDCKFillet
    {
        /**
         * Convert Rhino Meshes to MDCK model with separate surfaces for each disjoint mesh.
         *
         * @pram rhmesh             RhinoMesh consisting of only triangular faces
         *                          with the faces of each subsurface stored
         *                          sequentially in the face matrix.
         * @param surfStartIdx      The starting indices in the mesh face list where
         *                          each new surface starts.
         * @param surfFilletRadius  A fillet radius for each surface in surfStartIdx
         *                          radius <= 0 means no fillet.
         */

        public static bool FilletOperation(IEnumerable<Mesh> rhmeshes, out Mesh rounded, Point3d edgePoint, double filletRadius, uint numArcSegments = 4, double minSliceDist = 0.2, bool abruptEnding = false)
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

                // Perform the fillet operation
                using (var rounder = new MDCK.Operators.Round())
                {
                    // Set operator parameters
                    rounder.ModelPolyline = target_curve;
                    rounder.Radius = filletRadius;
                    rounder.NumberOfArcSegments = numArcSegments; // TODO: set depending on radius
                    rounder.AbruptEnding = abruptEnding;
                    rounder.MinimumSliceDistance = minSliceDist;

                    // Perform operation
                    try
                    {
                        rounder.Operate();
                    }
                    catch (MDCK.Operators.Round.Exception)
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
            } // end using MDCKModel
        }
    }
}