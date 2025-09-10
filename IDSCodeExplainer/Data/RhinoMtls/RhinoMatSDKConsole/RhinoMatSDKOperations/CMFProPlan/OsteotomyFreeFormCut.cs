using Materialise.SDK.MDCK.Model.Objects;
using System;
using System.Collections.Generic;
using System.Windows.Media.Media3D;
using MDCK = Materialise.SDK.MDCK;

namespace RhinoMatSDKOperations.CMFProPlan
{
    public class OsteotomyFreeFormCut : Osteotomy
    {
        public double ExtensionDistance { get; set; }

        public OsteotomyFreeFormCut()
        {
            ControlPoints = new List<Tuple<Point3D, Vector3D>>();
        }

        public override Model ReConstruct(TransformationInfo transformationInfo)
        {
            var model = new Model();

            var addCurve = new MDCK.Operators.ModelAddCurve();
            addCurve.IsClosed = IsClosed;
            addCurve.Model = model;
            addCurve.Operate();
            var newCurve = addCurve.NewCurve;

            for (int index = 0; index < ControlPoints.Count / 2; index++)
            {
                var addCurvePt = new MDCK.Operators.CurveAddVertexBack();
                addCurvePt.Curve = newCurve;
                addCurvePt.ModelVertex = ProplanUtilities.AddVertexPoint(ControlPoints[index].Item1, model);
                try
                {
                    addCurvePt.Operate();
                }
                catch (MDCK.Operators.CurveAddVertexBack.Exception ex)
                {
                    throw new Exception("Add curve point failed: " + ex.Message);
                }
                finally
                {
                    addCurvePt.Dispose();
                }
            }

            // Smooth the curve if you have more than 5 points indicated
            if (newCurve.NumberOfVertices > 5)
            {
                var smoothenPolyline = new MDCK.Operators.ModelPolylineSmoothen();
                smoothenPolyline.AddModelPolyline(newCurve);
                smoothenPolyline.SmoothenFactor = 0.5;
                smoothenPolyline.SmoothenIterations = 1;
                smoothenPolyline.TreatAsFreeCurve = false;
                smoothenPolyline.UseCompensation = true;
                smoothenPolyline.CheckForGeometricalError = true;
                smoothenPolyline.GeometricalError = 0.05;
                try
                {
                    smoothenPolyline.Operate();
                }
                catch (MDCK.Operators.ModelPolylineSmoothen.Exception ex)
                {
                    throw new Exception("Smoothen polyline failed: " + ex.Message);
                }
                finally
                {
                    smoothenPolyline.Dispose();
                }
            }

            var holeFillReconstruction = new MDCK.Operators.HoleFillReconstruction();
            holeFillReconstruction.GridSize = 0.25;
            holeFillReconstruction.NormalCalculationMethod = MDCK.Operators.HoleFillReconstruction.ENormalCalculationMethods.CONTOUR_DIRECTION;
            holeFillReconstruction.AddPolyline(newCurve);
            try
            {
                holeFillReconstruction.Operate();
            }
            catch (MDCK.Operators.HoleFillReconstruction.Exception ex)
            {
                throw new Exception("Hole fill reconstruction failed: " + ex.Message);
            }
            finally
            {
                holeFillReconstruction.Dispose();
            }


            var newSurface = holeFillReconstruction.NewSurfaces;
            // Calculate the extension
            if (ExtensionDistance > 0.0)
            {
                var surfaceExtend = new MDCK.Operators.SurfaceExtend();
                surfaceExtend.Offset = ExtensionDistance;
                surfaceExtend.NumberOfSegments = 5;
                surfaceExtend.SmoothingDistance = 0.2 * ExtensionDistance;
                surfaceExtend.CreateNewSurface = false;
                surfaceExtend.MoveNeighbourTriangles = false;
                surfaceExtend.AddPolyline(newCurve);
                try
                {
                    surfaceExtend.Operate();
                }
                catch (MDCK.Operators.SurfaceExtend.Exception ex)
                {
                    throw new Exception("Surface extend failed: " + ex.Message);
                }
                finally
                {
                    surfaceExtend.Dispose();
                }

                var reduceTriangles = new MDCK.Operators.Reduce();
                reduceTriangles.AddModel(model);
                try
                {
                    reduceTriangles.Operate();
                }
                catch (MDCK.Operators.Reduce.Exception ex)
                {
                    throw new Exception("Reduce triangles failed: " + ex.Message);
                }
                finally
                {
                    reduceTriangles.Dispose();
                }

                var offsetSurface = new MDCK.Operators.Offset();
                offsetSurface.AddModel(model, false);
                offsetSurface.OffsetDistance = Thickness;
                offsetSurface.LeaveOriginal = false;
                offsetSurface.Unify = true;
                offsetSurface.Solid = true;
                try
                {
                    offsetSurface.Operate();
                }
                catch (MDCK.Operators.Offset.Exception ex)
                {
                    throw new Exception("Offset failed: " + ex.Message);
                }
                finally
                {
                    offsetSurface.Dispose();
                }
            }

            return model;
        }
    }
}