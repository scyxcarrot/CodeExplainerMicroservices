using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace IDS.Amace.Quality
{
    public class PlateAnalyzer
    {
        private Mesh sideSurface;
        private Mesh topSurface;
        private Mesh bottomSurface;
        private List<Mesh> ignoreEntities;
        private double sideSurfaceHeight;

        public PlateAnalyzer(Mesh topSurface, Mesh bottomSurface, Mesh sideSurface, List<Mesh> ignoreEntities, double sideSurfaceHeight)
        {
            this.topSurface = topSurface;
            this.bottomSurface = bottomSurface;
            this.sideSurface = sideSurface;
            this.ignoreEntities = ignoreEntities;
            this.sideSurfaceHeight = sideSurfaceHeight;
        }

        public bool IsUpToDate(Mesh topSurface, Mesh bottomSurface, Mesh sideSurface)
        {
            return this.topSurface.IsEqual(topSurface)
                    && this.bottomSurface.IsEqual(bottomSurface)
                    && this.sideSurface.IsEqual(sideSurface);
        }

        private List<List<Point3d>> GetSideSurfaceContourPoints()
        {
            // Get contour vertex indices
            List<int[]> contourIDXarrays = MeshUtilities.GetValidContours(sideSurface);
            // Convert to points
            List<List<Point3d>> points = new List<List<Point3d>>(contourIDXarrays.Count);
            for (int i = 0; i < contourIDXarrays.Count; i++)
            {
                points.Add(new List<Point3d>());
                foreach (int id in contourIDXarrays[i])
                {
                    points[i].Add(new Point3d(sideSurface.Vertices[id].X, sideSurface.Vertices[id].Y, sideSurface.Vertices[id].Z));
                }
            }

            return points;
        }

        private bool PointIsInsideIgnoreEntity(Point3d point)
        {
            double tolerance = 0.05;
            bool insideIgnore = false;

            foreach (Mesh ignoreEntity in ignoreEntities)
            {
                if (ignoreEntity != null && ignoreEntity.IsPointInside(point, tolerance, true))
                {
                    insideIgnore = true;
                    break;
                }
            }

            return insideIgnore;
        }

        private static int GetNearestMeshPoint(Mesh mesh, Point3d point, double maxDistance)//, out Point3d foundPoint)
        {
            MeshPoint meshPoint = mesh.ClosestMeshPoint(point, maxDistance);
            int pointIndex = -1;

            if (meshPoint.ComponentIndex.ComponentIndexType == ComponentIndexType.MeshTopologyVertex)
            {
                pointIndex = meshPoint.ComponentIndex.Index;
            }
            else if(meshPoint.ComponentIndex.ComponentIndexType == ComponentIndexType.MeshFace)
            {
                int tMaxIndex = meshPoint.T.ToList().IndexOf(meshPoint.T.Max());
                pointIndex = mesh.Faces[meshPoint.FaceIndex][tMaxIndex];
#if DEBUG
                IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "Nearest Mesh Point: Face index instead of mesh index found.");
#endif
            }
            else
            {
                throw new Exception("Component type is not mesh topology vertex or mesh face.");
            }

            Debug.Assert(pointIndex < mesh.Vertices.Count, "Index out of bounds", "{0:D} >= {1:D}", pointIndex, mesh.Vertices.Count);

            return pointIndex;
        }

        private static List<int> GetConnectedFaceIndices(Mesh mesh, int vertexIndex)
        {
            return mesh.TopologyVertices.ConnectedFaces(vertexIndex).ToList();
        }

        private static List<int> GetConnectedFaceIndices(Mesh mesh, List<int> vertexIndices)
        {
            // Get all faces for all points
            List<int> connectedFaces = new List<int>();
            foreach (int vertexIndex in vertexIndices)
            {
                connectedFaces.AddRange(GetConnectedFaceIndices(mesh, vertexIndex));
            }
            // Preserve unique faces only
            connectedFaces = connectedFaces.Distinct().ToList();

            return connectedFaces;
        }

        private static Vector3d GetAverageFaceNormal(List<int> faceIndices, Mesh mesh)
        {
            // Average triangle size
            double averageFaceArea = mesh.CalculateAverageFaceArea();

            // Init
            Vector3d normal = Vector3d.Zero;
            List<double> fAreas = new List<double>(faceIndices.Count);

            // Calculate all face areas
            foreach (int idx in faceIndices)
            {
                fAreas.Add(MeshUtilities.CalculateFaceArea(idx, mesh));
            }
            
            // If no face area is above the threshold, use the largest face available
            double threshold = averageFaceArea * 0.005;
            if (fAreas.Max() < threshold)
            {
                int indexMax = !fAreas.Any() ? -1 : fAreas.Select((value, index) => new { Value = value, Index = index })
                                                          .Aggregate((a, b) => (a.Value > b.Value) ? a : b)
                                                          .Index;
                normal = mesh.FaceNormals[faceIndices[indexMax]];
            }
            // Otherwise use average of all normals on faces with an area above the threshold.
            else
            {
                int count = 0;
                for (int i = 0; i < faceIndices.Count; i++)
                {
                    if (fAreas[i] > threshold)
                    {
                        normal += mesh.FaceNormals[faceIndices[i]];
                        count++;
                    }
                }
                normal /= count;
            }

            return normal;
        }

        public List<Tuple<Line, double>> GetSideSurfaceLinesAndAngles()
        {
                // Initialize linesAngles
                List<Tuple<Line, double>> linesAndAngles = new List<Tuple<Line, double>>();

                // Combine top and bottom
                Mesh topAndBottom = new Mesh();
                topAndBottom.Append(topSurface);
                topAndBottom.Append(bottomSurface);
                topAndBottom.FaceNormals.ComputeFaceNormals();

                // Side surface contour points
                List<List<Point3d>> points = GetSideSurfaceContourPoints();

                // Get side surface curves
                double segmentLength = 1;
                Polyline sideCurve0 = new Polyline(points[0]);
                Polyline sideCurve1 = new Polyline(points[1]);
                // Resample
                Polyline sampleCurve0 = CurveUtilities.ResamplePolyline(sideCurve0, segmentLength);
                Polyline sampleCurve1 = CurveUtilities.ResamplePolyline(sideCurve1, segmentLength);

                // For each resampled curve
                foreach (Polyline sampleCurve in new List<Polyline>() { sampleCurve0, sampleCurve1 })
                {
                    // Get the target curve (i.e. the other side surface curve)
                    Polyline actualCurve = sampleCurve == sampleCurve0 ? sideCurve0 : sideCurve1;
                    Polyline otherCurve = sampleCurve == sampleCurve0 ? sideCurve1 : sideCurve0;

                    // For each sample point on the resampled curve
                    foreach (Point3d samplePoint in sampleCurve)
                    {
                        // Check if the sample point is outside the ignore entities
                        if (PointIsInsideIgnoreEntity(samplePoint))
                        {
                            continue;
                        }

                        // Closest index on the actual (i.e. not resampled) curve
                        int closestIDXcurve = actualCurve.ClosestIndex(samplePoint);

                        // Get closest 2 points of the acutal curve point on the topAndBottom mesh
                        Point3d segmentStart = actualCurve.SegmentAt(closestIDXcurve % (actualCurve.Count - 1)).From;
                        Point3d segmentEnd = actualCurve.SegmentAt(closestIDXcurve % (actualCurve.Count - 1)).To;
                        double maxDistance = sideSurfaceHeight > 1 ? sideSurfaceHeight - 1 : sideSurfaceHeight;
                        int topBottomIndex1 = GetNearestMeshPoint(topAndBottom, segmentStart, maxDistance);
                        int topBottomIndex2 = GetNearestMeshPoint(topAndBottom, segmentEnd, maxDistance);

                        // Get all connected faces for both neighbours
                        List<int> connectedFaces = GetConnectedFaceIndices(topAndBottom, new List<int>() { topBottomIndex1, topBottomIndex2 });

                        // Get average of all faces that do not have a significant face area
                        Vector3d normalTopAndBottom = GetAverageFaceNormal(connectedFaces, topAndBottom);

                        // Calculate the side surface normal
                        Vector3d normalSide;
                        Point3d pointSide;
                        double maximumDistance = 1;
                        sideSurface.ClosestPoint(samplePoint, out pointSide, out normalSide, maximumDistance);

                        // Create line
                        Line line = new Line(pointSide, otherCurve.ClosestPoint(pointSide));

                        // Calculate angle between surface normal for top/bottom and side surfaces
                        normalTopAndBottom.Reverse();
                        double angle = normalTopAndBottom == Vector3d.Zero ? 0 : Vector3d.VectorAngle(normalTopAndBottom, normalSide) / Math.PI * 180;

                        // Add to list
                        linesAndAngles.Add(new Tuple<Line, double>(line, angle));
                    }
                }
                return linesAndAngles;
        }
    }
}