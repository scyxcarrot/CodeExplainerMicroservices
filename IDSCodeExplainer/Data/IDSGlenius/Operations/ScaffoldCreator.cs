using IDS.Core.Operations;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Collections;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Glenius.Operations
{
    //TODO: Make each parts as component
    public class ScaffoldCreator
    {
        public Mesh ScaffoldTop { get; set; }
        public Mesh ScaffoldSupport { get; set; }
        public Mesh ScaffoldSide { get; set; }
        public Mesh ScaffoldBottom { get; set; }

        public string ErrorMessage { get; private set; }

        private readonly double tolerance = 0.01;

        public bool CreateTop(Curve curve)
        {
            ScaffoldTop = null;

            if (curve == null || !curve.IsClosed)
            {
                return false;
            }

            var meshingParameters = MeshParameters.IDS();
            var mesh = Mesh.CreateFromPlanarBoundary(curve, meshingParameters);
            if (mesh == null)
            {
                return false;
            }

            mesh.UnifyNormals();
            if (!mesh.Normals.ComputeNormals())
            {
                return false;
            }

            ScaffoldTop = mesh;
            return true;
        }

        public bool CreateSupport(Curve primaryBorder, Curve[] secondaryBorders, Mesh scapulaDesignReamed)
        {
            ScaffoldSupport = null;
            ErrorMessage = string.Empty;

            if (primaryBorder != null && primaryBorder.IsClosed && scapulaDesignReamed != null)
            {
                var inMesh = scapulaDesignReamed.DuplicateMesh();
                var holeBorders = new List<Curve>();
                if (secondaryBorders != null)
                {
                    holeBorders = secondaryBorders.ToList();
                    var pulledPrimaryCurve = primaryBorder.PullToMesh(inMesh, tolerance);
                    foreach (var border in secondaryBorders)
                    {
                        var pulledCurve = border.PullToMesh(inMesh, tolerance);
                        var intersection = Intersection.CurveCurve(pulledPrimaryCurve, pulledCurve, tolerance, tolerance);
                        if (intersection.Count > 0)
                        {
                            ErrorMessage =
                                "Secondary Border intersection with Primary Border detected. Please prevent the intersection of this two curve types.";
                            return false;
                        }

                        var parts = MeshOperations.SplitMeshWithCurves(inMesh, new List<Curve> {border});
                        if (parts == null)
                        {
                            continue;
                        }

                        var largestMesh = parts.Last();
                        holeBorders.Remove(border);
                        inMesh = largestMesh;
                    }
                }

                var finalParts = SplitMeshWithPrimaryCurve(inMesh, primaryBorder, holeBorders);
                if (finalParts != null && finalParts.Count > 0)
                {
                    ScaffoldSupport = finalParts.First();
                }
            }

            var successful = ScaffoldSupport != null;
            if (!successful)
            {
                ErrorMessage =
                    "Scaffold Support cannot be created. Please inspect the Primary Border and consider to add Secondary Borders.";
            }

            return successful;
        }

        private bool CreateSide(Curve bottomCurve)
        {
            ScaffoldSide = null;

            if (ScaffoldTop == null || bottomCurve == null)
            {
                return false;
            }

            var topContourIdx = MeshOperations.GetLongestContour(ScaffoldTop);
            List<int> supportContourIdx;
            var topEdges = new Point3dList(topContourIdx.Select(idx => (Point3d)ScaffoldTop.Vertices[idx]).ToArray());
            var supportEdges = MeshOperations.SampleCurve(bottomCurve, numberOfPoints: 200);

            Mesh mesh;
            if (!MeshOperations.StitchContoursNn(topEdges, supportEdges, out mesh, out topContourIdx,
                    out supportContourIdx) || mesh == null)
            {
                return false;
            }

            mesh.UnifyNormals();
            if (!mesh.Normals.ComputeNormals())
            {
                return false;
            }

            ScaffoldSide = mesh;
            return true;
        }

        public bool CreateSideWithGuides(List<RhinoObject> guides, RhinoDoc doc, RhinoObject topCurve, RhinoObject bottomCurve)
        {
            if (guides == null || !guides.Any())
            {
                return CreateSide(bottomCurve.Geometry as Curve);
            }

            ScaffoldSide = null;
            
            Locking.UnlockScaffoldCurves(doc);
            var mesh = BrepUtilities.CreateSweepMesh(doc, topCurve, bottomCurve, guides, false);
            Locking.LockAll(doc);
            if (mesh == null)
            {
                return false;
            }

            ScaffoldSide = mesh;
            return true;
        }

        public bool CreateBottom(RhinoDoc doc, Vector3d bottomDir)
        {
            ScaffoldBottom = null;

            if (ScaffoldTop == null || ScaffoldSupport == null || ScaffoldSide == null)
            {
                return false;
            }

            var support = ScaffoldSupport.DuplicateMesh();
            var mergedMesh = ScaffoldSide.DuplicateMesh();
            mergedMesh.Append(ScaffoldTop.DuplicateMesh());
            Mesh mesh;
            const double additionalOffset = 1.0;
            if (!MeshOperations.ComputeBottomHoleFill(doc, bottomDir, bottomDir, additionalOffset, support, mergedMesh, out mesh))
            {
                return false;
            }

            ScaffoldBottom = mesh;
            return true;
        }

        public bool CreateAll(RhinoObject topCurveObj, RhinoObject primarySupportBorderObj, Curve[] secondarySupportBorders, Mesh scapulaDesignReamed, RhinoDoc doc, Vector3d bottomDir, List<RhinoObject> guides)
        {
            ErrorMessage = string.Empty;

            var topCurve = topCurveObj.Geometry as Curve;
            if (primarySupportBorderObj != null)
            {
                var primarySupportBorder = primarySupportBorderObj.Geometry as Curve;
                if (CreateSupport(primarySupportBorder, secondarySupportBorders, scapulaDesignReamed) &&
                    CreateTop(topCurve) && CreateSideWithGuides(guides, doc, topCurveObj, primarySupportBorderObj) &&
                    CreateBottom(doc, bottomDir))
                {
                    return true;
                }
            }
            else
            {
                ErrorMessage =
                    "Primary Border is missing. Please add it.";
            }

            ScaffoldTop = null;
            ScaffoldSupport = null;
            ScaffoldSide = null;
            ScaffoldBottom = null;
            return false;
        }

        private List<Mesh> SplitMeshWithPrimaryCurve(Mesh inMesh, Curve primaryCurve, List<Curve> secondaryCurves)
        {
            var splittingCurves = new List<Curve> { primaryCurve };
            if (secondaryCurves != null)
            {
                splittingCurves.AddRange(secondaryCurves);
            }

            var parts = MeshOperations.SplitMeshWithCurves(inMesh, splittingCurves);
            if (parts != null)
            {
                //remove the largest
                parts.RemoveAt(parts.Count - 1);

                var pulledPrimaryCurve = primaryCurve.PullToMesh(inMesh, 0.01);
                var primaryCurveLength = pulledPrimaryCurve.GetLength();
                parts = parts.OrderBy(part => GetLongestContourLengthDifference(part, primaryCurveLength)).ToList();
            }

            return parts;
        }

        private double GetLongestContourLengthDifference(Mesh part, double compareLength)
        {
            var longestContour = MeshOperations.GetLongestContour(part);
            var edge = Curve.CreateControlPointCurve(longestContour.Select(idx => (Point3d) part.Vertices[idx])
                    .ToArray());
            var length = edge.GetLength();
            var diff = Math.Abs(length - compareLength);
            return diff;
        }
    }
}