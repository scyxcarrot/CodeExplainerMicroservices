using Rhino;
using Rhino.Geometry;
using RhinoMatSDKOperations.IO;
using System.IO;
using System.Linq;
using MDCK = Materialise.SDK.MDCK;

namespace RhinoMatSDKOperations.Smooth
{
    public class MDCKSmoothSurfaceEdge
    {
        public static bool OperatorSmoothEdge(Mesh firstMesh, Mesh secondMesh, out Mesh rounded, int iteration, double minEdgeLength, double maxEdgeLength, double regionOfInfluence)
        {
            rounded = null;

            // Write the meshes as temp stl file
            string firstPath = MDCKConversion.WriteStlTempFile(firstMesh);
            string secondPath = MDCKConversion.WriteStlTempFile(secondMesh);
            bool success;

            // Import the stls as models
            MDCK.Model.Objects.Model model1;
            success = MDCKInputOutput.ModelFromStlPath(firstPath, out model1);
            File.Delete(firstPath);
            if (!success)
            {
                return false;
            }

            MDCK.Model.Objects.Model model2;
            success = MDCKInputOutput.ModelFromStlPath(secondPath, out model2);
            File.Delete(secondPath);
            if (!success)
            {
                return false;
            }

            // Get surfaces from the models
            MDCK.Model.Objects.Surface orFirstSurf = model1.Surfaces.FirstOrDefault();
            MDCK.Model.Objects.Surface orSecondSurf = model2.Surfaces.FirstOrDefault();

            // Make new model and add surfaces
            MDCK.Model.Objects.Model model = new MDCK.Model.Objects.Model();
            MDCK.Operators.ModelAddSurface mas = new MDCK.Operators.ModelAddSurface();
            MDCK.Operators.SurfaceCopyToSurface scts = new MDCK.Operators.SurfaceCopyToSurface();
            mas.Model = model;

            // for first surface
            mas.NewSurfaceName = "first";
            mas.Operate();
            MDCK.Model.Objects.Surface firstSurf = mas.NewSurface;
            scts.SourceSurface = orFirstSurf;
            scts.DestinationSurface = firstSurf;
            scts.Operate();

            // for second surface
            mas.NewSurfaceName = "second";
            mas.Operate();
            MDCK.Model.Objects.Surface secondSurf = mas.NewSurface;
            scts.SourceSurface = orSecondSurf;
            scts.DestinationSurface = secondSurf;
            scts.Operate();

            // Convert contours to curves
            MDCK.Operators.ContourCopyToCurve cctc = new MDCK.Operators.ContourCopyToCurve();
            MDCK.Operators.ModelAddCurve mac = new MDCK.Operators.ModelAddCurve();
            
            MDCK.Model.Objects.Contour firstContour = firstSurf.Border.Contours.FirstOrDefault();
            mac.Model = model;
            mac.Operate();
            MDCK.Model.Objects.Curve firstCurve = mac.NewCurve;
            cctc.SourceContour = firstContour;
            cctc.DestinationCurve = firstCurve;
            cctc.Operate();

            RhinoApp.WriteLine("[MDCK] Model triangles: {0}", model.NumberOfTriangles);

            // Smooth edge
            success = MDCKSmoothImplantBorder.SmoothEdge(model, firstCurve, regionOfInfluence, minEdgeLength, maxEdgeLength, iteration);
            if (!success)
            {
                return false;
            }

            RhinoApp.WriteLine("[MDCK] Model triangles: {0}", model.NumberOfTriangles);

            // Read the stl in rhino
            success = MDCKConversion.MDCK2RhinoMeshStl(model, out rounded);
            if (!success)
            {
                return false;
            }

            // Clean dispose
            model.Dispose();
            model = null;
            model1.Dispose();
            model1 = null;
            model2.Dispose();
            model2 = null;

            return true;
        }
    }
}