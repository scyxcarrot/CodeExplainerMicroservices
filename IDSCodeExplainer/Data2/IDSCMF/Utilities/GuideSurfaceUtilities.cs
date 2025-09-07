using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace IDS.CMF.Utilities
{
    public static class GuideSurfaceUtilities
    {
        public static Mesh CreatePatch(Mesh tube, Mesh drawingBase, bool justOuter)
        {
            var intersectionCurves = MeshIntersectionCurve.IntersectionCurve(tube, drawingBase);
            var curves = intersectionCurves.OrderBy(x => x.GetLength()).ToList();
            curves.ForEach(x =>
            {
                if (!x.IsClosed)
                {
                    x.MakeClosed(2);
                }
            });

            return SurfaceUtilities.GetPatch(drawingBase, justOuter ? new List<Curve>() { curves.Last() } : curves);
        }

        public static Mesh CreatePatch(Mesh support, List<Point3d> controlPoints, double offset)
        {
            var curve = CurveUtilities.BuildCurve(controlPoints, 1, true);
            var pulledCurve = curve.PullToMesh(support, 0.1);
            var tube = CreateCurveTube(pulledCurve, offset);
            return CreatePatch(tube, support, true);
        }

        public static List<Curve> CreateSmoothingCurves(Mesh tube, Mesh drawingBase)
        {
            var mesh = tube;

            if (tube.DisjointMeshCount > 0)
            {
                var disjoints = tube.SplitDisjointPieces();
                mesh = MeshUtilities.AppendMeshes(MeshUtilities.FilterSmallMeshesByAreaMass(disjoints.ToList(), 2));
            }

            var intersectionCurves = MeshIntersectionCurve.IntersectionCurve(mesh, drawingBase);
            var outerCurves = intersectionCurves.OrderBy(x => x.GetLength()).ToList();

            var smoothCurves = new List<Curve>();
            outerCurves.ForEach(x =>
            {
                var smoothenCurve = SmoothCurve(x);
                smoothCurves.Add(smoothenCurve);
            });

            //Remove noise curves
            var finalCurve = new List<Curve>();
            smoothCurves.ForEach(x =>
            {
                if (x.IsClosed && x.IsValid)
                {
                    finalCurve.Add(x);
                }
            });

            return finalCurve;
        }

        public static Mesh CreatePatchWithSmoothing(Mesh tube, Mesh drawingBase)
        {
            var smoothenCurves = CreateSmoothingCurves(tube, drawingBase);

            return SurfaceUtilities.GetPatch(drawingBase, smoothenCurves);
        }

        private static Curve SmoothCurve(Curve outerCurve)
        {
            var polyline = ((PolylineCurve)outerCurve).ToPolyline();
            var tolerance = 0.5;
            polyline.DeleteShortSegments(tolerance);
            var polylineCurve = CurveUtilities.BuildCurve(polyline.ToList(), 3, true);

            var smoothenCurve = polylineCurve.DuplicateCurve();
            smoothenCurve = smoothenCurve.Smooth(0.7, true, true, true, true, SmoothingCoordinateSystem.World);

            return smoothenCurve;
        }

        public static Mesh CreateCurveTube(Curve curve, double radius)
        {
            Mesh tube;
            if (!TubeFromPolyline.PerformMeshFromPolyline(curve, radius, out tube))
            {
                throw new IDSException("Create Patch: Tube Creation Failed!");
            }

            // High chance that tube mesh in Rhino 7 will have degenerated faces and cause the operation later 
            tube.Faces.CullDegenerateFaces();

            return tube;
        }

        public static Brep CreatePipeBrep(List<Point3d> curvePoints, double pipeRadius)
        {
            var pipes = new Brep();
            for (var i = 1; i < curvePoints.Count; i++)
            {
                var curve = new PolylineCurve(new Point3d[] { curvePoints[i - 1], curvePoints[i] });
                var pipe = Brep.CreatePipe(curve, pipeRadius, false, PipeCapMode.Round, false, 0.1, 0.1);
                pipes.Append(BrepUtilities.Append(pipe));
            }
            return pipes;
        }

        public static Mesh CreateSkeletonTube(Mesh lowLoDSupport, List<Curve> curves, double offset)
        {
            var tubes = new List<Mesh>();

            foreach (var curve in curves)
            {
                var pulledCurve = curve.PullToMesh(lowLoDSupport, 0.1);

                Mesh tube;
                var succesMeshFromPolyline = TubeFromPolyline.PerformMeshFromPolyline(pulledCurve, offset, out tube);
                if (!succesMeshFromPolyline)
                {
                    continue;
                }

                tubes.Add(tube);
            }

            return MeshUtilities.AppendMeshes(tubes);
        }

        public static Mesh CreateSkeleton(Mesh support, List<List<Point3d>> controlPointList, double offset)
        {
            var curves = new List<Curve>();

            foreach (var i in controlPointList)
            {
                var curve = CurveUtilities.BuildCurve(i, 1, true);
                var pulledCurve = curve.PullToMesh(support, 0.1);
                curves.Add(pulledCurve);
            }

            return CreateSkeletonSurface(support, curves, offset);
        }

        public static Mesh CreateSkeletonSurface(Mesh support, List<Curve> curves, double offset)
        {
            var allTubes = new Mesh();
            var pulledCurves = new List<Curve>();
            foreach (var curve in curves)
            {
                var pulledCurve = curve.PullToMesh(support, 2.0);
                pulledCurves.Add(pulledCurve);

                Mesh tube;
                var succesMeshFromPolyline = TubeFromPolyline.PerformMeshFromPolyline(pulledCurve, offset, out tube);
                if (!succesMeshFromPolyline)
                {
                    continue;
                }

                allTubes.Append(tube);
            }

            allTubes = AutoFix.PerformUnify(allTubes);
            var patch = CreatePatch(allTubes, support, false);

            var resPatches =  patch.SplitDisjointPieces().ToList();
            var result = new Mesh();

            foreach (var resPatch in resPatches)
            {
                foreach (var pulledCurve in pulledCurves)
                {
                    Point3d ptOnCurve, ptOnMesh;
                    ptOnCurve = pulledCurve.PointAtNormalizedLength(0.5);
                    ptOnMesh = resPatch.ClosestPoint(ptOnCurve);

                    if (!((ptOnMesh - ptOnCurve).Length < 0.1))
                    {
                        continue;
                    }
                    result.Append(resPatch);
                    break;
                }
            }

            return result;
        }

        private static List<Mesh> CreateGuideSurfaces(List<Mesh> positiveSurfaces, List<Mesh> negativeSurfaces, List<Mesh> solidSurfaces, Mesh constraintMesh, out double smoothSurfaceTime)
        {
            smoothSurfaceTime = 0.0;

            var surfaces = new List<Mesh>();

            var offsettedSurfaces = CreateOffsettedGuideSurfaces(positiveSurfaces, negativeSurfaces, solidSurfaces, constraintMesh);

            if (offsettedSurfaces.Any())
            {
                var timer = new Stopwatch();
                timer.Start();

                foreach (var offsettedMesh in offsettedSurfaces)
                {
                    var bigPatch = CreatePatchWithSmoothing(offsettedMesh, constraintMesh);
                    surfaces.Add(bigPatch);
                }

                timer.Stop();
                smoothSurfaceTime = timer.ElapsedMilliseconds * 0.001;
            }

            return surfaces;
        }

        public static List<Mesh> CreateGuideSurfaces(List<Mesh> positiveSurfaces, List<Mesh> negativeSurfaces, List<Mesh> linkSurfaces, List<Mesh> solidSurfaces, List<Mesh> osteotomies, Mesh constraintMesh, string caseName)
        {
            return CreateGuideSurfaces(positiveSurfaces, negativeSurfaces, linkSurfaces, solidSurfaces, osteotomies, constraintMesh, caseName, out var _totalTime, out var _smoothSurfaceTime);
        }

        public static List<Mesh> CreateGuideSurfaces(List<Mesh> positiveSurfaces, List<Mesh> negativeSurfaces, List<Mesh> linkSurfaces, List<Mesh> solidSurfaces, List<Mesh> osteotomies, Mesh constraintMesh, string caseName, out double totalTime, out double smoothSurfaceTime)
        {
            totalTime = 0.0;
            smoothSurfaceTime = 0.0;

            var totalTimer = new Stopwatch();
            totalTimer.Start();

            try
            {
                var surfaces = new List<Mesh>();

                if (positiveSurfaces.Any() && osteotomies.Any())
                {
                    //subtract - guide surface with osteotomy => A
                    var offsettedSurfaces = CreateOffsettedGuideSurfaces(positiveSurfaces, negativeSurfaces, solidSurfaces, constraintMesh);

                    var offsettedSurfacesOnDrawn = new List<Mesh>();
                    var drawnSurfaces = positiveSurfaces.Union(negativeSurfaces).ToList();

                    // Include solid surface that are outside of positive surfaces so that it does not filter out
                    if (solidSurfaces.Any())
                    {
                        drawnSurfaces = drawnSurfaces.Union(solidSurfaces).ToList();
                    }
                    
                    offsettedSurfaces.ForEach(x =>
                    {
                        if (drawnSurfaces.Any(y =>
                        {
                            var dist = Math.Abs(MeshUtilities.Mesh2MeshMinimumDistance(x, y, 0));
                            return dist < 0.5;
                        }))
                        {
                            offsettedSurfacesOnDrawn.Add(x);
                        }
                    });

                    offsettedSurfaces = offsettedSurfacesOnDrawn;

                    var timer = new Stopwatch();
                    timer.Start();

                    var smoothenSurfaces = new List<Mesh>();
                    foreach (var surface in offsettedSurfaces)
                    {
                        var patch = CreatePatchWithSmoothing(surface, constraintMesh);
                        smoothenSurfaces.Add(patch);
                    }

                    offsettedSurfaces = new List<Mesh>
                    {
                        CreateOffset(smoothenSurfaces, Constants.GuideCreationParameters.PositiveSurfaceOffset)
                    };

                    timer.Stop();
                    smoothSurfaceTime = timer.ElapsedMilliseconds * 0.001;

                    Mesh osteotomyMesh;
                    if (!Booleans.PerformBooleanUnion(out osteotomyMesh, osteotomies.ToArray()))
                    {
                        osteotomyMesh = MeshUtilities.AppendMeshes(osteotomies);
                    }

                    var subtractedMesh = Booleans.PerformBooleanSubtraction(offsettedSurfaces, osteotomyMesh);
                    subtractedMesh.Faces.CullDegenerateFaces();
                    subtractedMesh = AutoFix.RemoveNoiseShells(subtractedMesh);
                    subtractedMesh = AutoFix.PerformUnify(subtractedMesh);

                    // Include solid surface that are outside of positive surfaces so that it does not filter out
                    RemoveShellThatIsNotOnSurface(ref subtractedMesh, (solidSurfaces.Any()? positiveSurfaces.Union(solidSurfaces) : positiveSurfaces).ToList());

                    if (linkSurfaces.Any())
                    {
                        //intersect - guide surface and link surface => B
                        //union A + B
                        var intersectedMeshes = new List<Mesh>();
                        foreach (var linkSurface in linkSurfaces)
                        {
                            var offsettedLinkMesh = CreateOffset(new List<Mesh> { linkSurface }, Constants.GuideCreationParameters.LinkSurfaceOffset);
                            offsettedLinkMesh = Remesh.PerformRemesh(offsettedLinkMesh, 0.0, 0.2, 0.2, 0.01, 0.3, false, 3);

                            foreach (var offsettedMeshDisjoint in offsettedSurfaces)
                            {
                                var intersectedMeshDisjoint = Booleans.PerformBooleanIntersection(offsettedMeshDisjoint, offsettedLinkMesh);
                                intersectedMeshes.Add(intersectedMeshDisjoint);
                            }
                        }

                        Mesh intersectedMesh;
                        if (!Booleans.PerformBooleanUnion(out intersectedMesh, intersectedMeshes.ToArray()))
                        {
                            throw new Exception("Boolean Union between intersections of positive surfaces and link surfaces failed!");
                        }

                        intersectedMesh = AutoFix.RemoveNoiseShells(intersectedMesh);
                        intersectedMesh = AutoFix.PerformUnify(intersectedMesh);

                        Mesh unionMesh;
                        if (Booleans.PerformBooleanUnion(out unionMesh, intersectedMesh, subtractedMesh))
                        {
                            unionMesh.Faces.CullDegenerateFaces();
                            var disjoints = unionMesh.SplitDisjointPieces();
                            var filteredDisjoints = MeshUtilities.FilterSmallMeshesByAreaMass(disjoints.ToList(), 2);

                            foreach (var disjoint in filteredDisjoints)
                            {
                                var bigPatch = CreatePatch(disjoint, constraintMesh, false);
                                if (bigPatch == null)
                                {
                                    throw new Exception($"There are small surface that are not able to filter out for Guide({caseName}). Please modify the drawing either:"+
                                                        "\n\t1) By increasing the drawing so that it won't generate small surface after subtraction with osteotomy plane." +
                                                        "\n\t2) By avoiding the osteotomy plane so that it won't result in generate small surface after subtraction with osteotomy plane." +
                                                        $"\nAnalysis info:\n\tTotal surface area = {MeshUtilities.ComputeTotalSurfaceArea(disjoint):F}mm^2\n\tVolume = {disjoint.Volume():F}mm^3");
                                }
                                surfaces.Add(bigPatch);
                            }
                        }
                    }
                    else
                    {
                        var disjoints = subtractedMesh.SplitDisjointPieces();
                        var filteredDisjoints = MeshUtilities.FilterSmallMeshesByAreaMass(disjoints.ToList(), 2);

                        foreach (var disjoint in filteredDisjoints)
                        {
                            var bigPatch = CreatePatch(disjoint, constraintMesh, false);
                            surfaces.Add(bigPatch);
                        }
                    }

                }
                else
                {
                    surfaces = CreateGuideSurfaces(positiveSurfaces, negativeSurfaces, solidSurfaces, constraintMesh, out smoothSurfaceTime);
                }

                var filteredGuideSurfaces = MeshUtilities.FilterSmallMeshesByAreaMass(surfaces, 5);

                if (filteredGuideSurfaces.Count != surfaces.Count)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Warning, $"There are " +
                                                                   $"{surfaces.Count - filteredGuideSurfaces.Count} small guide surfaces has" +
                                                                   $" been filtered because the area is smaller than {5} mm2.");
                }

                filteredGuideSurfaces.ForEach(x => x.Faces.CullDegenerateFaces());

                totalTimer.Stop();
                totalTime = totalTimer.ElapsedMilliseconds * 0.001;

                return filteredGuideSurfaces;
            }
            catch (Exception e)
            {
                Msai.TrackException(e, "CMF");
                IDSPluginHelper.WriteLine(LogCategory.Error, e.Message);
                IDSPluginHelper.WriteLine(LogCategory.Error, $"Aborting guide surfaces creation.");

                totalTimer.Stop();
                totalTime = totalTimer.ElapsedMilliseconds * 0.001;

                return null;
            }
        }

        private static void RemoveShellThatIsNotOnSurface(ref Mesh meshToBefiltered, List<Mesh> surfaceReference)
        {
            meshToBefiltered.Faces.CullDegenerateFaces();
            var disjoints = meshToBefiltered.SplitDisjointPieces().ToList();
            var appendedPositiveSurface = MeshUtilities.AppendMeshes(surfaceReference);
            var tmpSubtractedMeshFilteredFinal = new Mesh();
            disjoints.ForEach(x =>
            {
                if (x.CollidesWith(appendedPositiveSurface, 0.1))
                {
                    tmpSubtractedMeshFilteredFinal.Append(x);
                }
            });
            meshToBefiltered.Dispose();
            meshToBefiltered = tmpSubtractedMeshFilteredFinal;
        }

        private static List<Mesh> CreateOffsettedGuideSurfaces(List<Mesh> positiveSurfaces, List<Mesh> negativeSurfaces, List<Mesh> solidSurfaces, Mesh constraintMesh)
        {
            var offsettedSurfaces = new List<Mesh>();

            if (!positiveSurfaces.Any())
            {
                return offsettedSurfaces;
            }

            if (solidSurfaces.Any())
            {
                positiveSurfaces = positiveSurfaces.Union(solidSurfaces).ToList();
            }

            var offsettedMesh = CreateOffset(positiveSurfaces, Constants.GuideCreationParameters.PositiveSurfaceOffset);
            var offsettedMeshDisjointed = new List<Mesh>();

            if (offsettedMesh.DisjointMeshCount > 0)
            {
                var disjoined = MeshUtilities.FilterSmallMeshesByAreaMass(offsettedMesh.SplitDisjointPieces().ToList(), 1.0);
                offsettedMeshDisjointed.AddRange(disjoined);
            }
            else
            {
                offsettedMeshDisjointed.Add(offsettedMesh);
            }

            if (!negativeSurfaces.Any())
            {
                offsettedSurfaces.AddRange(offsettedMeshDisjointed);
                return offsettedSurfaces;
            }

            //needs to be bigger so boolean subtraction does not have overlapping triangles
            var offsettedNegMesh = CreateOffset(negativeSurfaces, Constants.GuideCreationParameters.NegativeSurfaceOffset);

            offsettedMeshDisjointed.ForEach(x =>
            {
                var subtractedMesh = Booleans.PerformBooleanSubtraction(x, offsettedNegMesh);

                if (subtractedMesh.IsValid)
                {
                    offsettedSurfaces.Add(subtractedMesh);
                }
            });

            return offsettedSurfaces;
        }

        public static Mesh CreateOffset(List<Mesh> surfaces, double offset)
        {
            foreach (var surface in surfaces)
            {
                surface.Faces.CullDegenerateFaces();
            }

            var resMesh = MeshUtilities.AppendMeshes(surfaces);
            resMesh.Compact();

            var offsettedMesh = resMesh.Offset(-offset / 2).Offset(offset, true);

            var res = AutoFix.PerformUnify(offsettedMesh);
            res = AutoFix.PerformAutoFix(res, 3);
            return res;
        }
    }
}
