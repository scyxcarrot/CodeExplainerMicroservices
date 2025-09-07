using IDS.Core.Utilities;
using Rhino;
using Rhino.Collections;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using RhinoMtlsCore.Operations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace IDS.Core.Operations
{
    /// <summary>
    /// MeshOperations provides functionality low level mesh operations
    /// </summary>
    public static class MeshOperations
    {
        public static Point3dList SampleCurve(Curve theCurve)
        {
            return SampleCurve(theCurve, 100);
        }

        /// <summary>
        /// Sample a curve into a Point3dList
        /// </summary>
        /// <param name="theCurve">The curve.</param>
        /// <param name="curveEdges">The curve edges.</param>
        /// <param name="numberOfPoints">The number of points.</param>
        /// <returns></returns>
        public static Point3dList SampleCurve(Curve theCurve, int numberOfPoints)
        {
            Point3dList curveEdges = new Point3dList();
            for (int i = 0; i < numberOfPoints; i++)
            {
                curveEdges.Add(theCurve.PointAtNormalizedLength((double)i / (double)numberOfPoints));
            }
            return curveEdges;
        }

        /// <summary>
        /// Split mesh with curve using MatSDK and return the largest and second largest patches.
        /// </summary>
        /// <param name="mesh">The mesh.</param>
        /// <param name="splitter">The splitter.</param>
        /// <param name="plate_patch">The plate patch.</param>
        /// <returns></returns>
        public static bool SplitMeshWithCurveMdck(Mesh mesh, Curve splitter, out Mesh plate_patch)
        {
            /// \todo This should be in MDCK operations
            // Cut out the patch
            plate_patch = null;
            List<Mesh> patches;
            var res = SplitWithCurve.OperatorSplitWithCurve(mesh, new []{ splitter }, true, 100, 0.05, out patches);
            if (!res || patches.Count < 1 || patches.Count > 2)
            {
                return false;
            }

            // TODO: this selection of the final patch seems to work, but this is a very non-robuust buildup
            // Find the patch that corresponds to the one we need
            Point3d curve_centroid = CurveUtilities.GetCurveCentroid(splitter, 1.0);
            List<AreaMassProperties> patch_props = new List<AreaMassProperties>(patches.Count);
            for (int i = 0; i < patches.Count; i++)
            {
                patch_props.Add(AreaMassProperties.Compute(patches[i]));
            }
            double[] patch_areas = patch_props.Select(am => am.Area).ToArray();

            // First check if the one with closest centroid has reasonable area
            double[] centroid_diffs = patch_props.Select(am => (am.Centroid - curve_centroid).Length).ToArray();
            int closest_centroid = Array.IndexOf(centroid_diffs, centroid_diffs.Min());
            double threshold_area = 400;
            if (patch_props[closest_centroid].Area >= threshold_area)
            {
                plate_patch = patches[closest_centroid];
                return true;
            }

            //temporary implementation only for demostrator 2 !!! need revisit!!
            // If not, use area of patches
            int[] indices = Enumerable.Range(0, patch_areas.Length).ToArray();
            Array.Sort(patch_areas, indices);

            plate_patch = patches[indices[0]]; // Return second-largest patch
            return true;
        }

        /// <summary>
        /// Stitch together two contours by matching vertices according to the nearest neighbor criterion.
        /// </summary>
        /// <param name="topEdges">The top edges.</param>
        /// <param name="bottomEdges">The bottom edges.</param>
        /// <param name="stitched">The stitched.</param>
        /// <param name="topContourIdx">Index of the top contour.</param>
        /// <param name="bottomContourIdx">Index of the bottom contour.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Could not find reciprocal NN points in border!</exception>
        public static bool StitchContoursNn(Point3dList topEdges, Point3dList bottomEdges, out Mesh stitched, out List<int> topContourIdx, out List<int> bottomContourIdx)
        {
            // init stitched mesh
            stitched = new Mesh();

            // Add vertices of topEdges to stitched.Vertices and add indices to topContourIdx
            topContourIdx = new List<int>();
            foreach (var theVertex in topEdges.Select((x, i) => new { Value = x, Index = i }))
            {
                stitched.Vertices.Add(theVertex.Value);
                topContourIdx.Add(theVertex.Index);
            }
            // Add vertices of bottomEdges to stitched.Vertices and add indices to bottomContourIdx
            bottomContourIdx = new List<int>();
            foreach (var theVertex in bottomEdges.Select((x, i) => new { Value = x, Index = i }))
            {
                stitched.Vertices.Add(theVertex.Value);
                bottomContourIdx.Add(theVertex.Index + topEdges.Count); // start counting from topEdges.Count()
            }

            // k-NN search in Rhino

            // for each vert in B1, find index of NN in B2
            int[] b1_nn2 = topEdges.Select(p => bottomEdges.ClosestIndex(p)).ToArray();
            // for each vert in B2, find index of NN in B1
            int[] b2_nn1 = bottomEdges.Select(p => topEdges.ClosestIndex(p)).ToArray();
            // Distances between NN
            double[] b1_dnn2 = b1_nn2.Select((ib2, ib1) => (topEdges[ib1] - bottomEdges[ib2]).Length).ToArray();

            // Find indices of points that "agree" (reciprocal NN) Compute indices in B1 where the NN
            // is reciprocal
            int[] b1_recip_idx = b1_nn2.Select((ib2, ib1) => new { IB1 = ib1, NN2 = ib2 })
                              .Where(x => b2_nn1[x.NN2] == x.IB1) // Reciprocal relation
                              .Select(x => x.IB1).ToArray(); // Get the index in idx1
            if (b1_recip_idx.Length < 1)
            {
                throw new ArgumentException("Could not find reciprocal NN points in border!");
            }
            // now (b1_recip, b2_recip) form pairs of reciprocal NN in B1 and B2

            // Find closest agreeing points
            double[] dagree = b1_recip_idx.Select(i => b1_dnn2[i]).ToArray();
            int closest = Array.IndexOf(dagree, dagree.Min());

            // Re-order contours (indices into Vedges) Vedges are ordered according to initial
            // contour order
            int b1_closest_idx = b1_recip_idx[closest];
            int[] cont1_a = Enumerable.Range(b1_closest_idx, topEdges.Count - b1_closest_idx).ToArray();
            int[] cont1_b = Enumerable.Range(0, b1_closest_idx).ToArray();
            int[] cont1 = cont1_a.Concat(cont1_b).ToArray();

            int b2_closest_idx = b1_nn2[b1_closest_idx];
            int[] cont2_a = Enumerable.Range(b2_closest_idx, bottomEdges.Count - b2_closest_idx).ToArray();
            int[] cont2_b = Enumerable.Range(0, b2_closest_idx).ToArray();
            int[] cont2 = cont2_a.Concat(cont2_b).ToArray();

            // Match contour directions
            Point3d[] Vtemp1 = cont1.Select(i => topEdges[i]).ToArray();
            Point3d[] Vtemp2 = cont2.Select(i => bottomEdges[i]).ToArray();
            Point3d[] Vinterp2;
            if (cont1.Length > cont2.Length)
            {
                PolylineCurve interpcurve = new PolylineCurve(Vtemp2);
                interpcurve.DivideByCount(Vtemp1.Length - 1, true, out Vinterp2);
            }
            else if (cont1.Length < cont2.Length)
            {
                PolylineCurve interpcurve = new PolylineCurve(Vtemp1);
                interpcurve.DivideByCount(Vtemp2.Length - 1, true, out Vinterp2); // NOTE: number of points is number of segments minus one
                Vtemp1 = Vtemp2;
            }
            else
            {
                Vinterp2 = Vtemp2;
            }
            Debug.Assert(Vtemp1.Length == Vinterp2.Length, "Vertex arrays are not of same length!");

            double[] d_normal = Vtemp1.Zip(Vinterp2, (p1, p2) => (p1 - p2).Length).ToArray();
            double[] d_reverse = Vtemp1.Reverse().Zip(Vinterp2, (p1, p2) => (p1 - p2).Length).ToArray();
            if (d_normal.Sum() > d_reverse.Sum())
            {
                cont2 = cont2.Take(1).Concat(cont2.Skip(1).Reverse()).ToArray();
            }
            // Note: this is the same as cont2 = cont2.Reverse().ToArray(); and then shifting by one
            //       so that the first element remains in place

            // NOTE: [top_contour & bot_contour] are vertex indices into mesh with original contour ordering
            // - [topEdges & bottomEdges] have same ordering [cont1 & cont2] are indices into
            // top_contour & bot_contour that can be circularly shifted and/or flipped

            // Walk over both contours and make the triangles
            int j = 0; // counter for B1 (top)
            int k = 0; // counter for B2 (bottom)
            int j_last = topEdges.Count - 1; // Last valid index in first contour
            int k_last = bottomEdges.Count - 1; // Last valid index in second contour

            while (true)
            {
                if (j > j_last && k > k_last)
                {
                    break; // Case 1: both borders have crossed the end
                }

                // Adjust indices for end-crossing
                int j_cur = j > j_last ? 0 : j;
                int k_cur = k > k_last ? 0 : k;
                int j_next = j + 1 > j_last ? 0 : j + 1;
                int k_next = k + 1 > k_last ? 0 : k + 1;
                bool k_step = false; // Step along k-border or j-border

                // Remaining cases: either one or none of the indices has crossed the end
                if (j <= j_last && k <= k_last)
                {
                    // Evaluate which of two alternative crossing edges is shortest
                    double e1 = (topEdges[cont1[j_cur]] - bottomEdges[cont2[k_next]]).Length;
                    double e2 = (bottomEdges[cont2[k_cur]] - topEdges[cont1[j_next]]).Length;
                    k_step = e1 < e2;
                }
                else if (j > j_last && k <= k_last)
                {
                    // Only j has crossed: step on k-border and keep j fixed
                    k_step = true;
                }
                else if (j <= j_last && k > k_last)
                {
                    // Only k has corssed: step on j-border and keep k fixed
                    k_step = false;
                }

                // Make the step based on decision
                if (k_step)
                {
                    int a = topContourIdx[cont1[j_cur]];
                    int b = bottomContourIdx[cont2[k_cur]];
                    int c = bottomContourIdx[cont2[k_next]];
                    stitched.Faces.AddFace(a, b, c);
                    k++;
                }
                else // j_step
                {
                    int a = topContourIdx[cont1[j_cur]];
                    int b = bottomContourIdx[cont2[k_cur]];
                    int c = topContourIdx[cont1[j_next]];
                    stitched.Faces.AddFace(a, b, c);
                    j++;
                }
            } // end while

            return true;
        }

        public static bool StitchCurves(Curve topCurve, Curve bottomCurve, out Mesh stitched,
            out List<int> topContourIdx, out List<int> bottomContourIdx)
        {
            return StitchCurves(topCurve, bottomCurve, out stitched, out topContourIdx, out bottomContourIdx, 100);
        }

        /// <summary>
        /// Create a stitching surface between 2 closed curves
        /// </summary>
        /// <param name="topCurve">The top curve.</param>
        /// <param name="bottomCurve">The bottom curve.</param>
        /// <param name="stitched">The stitched.</param>
        /// <param name="topContourIdx">Index of the top contour.</param>
        /// <param name="bottomContourIdx">Index of the bottom contour.</param>
        /// <param name="numberOfPoints">The number of points.</param>
        /// <returns></returns>
        public static bool StitchCurves(Curve topCurve, Curve bottomCurve, out Mesh stitched, out List<int> topContourIdx,
            out List<int> bottomContourIdx, int numberOfPoints)
        {
            // init
            stitched = new Mesh();
            topContourIdx = new List<int>();
            bottomContourIdx = new List<int>();
            bool success;

            // Sample top curve
            Point3dList topEdges = SampleCurve(topCurve, numberOfPoints: numberOfPoints);

            // Sample top curve
            Point3dList bottomEdges = SampleCurve(bottomCurve, numberOfPoints: numberOfPoints);

            // Stich curves
            success = StitchContoursNn(topEdges, bottomEdges, out stitched, out topContourIdx, out bottomContourIdx);
            if (!success)
            {
                return false;
            }

            // success
            return true;
        }

        public static bool StitchCurvesAndFillTop(Curve topCurve, Curve bottomCurve, out Mesh stitched, out Mesh filled)
        {
            return StitchCurvesAndFillTop(topCurve, bottomCurve, out stitched, out filled, 100);
        }

        /// <summary>
        /// Create a stitching surface between 2 closed curves and fill the top
        /// </summary>
        /// <param name="topCurve">The top curve.</param>
        /// <param name="bottomCurve">The bottom curve.</param>
        /// <param name="stitched">The stitched.</param>
        /// <param name="filled">The filled.</param>
        /// <param name="numberOfPoints">The number of points.</param>
        /// <returns></returns>
        public static bool StitchCurvesAndFillTop(Curve topCurve, Curve bottomCurve, out Mesh stitched, out Mesh filled, int numberOfPoints)
        {
            // init
            stitched = new Mesh();
            filled = new Mesh();
            bool success;

            // Stitch the curves
            List<int> topContourIdx;
            List<int> bottomContourIdx;
            success = StitchCurves(topCurve, bottomCurve, out stitched, out topContourIdx, out bottomContourIdx, numberOfPoints: numberOfPoints);
            if (!success)
            {
                return false;
            }

            long[,] segment;
            var foundSegment = HoleFill.FindBorderVertexHoleSegments(stitched, (long)topContourIdx[0], out segment);
            if (!foundSegment)
            {
                return false;
            }

            // Fill the top
            bool res = HoleFill.PerformNormalHoleFill(stitched, segment, out filled);
            if (!res)
            {
                return false;
            }

            return true;
        }

        public static Mesh StitchMeshSurfaces(Mesh top, Mesh bottom)
        {
            return StitchMeshSurfaces(top, bottom, true);
        }

        /// <summary>
        /// Create a stitching surface between 2 meshes with an each only 1 open contour
        /// </summary>
        /// <param name="top">The top.</param>
        /// <param name="bottom">The bottom.</param>
        /// <param name="stitched">The stitched.</param>
        /// <param name="combine">if set to <c>true</c> [combine].</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">
        /// Top mesh has more than one valid contour!
        /// or
        /// Bottom mesh has more than one valid contour!
        /// </exception>
        public static Mesh StitchMeshSurfaces(Mesh top, Mesh bottom, bool combine)
        {
            // Get first contour
            List<int[]> topContours = MeshUtilities.GetValidContours(top, duplast: false, raiseIfInvalid: true);
            if (topContours.Count != 1)
            {
                throw new ArgumentException("Top mesh has more than one valid contour!");
            }
            List<int> topContourIdx = topContours[0].ToList();

            // Get second contour
            List<int[]> bottomContours = MeshUtilities.GetValidContours(bottom, duplast: false, raiseIfInvalid: true);
            if (bottomContours.Count != 1)
            {
                throw new ArgumentException("Bottom mesh has more than one valid contour!");
            }
            List<int> bottomContourIdx = bottomContours[0].ToList();

            // Calculate edges list of bottom and top contour
            Point3dList topEdges = new Point3dList(topContourIdx.Select(idx => (Point3d)top.Vertices[idx]).ToArray());
            Point3dList bottomEdges = new Point3dList(bottomContourIdx.Select(idx => (Point3d)bottom.Vertices[idx]).ToArray());

            // Call function that calculate the stitched mesh between the 2 contours and update
            // topContourIdx and bottomContourIdx
            Mesh stitched;
            StitchContoursNn(topEdges, bottomEdges, out stitched, out topContourIdx, out bottomContourIdx);

            // combine side, bottom and top in one mesh
            if (combine)
            {
                stitched.Append(top);
                stitched.Append(bottom);
            }

            // success
            return stitched;
        }

        public static List<Mesh> SplitMeshWithCurves(Mesh inMesh, List<Curve> curves)
        {
            return SplitMeshWithCurves(inMesh, curves, true);
        }

        public static List<Mesh> SplitMeshWithCurves(Mesh inMesh, List<Curve> curves, bool sortSmallestToLargest, bool returnOpenMesh = false)
        {
            List<Mesh> parts;

            var fixedCurves = FixCurveForSplittingOperation(curves);

            if (!SplitWithCurve.OperatorSplitWithCurve(inMesh, fixedCurves.ToArray(), true, 100, 0.05, out parts) || parts.Count < 2)
            {
                return null;
            }

            if (returnOpenMesh)
            {
                parts = parts.Where(part => !part.IsClosed).ToList();
            }

            var orderedParts = MeshUtilities.SortMeshBySurfaceArea(parts.ToArray()).ToList();
            if (!sortSmallestToLargest)
            {
                orderedParts.Reverse();
            }

            return orderedParts;
        }

        public static List<Mesh> SplitMeshWithCurves(Mesh inMesh, List<Curve> curves, bool useRhinoPullCurveToMesh, double maxChordLengthRatio, double maxGeometricalError, bool sortSmallestToLargest)
        {
            List<Mesh> parts;

            //TODO: Fix curve, not doing it now because might break Implant
            if (!SplitWithCurve.OperatorSplitWithCurve(inMesh, curves.ToArray(), useRhinoPullCurveToMesh, maxChordLengthRatio, maxGeometricalError, out parts) || parts.Count < 2)
            {
                return null;
            }

            var orderedParts = MeshUtilities.SortMeshBySurfaceArea(parts.ToArray()).ToList();
            if (!sortSmallestToLargest)
            {
                orderedParts.Reverse();
            }

            return orderedParts;
        }

        public static List<int> GetLongestContour(Mesh mesh, bool raiseExceptionIfInvalid = true)
        {
            var contours = MeshUtilities.GetValidContours(mesh, duplast: false, raiseIfInvalid: raiseExceptionIfInvalid);
            int[] contourIdx = null;

            var longestContourLength = 0D;
            foreach (var contour in contours)
            {
                if (contour.Length < 2)
                {
                    continue;
                }

                var edge = Curve.CreateControlPointCurve(contour.Select(idx => (Point3d)mesh.Vertices[idx]).ToArray());
                var length = edge.GetLength();
                if (length <= longestContourLength)
                {
                    continue;
                }
                longestContourLength = length;
                contourIdx = contour;
            }

            return contourIdx?.ToList();
        }

        /**
         * Compute the bottom surface by first hole filling the top,
         * remeshing the newly craeted surface (filled hole), and TPS
         * transforming using landmarks in the defect.
         */

        public static bool ComputeBottomHoleFill(RhinoDoc doc, Vector3d holeFillVector, Vector3d shootDir, double additionalOffset, Mesh support, Mesh top, out Mesh bottomSurface)
        {
            // Variables
            bottomSurface = null;
            
            Mesh[] fillerSurfaces;

            // Fill hole
            //var viewVec = new Vector3D(holeFillVector.X, holeFillVector.Y, holeFillVector.Z);
            //var opparams = new MDCKHoleFillFreeformParameters(viewVec, GridSize: 2.0);
            //var successHoleFill = MDCKHoleFillFreeform.HoleFillFreeformOperationStl(top, opparams, out filler);
            
            var tempTop = top.DuplicateMesh();
            tempTop.Faces.ConvertQuadsToTriangles();
            tempTop.Normals.ComputeNormals();
            tempTop.Compact();
            tempTop.Normals.ComputeNormals();
            var segment = Edges.GetEdgeIndices(tempTop);
            Mesh dummy;
            var successHoleFill = HoleFill.PerformFreeformHoleFill(tempTop, segment, false, true, 2.0, out dummy, out fillerSurfaces);
            if (!successHoleFill)
            {
                return false;
            }

            // Find matching points
            ///////////////////////
            //index 0 is the input, which in this case is tempTop.
            var filteredSurf = fillerSurfaces.ToList();
            filteredSurf.RemoveAt(0);
            var filler = MeshUtilities.AppendMeshes(filteredSurf);

            // Init
            support.FaceNormals.ComputeFaceNormals();
            var isEdgeVertex = filler.GetNakedEdgePointStatus();
            var corrPoints = new List<Point3d>(isEdgeVertex.Count());
            var hitIdx = new List<int>();

            // Compute landmarks for interior vertices
            int edgeSkip = 5;
            int edge = 0;
            for (var vIdx = 0; vIdx < filler.Vertices.Count; vIdx++)
            {
                Point3d pt = filler.Vertices[vIdx];

                // If border vertex: fix point
                if (isEdgeVertex[vIdx])
                {
                    edge++;
                    if (edge == edgeSkip)
                    {
                        corrPoints.Add(filler.Vertices[vIdx]);
                        hitIdx.Add(vIdx);
                        edge = 0;
                    }
                    continue;
                }

                // Shoot ray
                var ray = new Ray3d(pt, shootDir);
                int[] hitFaces;
                var firstHit = Intersection.MeshRay(support, ray, out hitFaces);
                if (firstHit < 0) // No intersection found
                {
                    continue;
                }

                // Check if hit occured inside or outside mesh
                var noSupport = false;
                foreach (var fid in hitFaces)
                {
                    var norm = new Vector3d(support.FaceNormals[fid]);
                    if (shootDir * norm > -0.5)
                    {
                        noSupport = true;
                        break;
                    }
                }
                if (noSupport)
                {
                    continue;
                }

                // Store the intersection point
                corrPoints.Add(pt + shootDir * (firstHit + additionalOffset));
                hitIdx.Add(vIdx);
            }

            // Perform TPS transformation
            /////////////////////////////
            try
            {
                var bottom = MakeScaffoldBottom.TpsTransformRayIntersections(filler, hitIdx.ToArray(), corrPoints.ToArray());
                bottomSurface = bottom;
                // Move all edge vertices to their original position
                for (var i = 0; i < bottomSurface.Vertices.Count; i++)
                {
                    if (isEdgeVertex[i])
                    {
                        bottomSurface.Vertices[i] = filler.Vertices[i];
                    }
                }
            }
            catch (Exception exc)
            {
                RhinoApp.WriteLine("Exception thrown during execution of python script: \n" + exc.ToString());
                return false;
            }

            return true;
        }

        private static List<Curve> FixCurveForSplittingOperation(List<Curve> curves)
        {
            var fixedCurves = new List<Curve>();
            var filteredCurves = CurveUtilities.FilterNoiseCurves(curves);

            filteredCurves.ForEach(x =>
            {
                var fixedCurve = CurveUtilities.FixCurve(x);
                fixedCurves.Add(fixedCurve);
            });

            return fixedCurves;
        }

        private static void WriteToFile(Point3d[] vertices, string filename)
        {
            var path = $@"c:\TPSTEST\{filename}.txt";

            var str = new List<string>() { "[" };
            foreach (var v in vertices)
            {
                var onestr = "[";
                onestr += v.X.ToString();
                onestr += ",";
                onestr += v.Y.ToString();
                onestr += ",";
                onestr += v.Z.ToString();
                onestr += "]";

                if (v != vertices.Last())
                {
                    onestr += ",";
                }
                str.Add(onestr);
            }
            str.Add("]");

            File.WriteAllText(path, string.Join("", str), Encoding.UTF8);
        }

        private static void WriteToFile(int[] hits)
        {
            var path = @"c:\TPSTEST\hits.txt";

            var str = new List<string>() { "[" };
            foreach (var v in hits)
            {
                var onestr = v.ToString();

                if (v != hits.Last())
                {
                    onestr += ",";
                }
                str.Add(onestr);
            }
            str.Add("]");

            File.WriteAllText(path, string.Join("", str), Encoding.UTF8);
        }
    }
}