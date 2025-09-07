using IDS.CMF.CasePreferences;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Preferences;
using IDS.CMF.Utilities;
using IDS.CMF.V2.Tracking;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Core.V2.ExternalTools;
using IDS.Interface.Implant;
using Rhino;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
#if (INTERNAL)
using IDS.Core.NonProduction;
#endif

namespace IDS.CMF.Operations
{
    public class InternalConnectionCreatorV1
    {
        public MsaiTrackingInfo TrackingInfo { get; set; }

        //Old operations
        /// <summary>
        /// Steps to generate implant connection tube
        /// 1. Create initial tube to get intersection curves between tube and support mesh. 
        /// If connection width are half smaller than connection thickness, wrap ratio value to based on connection thickness.
        /// Calculation for this tube radius = radius - wrap ratio*connection(to compensate wrap later).
        /// tube radius = [connection width/2] - [wrap ratio*width or wrap ratio*thickness]
        /// 2. Get intersection curves between create tubes and support mesh.
        /// 3. Call "GenerateImplantComponent", refer comment to add that function.
        /// </summary>
        /// <param name="connectionDataModels"></param>
        /// <param name="casePreferencesData"></param>
        /// <param name="individualImplantParams"></param>
        /// <param name="supportMesh"></param>
        /// <param name="supportMeshFull"></param>
        /// <param name="screws"></param>
        /// <param name="isCreateActualConnection"></param>
        /// <param name="implantSurfaces"></param>
        /// <returns></returns>
        public bool GenerateImplantTubes(List<DotCurveDataModel> connectionDataModels, CasePreferenceDataModel casePreferencesData,
            IndividualImplantParams individualImplantParams, Mesh supportMesh, Mesh supportMeshFull, IEnumerable<Screw> screws, bool isCreateActualConnection, out Dictionary<Mesh, List<IDot>> implantSurfaces)
        {
            implantSurfaces = new Dictionary<Mesh, List<IDot>>();
            var numConnection = 0;
            var implantNum = casePreferencesData.NCase;

            foreach (var dataModel in connectionDataModels)
            {
                using (TimeTracking.NewInstance(
                           $"{TrackingConstants.DevMetrics}_V1GenerateConnection-Implant {casePreferencesData.CaseName} (numConnection: {numConnection}) ({dataModel.Curve.GetLength()}mm)",
                           TrackingInfo.AddTrackingParameterSafely))
                {
                    var curve = dataModel.Curve;
                    var connectionWidth = dataModel.ConnectionWidth;
                    var connectionThickness = dataModel.ConnectionThickness;
                    var averageVector = dataModel.AverageVector;

                    // start here for ConnectionCreator
                    var wrapBasis = ImplantWrapAndOffsetPredictor.GetBasis(connectionThickness, connectionWidth);
                    var tubeRadius =
                        ImplantWrapAndOffsetPredictor.GetTubeRadius(connectionThickness, connectionWidth);

                    try
                    {
                        var pulledCurve = curve.PullToMesh(supportMeshFull, 0.01);

                        Mesh tube;
                        bool succesMeshFromPolyline =
                            TubeFromPolyline.PerformMeshFromPolyline(
                                pulledCurve, tubeRadius, out tube);

                        if (!succesMeshFromPolyline)
                        {
                            DotSearchResult dotA, dotB;
                            ImplantCreationUtilities.GetDotInformation(
                                curve, dataModel.Dots, screws, out dotA, out dotB);

                            var dotStringA =
                                ImplantCreationUtilities.FormatDotDisplayString(
                                    dotA, implantNum);
                            var dotStringB =
                                ImplantCreationUtilities.FormatDotDisplayString(
                                    dotB, implantNum);

                            IDSPluginHelper.WriteLine(LogCategory.Error,
                                $"Tube creation for plate/link between" +
                                $" {dotStringA} and {dotStringB} failed.");
                            return false;
                        }

                        var intersectionPolyline =
                            ImplantCreationUtilities.GetIntersectionCurve(
                                tube, 
                                supportMesh, 
                                averageVector, 
                                implantNum, 
                                numConnection);
                        tube.Dispose();

#if (INTERNAL)
                        InternalUtilities.AddCurve(intersectionPolyline, $"Curve Tube of {numConnection}",
                            $"Test Implant::Implant {implantNum}", Color.Magenta);
#endif
                        List<Curve> sharpCurves;
                        var implantTube =
                            GenerateImplantComponent(intersectionPolyline, supportMesh, connectionThickness,
                                connectionWidth, wrapBasis, individualImplantParams, pulledCurve, out sharpCurves, isCreateActualConnection);

                        foreach (var sharpCurve in sharpCurves)
                        {
                            pulledCurve = sharpCurve.PullToMesh(supportMeshFull, 0.01);

                            if (pulledCurve.GetLength() < tubeRadius)
                            {
                                continue;
                            }

                            succesMeshFromPolyline =
                                TubeFromPolyline.PerformMeshFromPolyline(pulledCurve, tubeRadius, out tube);
                            if (!succesMeshFromPolyline)
                            {
                                DotSearchResult dotA, dotB;
                                ImplantCreationUtilities.GetDotInformation(
                                    curve, dataModel.Dots, screws, out dotA, out dotB);

                                var dotStringA =
                                    ImplantCreationUtilities.FormatDotDisplayString(
                                        dotA, implantNum);
                                var dotStringB =
                                    ImplantCreationUtilities.FormatDotDisplayString(
                                        dotB, implantNum);

                                IDSPluginHelper.WriteLine(LogCategory.Error,
                                    $"Tube creation for plate/link between" +
                                    $" {dotStringA} and {dotStringB} failed.");
                                return false;
                            }

                            intersectionPolyline = ImplantCreationUtilities.GetIntersectionCurve(tube, supportMesh, averageVector, implantNum, numConnection);
                            tube.Dispose();

                            var dupimplantTube =
                                GenerateSharpImplantComponent(intersectionPolyline, sharpCurve, supportMesh, connectionThickness, connectionWidth, wrapBasis,
                                    individualImplantParams, isCreateActualConnection);

#if (INTERNAL)
                            InternalUtilities.AddObject(dupimplantTube, $"DupImplantTube of {numConnection}", $"Test Implant::Implant {implantNum}");
#endif

                            implantTube.Append(dupimplantTube);
                        }

                        intersectionPolyline.Dispose();
                        pulledCurve.Dispose();
#if (INTERNAL)
                        InternalUtilities.AddObject(implantTube, $"ImplantTube of {numConnection}", $"Test Implant::Implant {implantNum}");
#endif
                        implantSurfaces.Add(implantTube, dataModel.Dots);
                    }
                    catch (Exception e)
                    {
                        Msai.TrackException(e, "CMF");

                        DotSearchResult dotA, dotB;
                        ImplantCreationUtilities.GetDotInformation(
                            curve, dataModel.Dots, screws, out dotA, out dotB);

                        var dotStringA =
                            ImplantCreationUtilities.FormatDotDisplayString(
                                dotA, implantNum);
                        var dotStringB =
                            ImplantCreationUtilities.FormatDotDisplayString(
                                dotB, implantNum);
                        IDSPluginHelper.WriteLine(LogCategory.Error,
                            $"Plate/Link between {dotStringA} and " +
                            $"{dotStringB} could not be created." +
                            $"\nThe following unknown exception was thrown. " +
                            $"Please report this to the development team." +
                            $"\n{e}");

                        return false;
                    }

                    numConnection++;
                }
            }
            return true;
        }

        /// <summary>
        /// 1. Calculate wrap offset value which slightly larger than create tube wrap offset to make sure there intersection afterwards with support mesh.
        /// 2. If width smaller than thickness, offset distance of upper and lower surface will be different. or else it will have same offset distance.
        /// 3. Offset been done 2 time in order to create "radial" effect which bottom implant facing towards inside.
        /// 4. Offset direction will based on average direction of closest vertex.
        /// 5. Wrap final connection with wrap value that been compensate at beginning(wrap ratio*connection width or wrap ratio*connection thickness).
        /// </summary>
        /// <param name="interCurve"></param>
        /// <param name="supportMesh"></param>
        /// <param name="thickness"></param>
        /// <param name="wrapBasis"></param>
        /// <param name="individualImplantParams"></param>
        /// <param name="connectionCurve"></param>
        /// <param name="isCreateActualConnection"></param>
        /// <returns></returns>
        private Mesh GenerateImplantComponent(Curve interCurve, Mesh supportMesh, double connectionThickness, double connectionWidth, double wrapBasis,
            IndividualImplantParams individualImplantParams, Curve connectionCurve, out List<Curve> sharpCurves, bool isCreateActualConnection)
        {
            var lowerOffsetCompensation =
                ImplantWrapAndOffsetPredictor.CalculateLowerOffsetCompensation(connectionThickness, connectionWidth, wrapBasis);
            var finalWrapOffset = wrapBasis * individualImplantParams.WrapOperationOffsetInDistanceRatio;
            var offsetDistanceLower = ((connectionThickness - finalWrapOffset) / 2) - lowerOffsetCompensation;
            var offsetDistanceUpper = (connectionThickness - finalWrapOffset);

            if (offsetDistanceUpper < 0.00)
            {
                throw new IDSException("Implant pastille thickness and width ratio invalid.");
            }

            var connectionSurface = SurfaceUtilities.GetPatch(supportMesh, interCurve);

            if (isCreateActualConnection)
            {
                connectionSurface = Remesh.PerformRemesh(connectionSurface, 0.0, 0.2, 0.2, 0.01, 0.3, false, 3);
            }

            var connectionOnMesh = connectionCurve.DuplicateCurve();
            var connectionOnMeshPts = PointUtilities.PointsOnCurve(connectionOnMesh, 0.05);

            connectionOnMesh.Dispose();

            var connectionOnMeshDatas = new List<VertexAndNormal>();
            connectionOnMeshPts.ForEach(x =>
            {
                var norm = VectorUtilities.FindNormalAtPoint(x, supportMesh, 2.0);
                connectionOnMeshDatas.Add(new VertexAndNormal
                {
                    Normal = norm,
                    Point = x
                });
            });
            connectionOnMeshPts.Clear();

            int startDiviate;
            int endDiviate;
            var interpolatedConnectionOnMeshDatas = InterpolateNormal(connectionOnMeshDatas, 20, out startDiviate, out endDiviate);

            var datas = new List<VertexAndNormal>();
            foreach (var connectionSurfaceVertex in connectionSurface.Vertices)
            {
                var curveData = FindClosest(connectionSurfaceVertex, interpolatedConnectionOnMeshDatas);

                var data = new VertexAndNormal
                {
                    Normal = curveData.Normal,
                    Point = connectionSurfaceVertex,
                };
                datas.Add(data);
            }

            var toReplace = FixAbnormalsNormals(datas, connectionSurface, connectionThickness + 0.1, 70);
            toReplace.ForEach(x =>
            {
                datas[x.Key] = x.Value;
            });
            for (var i = 0; i < 300; i++)
            {
                toReplace.ForEach(u =>
                {
                    var corrected = u.Value;
                    var closestAround = FindClosestAround(corrected, datas, 1 - (i / 300));

                    var sumNormal = corrected.Normal;
                    closestAround.ForEach(y =>
                    {
                        sumNormal = Vector3d.Add(sumNormal, y.Normal);
                    });

                    corrected.Normal = Vector3d.Divide(sumNormal, closestAround.Count + 1);
                    datas[u.Key] = corrected;
                });
            }

            for (var i = 0; i < 100; i++)
            {
                toReplace.ForEach(u =>
                {
                    var corrected = u.Value;
                    var closestAround = FindClosestAroundAndClosestWithTheNormal(corrected, connectionOnMeshDatas, 0.5 - (i / 300));

                    var sumNormal = Vector3d.Add(corrected.Normal, closestAround.Normal);

                    corrected.Normal = Vector3d.Divide(sumNormal, 2);
                    datas[u.Key] = corrected;
                });
            }

            sharpCurves = GetSharpAngleCurves(interpolatedConnectionOnMeshDatas.Select(i => i.Point).ToList(), startDiviate, endDiviate);

            var offsetVerticesLower = new List<Point3d>();
            var offsetVerticesUpper = new List<Point3d>();
            foreach (var vertexData in datas)
            {
                var ptLower = vertexData.Point + offsetDistanceLower * vertexData.Normal;
                offsetVerticesLower.Add(ptLower);
                var ptUpper = vertexData.Point + offsetDistanceUpper * vertexData.Normal;
                ptUpper = ImplantCreationUtilities.EnsureVertexIsOnSameLevelAsThickness(connectionSurface, ptUpper, offsetDistanceUpper);
                offsetVerticesUpper.Add(ptUpper);
            }

            datas.Clear();

            var smallestDetail = individualImplantParams.WrapOperationSmallestDetails;
            var gapClosingDistance = individualImplantParams.WrapOperationGapClosingDistance;

            var offsettedVertices = new List<List<Point3d>>()
            {
                offsetVerticesLower, 
                offsetVerticesUpper
            };
            var wrappedMesh = ImplantCreationUtilities
                .OptimizeOffsetandWrap(
                    offsettedVertices, 
                    connectionSurface, 
                    smallestDetail, 
                    gapClosingDistance, 
                    finalWrapOffset);
            connectionSurface.Dispose();
            offsetVerticesLower.Clear();
            offsetVerticesUpper.Clear();
            return wrappedMesh;
        }

        private Mesh GenerateSharpImplantComponent(Curve interCurve, Curve connectionCurve, Mesh supportMesh,
            double connectionThickness, double connectionWidth, double wrapBasis,
            IndividualImplantParams individualImplantParams, bool isCreateActualConnection)
        {
            var lowerOffsetCompensation =
                ImplantWrapAndOffsetPredictor.CalculateLowerOffsetCompensation(connectionThickness, connectionWidth, wrapBasis);

            var finalWrapOffset = wrapBasis * individualImplantParams.WrapOperationOffsetInDistanceRatio;
            var offsetDistanceLower = ((connectionThickness - finalWrapOffset) / 2) - lowerOffsetCompensation;
            var offsetDistanceUpper = (connectionThickness - finalWrapOffset);
            if (offsetDistanceUpper < 0.00)
            {
                throw new IDSException("Implant pastille thickness and width ratio invalid.");
            }

            var tmpConnectionSurface = SurfaceUtilities.GetPatch(supportMesh, interCurve);

#if (INTERNAL)
            InternalUtilities.AddObject(tmpConnectionSurface, $"ImplantTubeGenerateImplantComponentPatch", $"Test Implant::Implant");
#endif
            var connectionSurface = tmpConnectionSurface.DuplicateMesh();
            if (isCreateActualConnection)
            {

                connectionSurface = Remesh.PerformRemesh(connectionSurface, 0.0, 0.2, 0.2, 0.01, 0.3, false, 3);
            }

            var connectionOnMesh = connectionCurve.DuplicateCurve();
            var connectionOnMeshPts = PointUtilities.PointsOnCurve(connectionOnMesh, 0.05);
            var connectionOnMeshDatas = new List<VertexAndNormal>();
            connectionOnMeshPts.ForEach(x =>
            {
                var norm = VectorUtilities.FindNormalAtPoint(x, supportMesh, 2.0);
                connectionOnMeshDatas.Add(new VertexAndNormal
                {
                    Normal = norm,
                    Point = x
                });
            });

            connectionOnMeshPts.Clear();

            int startDiviate;
            int endDiviate;
            var interpolatedConnectionOnMeshDatas =
                InterpolateNormal(connectionOnMeshDatas, 1, out startDiviate, out endDiviate);
            connectionOnMeshDatas.Clear();

            var datas = new List<VertexAndNormal>();
            foreach (var connectionSurfaceVertex in connectionSurface.Vertices)
            {
                var curveData = FindClosest(connectionSurfaceVertex, interpolatedConnectionOnMeshDatas);

                var data = new VertexAndNormal
                {
                    Normal = curveData.Normal,
                    Point = connectionSurfaceVertex,
                };
                datas.Add(data);
            }
            datas = Uniformize(datas);
            var offsetVerticesLower = new List<Point3d>();
            var offsetVerticesUpper = new List<Point3d>();
            foreach (var vertexData in datas)
            {
                var ptLower = vertexData.Point + offsetDistanceLower * vertexData.Normal;
                ptLower = ImplantCreationUtilities.EnsureVertexIsOnSameLevelAsThickness(connectionSurface, ptLower, offsetDistanceLower);
                offsetVerticesLower.Add(ptLower);
                var ptUpper = vertexData.Point + offsetDistanceUpper * vertexData.Normal;
                ptUpper = ImplantCreationUtilities.EnsureVertexIsOnSameLevelAsThickness(connectionSurface, ptUpper, offsetDistanceUpper);
                offsetVerticesUpper.Add(ptUpper);
            }

            datas.Clear();

            var smallestDetail = individualImplantParams.WrapOperationSmallestDetails;
            var gapClosingDistance = individualImplantParams.WrapOperationGapClosingDistance;

            var wrappedMesh = ImplantCreationUtilities.OptimizeOffsetandWrap(new List<List<Point3d>>() { offsetVerticesLower, offsetVerticesUpper },
                connectionSurface, smallestDetail, gapClosingDistance, finalWrapOffset);
            connectionSurface.Dispose();
            offsetVerticesLower.Clear();
            offsetVerticesUpper.Clear();

            return wrappedMesh;
        }


        private List<VertexAndNormal> InterpolateNormal(List<VertexAndNormal> datas, int sizeOnBothEnds, out int startDiviate, out int endDiviate)
        {
            var res = new List<VertexAndNormal>();

            startDiviate = -1;
            endDiviate = -1;
            for (var i = 0; i < datas.Count; i++)
            {
                var backEnd = 0;
                var frontEnd = 0;

                if (i == 0)
                {
                    backEnd = 0;
                }
                else if (i < sizeOnBothEnds)
                {
                    backEnd = i;
                }
                else
                {
                    backEnd = sizeOnBothEnds;
                }

                if (i == datas.Count - 1)
                {
                    frontEnd = 0;
                }
                else if (i < datas.Count - sizeOnBothEnds)
                {
                    frontEnd = sizeOnBothEnds;
                }
                else
                {
                    frontEnd = datas.Count - 1 - i;
                }

                var normals = new List<Vector3d>();
                for (var j = i; j > i - backEnd; j--)
                {
                    normals.Add(datas[j].Normal);
                }

                for (var j = i; j < i + frontEnd; j++)
                {
                    normals.Add(datas[j].Normal);
                }

                var avgNormal = datas[i].Normal;
                normals.ForEach(x => { avgNormal += x; });
                avgNormal = avgNormal / (normals.Count + 1);
                avgNormal.Unitize();
                normals.Clear();

                var vn = new VertexAndNormal
                {
                    Normal = avgNormal,
                    Point = datas[i].Point
                };

                //check the diff
                var diff = RhinoMath.ToDegrees(Vector3d.VectorAngle(avgNormal, datas[i].Normal));
                if (diff > 20.0)
                {
                    int sizeOnSharpEnd = 0;
                    if (startDiviate == -1)
                    {
                        if (i > sizeOnSharpEnd)
                        {
                            startDiviate = i - sizeOnSharpEnd;
                        }
                        else
                        {
                            startDiviate = i;
                        }
                    }
                    else
                    {
                        if (i + sizeOnSharpEnd < datas.Count)
                        {
                            endDiviate = i + sizeOnSharpEnd;
                        }
                        else
                        {
                            endDiviate = i + datas.Count - 1;
                        }

                    }
                }

                res.Add(vn);
            }

            return res;
        }

        private List<KeyValuePair<int, VertexAndNormal>> FixAbnormalsNormals(List<VertexAndNormal> baseItem, Mesh connectionSurface, double radius, double tolerance)
        {
            var res = new List<KeyValuePair<int, VertexAndNormal>>();
            if (!baseItem.Any())
            {
                return res;
            }

            if (connectionSurface.FaceNormals == null || !connectionSurface.FaceNormals.Any())
            {
                connectionSurface.FaceNormals.ComputeFaceNormals();
            }

            for (var i = 0; i < baseItem.Count; i++)
            {
                var x = baseItem[i];
                var closest = connectionSurface.ClosestMeshPoint(x.Point, radius);
                var closestNormal = connectionSurface.FaceNormals[closest.FaceIndex];

                var angle = RhinoMath.ToDegrees(Vector3d.VectorAngle(closestNormal, x.Normal));
                if (angle <= tolerance)
                {
                    continue;
                }

                var corrected = new VertexAndNormal() { Point = x.Point, Normal = closestNormal };
                var closestIdentical = FindClosestAroundAndClosestWithTheNormal(corrected, baseItem, 5);
                var finalCorrected = new VertexAndNormal() { Point = x.Point, Normal = closestIdentical.Normal };

                res.Add(new KeyValuePair<int, VertexAndNormal>(i, finalCorrected));
            }

            return res;
        }

        private VertexAndNormal FindClosestAroundAndClosestWithTheNormal(VertexAndNormal pt, List<VertexAndNormal> baseItem,
            double radius)
        {
            var res = baseItem[0];

            bool foundAny = false;

            baseItem.ForEach(x =>
            {
                if (!(pt.Point.DistanceTo(x.Point) <= radius))
                {
                    return;
                }

                var angle = RhinoMath.ToDegrees(Vector3d.VectorAngle(x.Normal, pt.Normal));
                if (angle < RhinoMath.ToDegrees(Vector3d.VectorAngle(res.Normal, pt.Normal)))
                {
                    res = x;
                    foundAny = true;
                }
            });

            if (!foundAny)
            {
                return pt;
            }

            return res;
        }

        private List<VertexAndNormal> FindClosestAround(VertexAndNormal pt, List<VertexAndNormal> baseItem, double radius)
        {
            var res = new List<VertexAndNormal>();
            if (!baseItem.Any())
            {
                return res;
            }

            baseItem.ForEach(x =>
            {
                if (pt.Point.DistanceTo(x.Point) <= radius)
                {
                    res.Add(x);
                }
            });

            return res;
        }

        private VertexAndNormal FindClosest(Point3d pt, List<VertexAndNormal> baseItem)
        {
            if (!baseItem.Any())
            {
                return new VertexAndNormal { Point = Point3d.Unset, Normal = Vector3d.Unset };
            }

            var closest = baseItem[0];

            baseItem.ForEach(x =>
            {
                if (pt.DistanceTo(x.Point) < pt.DistanceTo(closest.Point))
                {
                    closest = x;
                }
            });

            return closest;
        }

        private List<VertexAndNormal> FindAround(VertexAndNormal data, List<VertexAndNormal> datas, double radius)
        {
            var res = new List<VertexAndNormal>();

            datas.ForEach(x =>
            {
                if (x.Point.DistanceTo(data.Point) <= radius)
                {
                    res.Add(x);
                }
            });

            return res;
        }

        private List<VertexAndNormal> Uniformize(List<VertexAndNormal> datas)
        {
            var res = new List<VertexAndNormal>();

            for (int i = 0; i < datas.Count; i++)
            {
                var vertexAndNormal = datas[i];
                var surrounding = FindAround(vertexAndNormal, datas, 2);
                Vector3d normal = vertexAndNormal.Normal;

                surrounding.ForEach(n => { normal += n.Normal; });
                normal = normal / (surrounding.Count - 1);
                normal.Unitize();

                var newData = new VertexAndNormal()
                {
                    Point = vertexAndNormal.Point,
                    Normal = normal
                };

                res.Add(newData);
            }

            return res;
        }

        //atm, only one sharp angle curve
        private List<Curve> GetSharpAngleCurves(List<Point3d> points, int startDiviate, int endDiviate)
        {
            var curves = new List<Curve>();
            if (startDiviate != -1 && endDiviate != -1)
            {
                var sharpCurvePoints = points.Skip(startDiviate).Take(endDiviate - startDiviate + 1);
                var sharpCurve = new Polyline(sharpCurvePoints).ToNurbsCurve();

                curves.Add(sharpCurve);

#if (INTERNAL)
                InternalUtilities.AddCurve(sharpCurve, $"sharpCurve", $"Test Implant::Implant", Color.Magenta);
#endif
            }
            return curves;
        }
    }
}
