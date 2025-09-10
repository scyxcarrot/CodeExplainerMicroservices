using Materialise.SDK.MDCK.Operators;
using Rhino;
using Rhino.Geometry;
using RhinoMatSDKOperations.Fix;
using RhinoMatSDKOperations.IO;
using System.IO;
using System.Linq;
using MDCK = Materialise.SDK.MDCK;

namespace RhinoMatSDKOperations.Smooth
{
    public class MDCKSmoothImplantBorder
    {
        public static bool OperatorSmoothEdge(Mesh topMesh, Mesh sideMesh, Mesh bottomMesh, out Mesh rounded, MDCKSmoothImplantBorderParameters opParams)
        {
            return OperatorSmoothEdge(topMesh, sideMesh, bottomMesh, out rounded, opParams, false);
        }

        public static bool OperatorSmoothEdge(Mesh topMesh, Mesh sideMesh, Mesh bottomMesh, out Mesh rounded, MDCKSmoothImplantBorderParameters opParams, bool applyFixes)
        {
            rounded = new Mesh();

            // Write the 3 meshes as temp stl file
            string topPath = MDCKConversion.WriteStlTempFile(topMesh);
            string sidePath = MDCKConversion.WriteStlTempFile(sideMesh);
            string bottomPath = MDCKConversion.WriteStlTempFile(bottomMesh);
            bool success;

            // Import the 3 stls as 3 models
            MDCK.Model.Objects.Model model1;
            success = MDCKInputOutput.ModelFromStlPath(topPath, out model1);
            File.Delete(topPath);
            if (!success)
                return false;

            MDCK.Model.Objects.Model model2;
            success = MDCKInputOutput.ModelFromStlPath(sidePath, out model2);
            File.Delete(sidePath);
            if (!success)
                return false;

            MDCK.Model.Objects.Model model3;
            success = MDCKInputOutput.ModelFromStlPath(bottomPath, out model3);
            File.Delete(bottomPath);
            if (!success)
                return false;

            // Get surfaces from the 3 models
            MDCK.Model.Objects.Surface orTopSurf = model1.Surfaces.FirstOrDefault();
            MDCK.Model.Objects.Surface orSideSurf = model2.Surfaces.FirstOrDefault();
            MDCK.Model.Objects.Surface orBottomSurf = model3.Surfaces.FirstOrDefault();

            // Make new model and add 3 surfaces
            MDCK.Model.Objects.Model model = new MDCK.Model.Objects.Model();
            MDCK.Operators.ModelAddSurface mas = new MDCK.Operators.ModelAddSurface();
            MDCK.Operators.SurfaceCopyToSurface scts = new MDCK.Operators.SurfaceCopyToSurface();
            mas.Model = model;

            // for top surface
            mas.NewSurfaceName = "top";
            mas.Operate();
            MDCK.Model.Objects.Surface topSurf = mas.NewSurface;
            scts.SourceSurface = orTopSurf;
            scts.DestinationSurface = topSurf;
            scts.Operate();

            // for side surface
            mas.NewSurfaceName = "side";
            mas.Operate();
            MDCK.Model.Objects.Surface sideSurf = mas.NewSurface;
            scts.SourceSurface = orSideSurf;
            scts.DestinationSurface = sideSurf;
            scts.Operate();

            // for bottom surface
            mas.NewSurfaceName = "bottom";
            mas.Operate();
            MDCK.Model.Objects.Surface bottomSurf = mas.NewSurface;
            scts.SourceSurface = orBottomSurf;
            scts.DestinationSurface = bottomSurf;
            scts.Operate();

            // Convert contours to curves
            MDCK.Operators.ContourCopyToCurve cctc = new MDCK.Operators.ContourCopyToCurve();
            MDCK.Operators.ModelAddCurve mac = new MDCK.Operators.ModelAddCurve();

            // for top surface
            MDCK.Model.Objects.Contour topContour = topSurf.Border.Contours.FirstOrDefault();
            mac.Model = model;
            mac.Operate();
            MDCK.Model.Objects.Curve topCurve = mac.NewCurve;
            cctc.SourceContour = topContour;
            cctc.DestinationCurve = topCurve;
            cctc.Operate();

            // for bottom surface
            MDCK.Model.Objects.Contour bottomContour = bottomSurf.Border.Contours.FirstOrDefault();
            mac.Model = model;
            mac.Operate();
            MDCK.Model.Objects.Curve bottomCurve = mac.NewCurve;
            cctc.SourceContour = bottomContour;
            cctc.DestinationCurve = bottomCurve;
            cctc.Operate();

            if (applyFixes)
            {
                MDCKAutoFix.AutoFixOperation(model, new MDCKAutoFixParameters(true));

                using (var op = new Subdivide())
                {
                    op.AddModel(model);
                    op.SubdivideIterations = 3;
                    op.Operate();
                }
            }

            RhinoApp.WriteLine("[MDCK] Model triangles: {0}", model.NumberOfTriangles);

            // Smooth top edge
            success = SmoothEdge(model, topCurve, opParams.topEdgeRadius, opParams.topMinEdgeLength, opParams.topMaxEdgeLength, opParams.iterations);
            if (!success)
                return false;

            RhinoApp.WriteLine("[MDCK] Model triangles: {0}", model.NumberOfTriangles);

            // Smooth bot edge
            success = SmoothEdge(model, bottomCurve, opParams.bottomEdgeRadius, opParams.bottomMinEdgeLength, opParams.bottomMaxEdgeLength, opParams.iterations);
            if (!success)
                return false;

            RhinoApp.WriteLine("[MDCK] Model triangles: {0}", model.NumberOfTriangles);

            // Read the stl in rhino
            success = MDCKConversion.MDCK2RhinoMeshStl(model, out rounded);
            if (!success)
                return false;

            // Clean dispose
            model.Dispose();
            model = null;
            model1.Dispose();
            model1 = null;
            model2.Dispose();
            model2 = null;
            model3.Dispose();
            model3 = null;

            return true;
        }

        public static bool SmoothEdge(MDCK.Model.Objects.Model model, MDCK.Model.Objects.Curve curve, double influenceDistance, double minEdgeLength, double maxEdgeLength, int iterations)
        {
            MDCK.Operators.ModelDeformationCurveSmooth se = new MDCK.Operators.ModelDeformationCurveSmooth();
            se.Curve = curve;
            se.Model = model;
            se.Iteration = iterations;
            se.MaxEdgeLength = maxEdgeLength;
            se.MinEdgeLength = minEdgeLength;
            se.RemeshLowQuality = false;
            se.RegionOfInfluence = influenceDistance;
            se.PointWeight = 0;
            se.AutoSubdivide = true;
            se.FastCollapse = true;
            se.SubdivisionMethod = MDCK.Operators.ModelDeformationCurveSmooth.ESubdivisionMethod.LINEAR;
            se.Operate();

            return true;
        }
    }
}