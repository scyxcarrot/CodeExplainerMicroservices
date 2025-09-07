using Rhino;
using Rhino.Geometry;
using RhinoMatSDKOperations.IO;
using System.IO;
using System.Linq;
using MDCK = Materialise.SDK.MDCK;

namespace RhinoMatSDKOperations.Fix
{
    public class MDCKHoleFillFreeform
    {
        /**
         * Perform the HoleFillFreeform operation by importing an stl from a path.
         * @return       The HoleFillFreeformed mesh
         */

        public static bool HoleFillFreeformOperationStl(Mesh mesh, MDCKHoleFillFreeformParameters opparams, out Mesh fillsurfmesh)
        {
            // Write temp mesh
            string StlFilePath = MDCKConversion.WriteStlTempFile(mesh);

            // Import the STL file
            MDCK.Model.Objects.Model mdck_in = new MDCK.Model.Objects.Model();
            using (var importer = new MDCK.Operators.ModelImportFromStl())
            {
                // Set operator parameters
                importer.FileName = StlFilePath;
                importer.ForceLoad = true; // STL file format check is done before reading
                importer.OutputModel = mdck_in;
                importer.MmPerUnit = 1.0; // Conversion factor: STL units to mm
                try
                {
                    importer.Operate(); // Import the STL
                    File.Delete(StlFilePath);
                }
                catch (MDCK.Operators.ModelImportFromStl.Exception)
                {
                    mdck_in.Dispose();
                    mdck_in = null;
                    fillsurfmesh = null;
                    File.Delete(StlFilePath);
                    return false;
                }
            }

            // Get the border contour that needs hole filling
            MDCK.Model.Objects.Contour contourToFill;
            if (mdck_in.Border.NumberOfContours > 0)
            {
                var test = mdck_in.Border.Contours.ToList();
                contourToFill = test[0];
            }
            else
            {
                mdck_in.Dispose();
                mdck_in = null;
                fillsurfmesh = null;
                RhinoApp.WriteLine("[MDCK::Error] You asked to fill an already closed mesh, abort.");
                return false; // unsuccessful
            }

            // Make a new surface that will hold the hole filling
            MDCK.Model.Objects.Surface fillsurf;
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
                    mdck_in.Dispose();
                    fillsurfmesh = null;
                    fillsurf = null;
                    RhinoApp.WriteLine("[MDCK::Error] Could not create a new surface.");
                    return false;
                }
                fillsurf = op_addsurf.NewSurface;
            }

            // Holefill operator
            using (var op_holefill = new MDCK.Operators.HoleFillFreeform())
            {
                op_holefill.AddPolyline(contourToFill);
                op_holefill.OutputSurface = fillsurf;

                op_holefill.GridSize = opparams.GridSize;
                op_holefill.Tangent = opparams.Tangent;
                // marking these triangles makes sure that we can copy them to a new model
                op_holefill.MarkNewTriangles = true;

                op_holefill.TreatAsOneHole = opparams.TreatAsOneHole;
                op_holefill.ViewDirection = opparams.ViewDirection;
                if (opparams.SetLeastSquareDirection)
                {
                    op_holefill.SetLeastSquareDirection();
                }
                op_holefill.CheckMinimalGridSize = opparams.CheckMinimalGridSize;
                try
                {
                    op_holefill.Operate();
                }
                catch (MDCK.Operators.HoleFillFreeform.Exception)
                {
                    mdck_in.Dispose();
                    fillsurfmesh = null;
                    fillsurf = null;
                    RhinoApp.WriteLine("[MDCK::Error] Could not hole fill.");
                    return false;
                }
            }

            // Make the output model with a new empty surface
            MDCK.Model.Objects.Model mdck_out = new MDCK.Model.Objects.Model();
            MDCK.Model.Objects.Surface copysurf;
            using (var op_addsurf2 = new MDCK.Operators.ModelAddSurface())
            {
                op_addsurf2.Model = mdck_out;
                op_addsurf2.NewSurfaceName = "CopyHoleFillingSurface";
                try
                {
                    op_addsurf2.Operate();
                }
                catch (MDCK.Operators.ModelAddSurface.Exception)
                {
                    op_addsurf2.Model = null;
                    mdck_in.Dispose();
                    mdck_out.Dispose();
                    fillsurfmesh = null;
                    copysurf = null;
                    RhinoApp.WriteLine("[MDCK::Error] Could not create a new copy surface.");
                    return false;
                }
                copysurf = op_addsurf2.NewSurface;
            }

            // MarkedTriangles Copy to copysurf
            using (var op = new MDCK.Operators.MarkedTrianglesCopyToSurface())
            {
                op.SourceModel = mdck_in;
                op.DestinationSurface = copysurf;
                try
                {
                    op.Operate();
                }
                catch (MDCK.Operators.MarkedTrianglesCopyToSurface.Exception)
                {
                    mdck_in.Dispose();
                    mdck_out.Dispose();
                    fillsurfmesh = null;
                    RhinoApp.WriteLine("[MDCK::Error] Could not copy surface to new model.");
                    return false;
                }
            }

            RhinoApp.WriteLine("[MDCK] Everything seemed to go well");
            // Convert to Rhino mesh via STL file
            bool ok = MDCKConversion.MDCK2RhinoMeshStl(mdck_out, out fillsurfmesh);
            mdck_out.Dispose();
            mdck_out = null;
            mdck_in.Dispose();
            mdck_in = null;
            return ok;
        }
    }
}