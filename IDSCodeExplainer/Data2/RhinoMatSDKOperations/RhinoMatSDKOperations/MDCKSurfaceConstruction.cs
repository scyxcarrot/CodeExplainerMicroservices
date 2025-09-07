using Rhino;
using Rhino.Collections;
using Rhino.Geometry;
using RhinoMatSDKOperations.IO;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MDCK = Materialise.SDK.MDCK;
using WVector3d = System.Windows.Media.Media3D.Vector3D;

namespace RhinoMatSDKOperations.Surface
{
    public class MDCKSurfaceConstruction
    {
        public static void sampleCurve(Curve theCurve, out Point3dList curveEdges, int numberOfPoints = 100)
        {
            curveEdges = new Point3dList();
            for (int i = 0; i < numberOfPoints; i++)
            {
                curveEdges.Add(theCurve.PointAtNormalizedLength((double)i / (double)numberOfPoints));
            }
        }

        public static bool OperatorSurfaceConstruction(Mesh filledSkirt, Mesh filled, List<Curve> CurveList, Vector3d viewVec, out Mesh constructSurf)
        {
            constructSurf = new Mesh();
            bool success;

            // Write the mesh as temp stl file
            string filledSkirtPath = MDCKConversion.WriteStlTempFile(filledSkirt);
            string filledPath = MDCKConversion.WriteStlTempFile(filled);

            // Import the stl as model
            MDCK.Model.Objects.Model filledSkirtModel;
            success = MDCKInputOutput.ModelFromStlPath(filledSkirtPath, out filledSkirtModel);
            File.Delete(filledSkirtPath);
            if (!success)
                return false;

            // Import the stl as model
            MDCK.Model.Objects.Model filledModel;
            success = MDCKInputOutput.ModelFromStlPath(filledPath, out filledModel);
            File.Delete(filledPath);
            if (!success)
                return false;

            // New model that will hold the smooth skirt
            MDCK.Model.Objects.Model outputModel = new MDCK.Model.Objects.Model();
            MDCK.Operators.ModelAddSurface mas = new MDCK.Operators.ModelAddSurface();
            MDCK.Operators.SurfaceCopyToSurface scts = new MDCK.Operators.SurfaceCopyToSurface();

            // Get surfaces from the models
            MDCK.Model.Objects.Surface orFilledSkirtSurf = filledSkirtModel.Surfaces.FirstOrDefault();
            MDCK.Model.Objects.Surface orFilledSurf = filledModel.Surfaces.FirstOrDefault();

            // boneCurve contour
            MDCK.Model.Objects.Contour boneContour = orFilledSkirtSurf.Border.Contours.FirstOrDefault();

            // cupCurve contour
            MDCK.Model.Objects.Contour cupContour = orFilledSurf.Border.Contours.FirstOrDefault();

            // Convert all construction curves to mdck curves
            List<MDCK.Math.Objects.Polyline3D> polyLines = new List<MDCK.Math.Objects.Polyline3D>();
            foreach (Curve conCurve in CurveList)
            {
                Point3dList curvePoints = new Point3dList();
                sampleCurve(conCurve, out curvePoints, numberOfPoints: 30);
                MDCK.Math.Objects.Polyline3D polyLine = new MDCK.Math.Objects.Polyline3D();
                foreach (Point3d thePoint in curvePoints)
                {
                    polyLine.AddPoint(thePoint.X, thePoint.Y, thePoint.Z);
                }
                polyLines.Add(polyLine);
            }

            // Wvector3d
            WVector3d theVec = new WVector3d();
            theVec.X = viewVec.X;
            theVec.Y = viewVec.Y;
            theVec.Z = viewVec.Z;

            // Holefill
            using (var op_holefill = new MDCK.Operators.HoleFillReconstruction())
            {
                //op_holefill.NormalCalculationMethod = MDCK.Operators.HoleFillReconstruction.ENormalCalculationMethods.AVERAGE_NORMALS;
                //op_holefill.UserNormal = theVec;
                op_holefill.AddPolyline(boneContour);
                op_holefill.AddPolyline(cupContour);
                //op_holefill.UseAverageNormalsForTriangleVertices = true;
                op_holefill.ApplySimpleHolefillingIfFailed = true;
                //op_holefill.PreprocessInputPolylines = true;
                op_holefill.GridSize = 0.5;
                foreach (MDCK.Math.Objects.Polyline3D poly in polyLines)
                {
                    op_holefill.AddGuidelineCurve(poly);
                }

                // marking these triangles makes sure that we can copy them to a new model
                op_holefill.MarkNewTriangles = true;
                op_holefill.TreatAsOneHole = true;
                try
                {
                    op_holefill.Operate();

                    foreach (MDCK.Model.Objects.Surface theSurf in op_holefill.NewSurfaces)
                    {
                        mas.Model = outputModel;
                        mas.NewSurfaceName = "test";
                        mas.Operate();
                        MDCK.Model.Objects.Surface tempSurf = mas.NewSurface;
                        scts.SourceSurface = theSurf;
                        scts.DestinationSurface = tempSurf;
                        scts.Operate();
                    }
                }
                catch (MDCK.Operators.HoleFillReconstruction.Exception)
                {
                    outputModel.Dispose();
                    filledSkirtModel.Dispose();
                    filledModel.Dispose();
                    outputModel = null;
                    filledSkirtModel = null;
                    filledModel = null;
                    RhinoApp.WriteLine("[MDCK::Error] Could not hole fill.");
                    return false;
                }
            }

            // Read the stl in rhino
            success = MDCKConversion.MDCK2RhinoMeshStl(outputModel, out constructSurf);
            if (!success)
                return false;

            // Clean dispose
            outputModel.Dispose();
            filledSkirtModel.Dispose();
            filledModel.Dispose();
            outputModel = null;
            filledSkirtModel = null;
            filledModel = null;

            return true;
        }
    }
}