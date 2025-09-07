using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using RhinoMatSDKOperations.IO;
using System.Collections.Generic;
using System.Linq;
using MDCK = Materialise.SDK.MDCK;
using MDCKTriangle = Materialise.SDK.MDCK.Model.Objects.Triangle;
using MDCKVertex = Materialise.SDK.MDCK.Model.Objects.Vertex;

namespace RhinoMatSDKOperations.Fix
{
    public class MDCKHoleFill
    {
        /**
         * Fill a hole given a vertex lying on the hole border.
         */

        public static bool OperatorHoleFillNormal(Mesh targetmesh, int borderVertex, out Mesh mesh_filled)
        {
            // MatSDK does not support quads
            if (targetmesh.Faces.QuadCount > 0)
            {
                targetmesh.Faces.ConvertQuadsToTriangles();
            }

            // Get MDCK model and face/vertex mapping
            MDCK.Model.Objects.Model mdck_in;
            List<MDCKVertex> mesh_verts;
            List<MDCKTriangle> mesh_tris;
            bool res = MDCKConversion.Rhino2MDCKMeshUnsafe(targetmesh, out mdck_in, out mesh_verts, out mesh_tris);
            if (!res)
            {
                mesh_verts = null;
                mesh_tris = null;
                mdck_in.Dispose();
                mesh_filled = null;
                return false;
            }

            // Search for the correct bordercurve
            MDCK.Model.Objects.Contour hole_border = null;
            foreach (var curve in mdck_in.Border.Contours)
            {
                if (curve.HasVertex(mesh_verts[borderVertex]))
                {
                    hole_border = curve;
                    break;
                }
            }
            if (null == hole_border)
            {
                hole_border = null;
                mesh_verts = null;
                mesh_tris = null;
                mdck_in.Dispose();
                mesh_filled = null;
                return false;
            }

            // Create output model and surface
            MDCK.Model.Objects.Surface surf_out;
            using (var op_addsurf = new MDCK.Operators.ModelAddSurface())
            {
                op_addsurf.Model = mdck_in;
                op_addsurf.NewSurfaceName = "HoleFillingSurface";
                try
                {
                    op_addsurf.Operate();
                }
                catch (MDCK.Operators.ModelAddSurface.Exception)
                {
                    op_addsurf.Model = null;
                    surf_out = null;
                    hole_border = null;
                    mesh_verts = null;
                    mesh_tris = null;
                    mdck_in.Dispose();
                    mesh_filled = null;
                    return false;
                }
                surf_out = op_addsurf.NewSurface;
            }

            // Make the operator
            using (var sop = new MDCK.Operators.HoleFillNormal())
            {
                sop.AddPolyline(hole_border); // Polyline curve references the model
                sop.MarkNewTriangles = false;
                sop.TreatAsOneHole = false; // interpret the added polylines as being the contours of one surface and create only one surface
                sop.OutputSurface = surf_out; // Surface in the same model as referenced by polyline curve
                try
                {
                    sop.Operate();
                }
                catch (MDCK.Operators.HoleFillNormal.Exception)
                {
                    surf_out = null;
                    hole_border = null;
                    mesh_verts = null;
                    mesh_tris = null;
                    mdck_in.Dispose();
                    mesh_filled = null;
                    return false;
                }
            }

            // Convert the output back to a Rhino mesh
            try
            {
                return MDCKConversion.MDCK2RhinoMeshStl(mdck_in, out mesh_filled);
            }
            finally
            {
                surf_out.Dispose();
                surf_out = null;
                hole_border.Dispose();
                hole_border = null;
                mesh_verts = null;
                mesh_tris = null;
                mdck_in.Dispose();
            }
        }

        /**
        * Stitch two meshes together at their borders, given by a closed sequence of vertices.
        */

        public static bool OperatorHoleFillNormal(RhinoDoc doc, Mesh first_part, Mesh second_part, int[] first_border, int[] second_border, out Mesh mesh_filled)
        {
            // MatSDK does not support quads
            if (first_part.Faces.QuadCount > 0)
                first_part.Faces.ConvertQuadsToTriangles();
            if (second_part.Faces.QuadCount > 0)
                second_part.Faces.ConvertQuadsToTriangles();
            mesh_filled = null;

            // Put everything in one mesh
            int numfirst = first_part.Vertices.Count;
            first_part.Append(second_part);
            second_border = second_border.Select(x => x + numfirst).ToArray(); // Update border indices

            // Create new MDCK model for mesh
            MDCK.Model.Objects.Model matmesh;
            List<MDCK.Model.Objects.Curve> bordercurves;
            MDCK.Model.Objects.CurveSet curveset;
            bool res = MDCKConversion.Rhino2MDCKMeshUnsafe(first_part, new List<int[]>() { first_border, second_border }, out matmesh, out bordercurves, out curveset);
            if (!res)
            {
                mesh_filled = null;
                return false;
            }

            // Create output surface on the model
            MDCK.Model.Objects.Surface surfout;
            var op_addsurf = new MDCK.Operators.ModelAddSurface();
            op_addsurf.Model = matmesh;
            op_addsurf.NewSurfaceName = "HoleFillingSurface";
            op_addsurf.Operate();
            surfout = op_addsurf.NewSurface;

            // Make the operator
            var sop = new MDCK.Operators.HoleFillNormal();
            sop.AddPolylineSet(curveset); // Curveset must be part of the model
            sop.MarkNewTriangles = false;
            sop.TreatAsOneHole = true; // interpret the added polylines as being the contours of one surface and create only one surface
            sop.OutputSurface = surfout; // Surface in the same model as referenced by polyline curve
            try
            {
                sop.Operate();
            }
            catch (MDCK.Operators.HoleFillNormal.Exception)
            {
                return false;
                throw;
            }

            // Convert the output back to a Rhino mesh
            Mesh rmesh;
            bool result = MDCKConversion.MDCK2RhinoMeshStl(matmesh, out rmesh);
            if (!result)
            {
                mesh_filled = null;
                return false;
            }
            mesh_filled = rmesh;
            return true;
        }

        /**
         * Fill a hole and retrieve only the filling surface.
         */

        public static bool OperatorHoleFillNormal(Mesh openmesh, out Mesh filler)
        {
            // Set up
            filler = null;
            if (null == openmesh || openmesh.IsClosed)
                return false;

            // MatSDK does not support quads
            if (openmesh.Faces.QuadCount > 0)
                openmesh.Faces.ConvertQuadsToTriangles();

            // Create new MDCK model for mesh
            MDCK.Model.Objects.Model matmesh;
            bool res = MDCKConversion.Rhino2MDCKMeshStl(openmesh, out matmesh);

            // Call the actual hole fill operator
            using (var holefiller = new MDCK.Operators.HoleFillNormal())
            using (var filledmodel = new MDCK.Model.Objects.Model())
            {
                holefiller.AddPolylineSet(matmesh.Border);
                holefiller.TreatAsOneHole = false;
                holefiller.MarkNewTriangles = false;
                try
                {
                    holefiller.Operate();
                }
                catch (MDCK.Operators.HoleFillNormal.Exception)
                {
                    return false;
                }

                // Add all patches to new surface
                foreach (var patch in holefiller.OutputSurfaces)
                {
                    // Create new model to feature
                    using (var mover = new MDCK.Operators.SurfaceMoveToFeature())
                    {
                        mover.SourceSurface = patch;
                        mover.DestinationFeature = filledmodel.MainFeature;
                        try
                        {
                            mover.Operate();
                        }
                        catch (MDCK.Operators.SurfaceMoveToFeature.Exception)
                        {
                            matmesh.Dispose();
                            matmesh = null;
                            return false;
                        }
                    }
                }

                // Convert the output back to a Rhino mesh
                bool ok = MDCKConversion.MDCK2RhinoMeshStl(filledmodel, out filler);
                matmesh.Dispose();
                matmesh = null;
                return ok;
            }
        }

        /**
        * Stitch two meshes together by using their bad contours
        * computed by MDCK. Meshes must contain a single border each.
        */

        public static bool OperatorStitchBorders(RhinoDoc doc, Mesh first_part, Mesh second_part, out Mesh mesh_filled)
        {
            // MatSDK does not support quads
            if (first_part.Faces.QuadCount > 0)
                first_part.Faces.ConvertQuadsToTriangles();
            if (second_part.Faces.QuadCount > 0)
                second_part.Faces.ConvertQuadsToTriangles();
            mesh_filled = null;

            // METHOD 1: Manually convert surface border to child CurveSet object of model
            first_part.Append(second_part);
            first_part.Compact();

            // Create new MDCK model for mesh
            MDCK.Model.Objects.Model matmesh;
            bool res = MDCKConversion.Rhino2MDCKMeshStl(first_part, out matmesh);

            // METHOD 4: See documentation for MDCKOperatorHoleFillNormal::GetOutputSurfaceAt(int i)
            // Call the actual hole fill operator
            using (var holefiller = new MDCK.Operators.HoleFillNormal())
            using (var filledmodel = new MDCK.Model.Objects.Model())
            {
                holefiller.AddPolylineSet(matmesh.Border);
                holefiller.TreatAsOneHole = true;
                holefiller.MarkNewTriangles = false;
                try
                {
                    holefiller.Operate();
                }
                catch (MDCK.Operators.HoleFillNormal.Exception)
                {
                    return false;
                }

                // Add all patches to new surface
                foreach (var patch in holefiller.OutputSurfaces)
                {
                    // Create new model to feature
                    using (var mover = new MDCK.Operators.SurfaceMoveToFeature())
                    {
                        mover.SourceSurface = patch;
                        mover.DestinationFeature = filledmodel.MainFeature;
                        try
                        {
                            mover.Operate();
                        }
                        catch (MDCK.Operators.SurfaceMoveToFeature.Exception)
                        {
                            matmesh.Dispose();
                            matmesh = null;
                            return false;
                        }
                    }
                }

                // Convert the output back to a Rhino mesh
                bool ok = MDCKConversion.MDCK2RhinoMeshStl(filledmodel, out mesh_filled);
                matmesh.Dispose();
                matmesh = null;
                return ok;
            }

            // Extract border curves from model
            //MDCK.Model.Objects.CurveSet borderCurves;
            //try
            //{
            //    var mupdate = new MDCK.Operators.ModelUpdate();
            //    mupdate.Model = matmesh;
            //    mupdate.RemoveFreeVertices = false;
            //    mupdate.ResortVertexList = false;
            //    mupdate.Operate();

            //    var setadd = new MDCK.Operators.ModelAddCurveSet();
            //    setadd.Model = matmesh;
            //    setadd.Operate();
            //    borderCurves = setadd.NewCurveSet;

            //    var bop = new MDCK.Operators.BorderCopyToCurveSet();
            //    bop.DestinationCurveSet = borderCurves;
            //    bop.SourceBorder = matmesh.Border; // will use borders of its single surface
            //    bop.Operate();

            //}
            //catch (MDCK.Operators.ModelUpdate.Exception)
            //{
            //    return false;
            //}
            //catch (MDCK.Operators.ModelAddCurveSet.Exception)
            //{
            //    return false;
            //}
            //catch (MDCK.Operators.BorderCopyToCurveSet.Exception)
            //{
            //    return false;
            //}

            // METHOD 2: Add the border itself as the curve set to operate on
            //var sop = new MDCK.Operators.HoleFillNormal();
            //sop.AddPolylineSet(matmesh.Border); // Curveset must be part of the model

            // METHOD 3: Create a separate MDCKModel for each mesh, then use the respective borders
            // Create new MDCK model for mesh
            //MDCK.Model.Objects.Model model_a, model_b;
            //bool resa = MDCKUtil.Rhino2MDCKMesh(first_part, out model_a);
            //bool resb = MDCKUtil.Rhino2MDCKMesh(second_part, out model_b);
            //if (!resa || !resb)
            //    return false;

            // Create output surface on the model
            //MDCK.Model.Objects.Surface surfout;
            //try
            //{
            //    var op_addsurf = new MDCK.Operators.ModelAddSurface();
            //    op_addsurf.Model = matmesh;
            //    op_addsurf.NewSurfaceName = "HoleFillingSurface";
            //    op_addsurf.Operate();
            //    surfout = op_addsurf.NewSurface;
            //}
            //catch (MDCK.Operators.ModelAddSurface.Exception)
            //{
            //    return false;
            //}
        }
    }
}