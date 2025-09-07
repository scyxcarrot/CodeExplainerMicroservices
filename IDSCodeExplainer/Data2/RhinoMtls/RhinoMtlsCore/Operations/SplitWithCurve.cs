using MtlsIds34.Array;
using Rhino;
using Rhino.Geometry;
using RhinoMtlsCore.Common;
using RhinoMtlsCore.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;

namespace RhinoMtlsCore.Operations
{
    public class SplitWithCurve
    {
        /// <summary>
        /// Operator that splits a mesh with a curve. Splitting is done by attracting the mesh to the curve, creating new
        /// triangles, and subsequently separating the mesh along the created border (sequence of triangle edges).
        /// </summary>
        /// <param name="inputMesh">The inmesh.</param>
        /// <param name="curves">The curves.</param>
        /// <param name="useRhinoPullToMesh">if set to <c>true</c> [use rhino pull to mesh] before passing the curve to the operator.</param>
        /// <param name="maxChordLengthRatio">The maximum chord length ratio.</param>
        /// <param name="maxGeometricalError">The maximum geometrical error.</param>
        /// <param name="parts">The parts.</param>
        /// <returns></returns>
        public static bool OperatorSplitWithCurve(Mesh inputMesh, Curve[] curves, bool useRhinoPullToMesh, double maxChordLengthRatio, double maxGeometricalError, out List<Mesh> parts)
        {
            parts = new List<Mesh>() {inputMesh};

            if (!AreAllCurvesClosed(curves))
            {
                RhinoApp.WriteLine("Not all curves are closed.");
                return false;
            }

            // MatSDK does not support quads
            if (inputMesh.Faces.QuadCount > 0)
            {
                inputMesh.Faces.ConvertQuadsToTriangles();
            }

            var splittingCurves = new Curve[curves.Length];
            if (useRhinoPullToMesh)
            {
                PullCurvesToMesh(inputMesh, curves, out splittingCurves);
            }
            else
            {
                curves.CopyTo(splittingCurves, 0);
            }
            
            var curvesPointArray = new double[0, 0];
            var curvesSegmentArray = new double[0, 0];
            foreach (var splittingCurve in splittingCurves)
            {
                var curvePointArray = splittingCurve.ToDouble2DArray(maxChordLengthRatio, maxGeometricalError);
                var curveSegmentArray = Curves.PopulateSegments(curvePointArray.GetLength(0), curvesPointArray.GetLength(0));

                var pointArray = new double[curvesPointArray.GetLength(0) + curvePointArray.GetLength(0), 3];
                Array.Copy(curvesPointArray, pointArray, curvesPointArray.Length);
                Array.Copy(curvePointArray, 0, pointArray, curvesPointArray.Length, curvePointArray.Length);
                curvesPointArray = pointArray;
                
                var segmentArray = new double[curvesSegmentArray.GetLength(0) + curveSegmentArray.GetLength(0), 2];
                Array.Copy(curvesSegmentArray, segmentArray, curvesSegmentArray.Length);
                Array.Copy(curveSegmentArray, 0, segmentArray, curvesSegmentArray.Length, curveSegmentArray.Length);
                curvesSegmentArray = segmentArray;
            }
            
            parts = SplitWithCurveArray(inputMesh, curvesPointArray, curvesSegmentArray);

            return true;
        }

        private static bool AreAllCurvesClosed(Curve[] curves)
        {
            return curves.All(curve => curve.IsClosed);
        }

        private static void PullCurvesToMesh(Mesh inmesh, Curve[] splittingCurves, out Curve[] pulledCurves)
        {
            List<Curve> pulledCurvesList;
            PullCurvesToMesh(inmesh, splittingCurves, out pulledCurvesList);

            pulledCurves = pulledCurvesList.ToArray();
        }

        private static void PullCurvesToMesh(Mesh inmesh, Curve[] splittingCurves, out List<Curve> pulledCurves)
        {
            pulledCurves = new List<Curve>();
            foreach (var curve in splittingCurves)
            {
                var pulledCurve = curve.PullToMesh(inmesh, 0.01);
                if (pulledCurve != null)
                {
                    pulledCurves.Add(pulledCurve);
                }
                else
                {
                    return;
                }
            }
        }

        [HandleProcessCorruptedStateExceptions]
        private static List<Mesh> SplitWithCurveArray(Mesh inputMesh, double[,] curvePointArray, double[,] curveSegmentArray)
        {
            var parts = new List<Mesh>();

            using (var context = MtlsIds34Globals.CreateContext())
            {
                // Split by curve operator
                var splitByCurve = new MtlsIds34.MeshDesign.SplitByCurve();
                splitByCurve.Triangles = inputMesh.Faces.ToArray2D(context);
                splitByCurve.Vertices = inputMesh.Vertices.ToArray2D(context);
                splitByCurve.CurvePoints = Array2D.Create(context, curvePointArray);
                splitByCurve.CurveSegments = Array2D.Create(context, curveSegmentArray);

                try
                {
                    var result = splitByCurve.Operate(context);

                    var vertexArray = result.Vertices.ToDouble2DArray();
                    var triangleArray = result.Triangles.ToUint64Array();

                    // Extract results
                    // Culling triangles in MakeRhinoMesh would cause an exception where splitStructure is mismatched with the number of triangles in resultMesh
                    var resultMesh = MeshUtilities.MakeRhinoMesh(vertexArray, triangleArray, false);
                    var splitStructure = (ulong[])result.SplitStructure.Data;
                    var surfaceIndices = splitStructure.Distinct().ToArray();

                    // Convert every patch/surface to a Rhino mesh

                    parts.AddRange(surfaceIndices.Select(surfaceIndex => MeshUtilities.GetSubSurface(resultMesh, splitStructure, surfaceIndex)));

                    resultMesh.Dispose();

                    return parts;
                }
                catch (Exception e)
                {
                    throw new MtlsException("SplitByCurve", e.Message);
                }
            }
        }
    }
}