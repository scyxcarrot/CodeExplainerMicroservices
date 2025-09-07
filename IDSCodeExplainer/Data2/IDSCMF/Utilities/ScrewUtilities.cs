using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Query;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Interface.Implant;
using IDS.RhinoInterfaces.Converter;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IDS.CMF.Utilities
{
    public static class ScrewUtilities
    {
        public static Screw AdjustScrewLength(Screw screw, Point3d newTipPoint)
        {
            var adjustedScrew = new Screw(screw.Director,
                screw.HeadPoint,
                newTipPoint,
                screw.ScrewAideDictionary,
                screw.Index, screw.ScrewType, screw.BarrelType);
            return adjustedScrew;
        }

        public static Point3d[] GetIntersectionPoints(Point3d screwHeadPt, Point3d screwTipPt, Mesh constraintMesh)
        {
            var line = new Line(screwHeadPt, screwTipPt);
            int[] faceIds;
            return Intersection.MeshLine(constraintMesh, line, out faceIds);
        }

        public static List<double> GetAvailableScrewLengths(Screw screw, bool isGuideFixationScrew)
        {
            var objectManager = new CMFObjectManager(screw.Director);
            var screwStyle = isGuideFixationScrew ?
                objectManager.GetGuidePreference(screw).GuidePrefData.GuideScrewStyle : objectManager.GetCasePreference(screw).CasePrefData.ScrewStyle;
            return Queries.GetAvailableScrewLengths(screw.ScrewType, screwStyle);
        }

        public static RhinoObject FindIntersection(IEnumerable<RhinoObject> rhinoObjects,
            List<Mesh> rhinoObjectsMeshReflection, Screw screw)
        {
            if (rhinoObjects.Count() != rhinoObjectsMeshReflection.Count)
            {
                throw new IDSException("rhinoObjects and rhinoObjectsMeshReflection is not synchronized!");
            }

            var intersectedMesh = FindIntersection(rhinoObjectsMeshReflection, screw);
            RhinoObject intersectedRhiObj = null;
            if (intersectedMesh != null)
            {
                var intersectedMeshIdx = rhinoObjectsMeshReflection.IndexOf(intersectedMesh);
                intersectedRhiObj = rhinoObjects.ElementAt(intersectedMeshIdx);
            }

            return intersectedRhiObj;
        }

        public static Mesh FindIntersection(IEnumerable<Mesh> meshes, Screw screw)
        {
            Mesh intersectedMesh = null;

            var minDistance = double.MaxValue;
            foreach (var mesh in meshes)
            {
                var intersectionPts = GetIntersectionPoints(screw.HeadPoint, screw.TipPoint, mesh);
                foreach (var interPt in intersectionPts)
                {
                    if (!(interPt.DistanceTo(screw.HeadPoint) <= minDistance))
                    {
                        continue;
                    }

                    minDistance = interPt.DistanceTo(screw.HeadPoint);
                    intersectedMesh = mesh;
                }
            }

            return intersectedMesh;
        }

        public static DotPastille FindDotTheScrewBelongsTo(Screw screw, List<IDot> dots)
        {
            foreach (var dot in dots)
            {
                if (dot is DotPastille pastille)
                {
                    if (pastille.Screw != null && pastille.Screw.Id == screw.Id)
                    {
                        return pastille;
                    }
                }
            }

            return null;
        }

        public static IEnumerable<DotPastille> FindDotsTheScrewBelongsTo(IEnumerable<Screw> screws,
            IEnumerable<IDot> dots)
        {
            var dotsPastilles = new List<DotPastille>();

            foreach (var dot in dots)
            {
                if (dot is DotPastille pastille)
                {
                    if (pastille.Screw != null)
                    {
                        foreach (var screw in screws)
                        {
                            if (pastille.Screw.Id == screw.Id)
                            {
                                dotsPastilles.Add(pastille);
                            }
                        }
                    }
                }
            }

            return dotsPastilles;
        }

        public static List<Screw> FindScrewsAroundRadius(Point3d pt, List<Screw> screws, double radius)
        {
            var res = new List<Screw>();

            screws.ForEach(x =>
            {
                var brep = ((Brep) x.Geometry).DuplicateBrep();
                var closestPt = brep.ClosestPoint(pt);

                var d = closestPt.DistanceTo(pt);
                if (d <= radius)
                {
                    res.Add(x);
                }
            });

            return res;
        }

        public static List<Screw> FindScrewsAroundRadiusReferencedToScrew(Point3d pt, List<Screw> screws, double radius,
            Screw screw)
        {
            var res = new List<Screw>();

            var refDir = screw.HeadPoint - screw.TipPoint;
            refDir.Unitize();

            screws.ForEach(x =>
            {
                var dir = x.HeadPoint - x.TipPoint;
                dir.Unitize();

                var brep = ((Brep) x.Geometry).DuplicateBrep();
                var closestPt = brep.ClosestPoint(pt);

                var d = closestPt.DistanceTo(pt);
                if (d <= radius)
                {
                    if (dir.EpsilonEquals(refDir, 0.01))
                    {
                        res.Add(x);
                    }
                }
            });

            return res;
        }

        public static Screw FindClosestScrew(Point3d pt, List<Screw> screws, double maxDist)
        {
            Screw closest = null;
            var dist = double.MaxValue;

            screws.ForEach(x =>
            {
                var brep = ((Brep) x.Geometry).DuplicateBrep();
                var closestPt = brep.ClosestPoint(pt);

                var d = closestPt.DistanceTo(pt);
                if (d <= maxDist && d < dist)
                {
                    closest = x;
                    dist = d;
                }
            });

            return closest;
        }

        public static bool IsScrewOnGraft(List<MeshProperties> plannedBones, Screw screwOnPlanned)
        {
            foreach (var plannedBone in plannedBones)
            {
                if (ProPlanImportUtilities.IsPartAsPartType(
                    ProPlanImportPartType.Graft, plannedBone.LayerPath))
                {
                    var plannedMesh = FindIntersection(new[] {plannedBone.Mesh}, screwOnPlanned);
                    if (plannedMesh != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static Color GetScrewGroupColor(int groupIndex)
        {
            var _colorQueue = new List<Color>()
                {Color.Aqua, Color.Magenta, Color.Yellow, Color.Teal, Color.Red, Color.DarkSalmon};

            if (groupIndex < 0)
            {
                return _colorQueue[0];
            }

            var selectedIndex = groupIndex % _colorQueue.Count;

            return _colorQueue[selectedIndex];

        }

        public static Mesh FindBoneToRegister(IEnumerable<Mesh> meshes, Screw screw)
        {
            Mesh boneToRegister = null;

            var screwHeadRef = screw.GetScrewHeadRef();
            var screwHeadRefPts = CurveUtilities.GetCurveControlPoints(screwHeadRef).ToList();

            foreach (var pt in screwHeadRefPts)
            {
                var minDistance = double.MaxValue;
                Mesh closestBoneToControlPoint = null;
                foreach (var mesh in meshes)
                {
                    var meshPoint = mesh.ClosestMeshPoint(pt, 30.0);
                    if (meshPoint != null)
                    {
                        if (!(meshPoint.Point.DistanceTo(pt) <= minDistance))
                        {
                            continue;
                        }

                        minDistance = meshPoint.Point.DistanceTo(pt);
                        closestBoneToControlPoint = mesh;
                    }
                }

                if (boneToRegister == null)
                {
                    boneToRegister = closestBoneToControlPoint;
                }
                else if (boneToRegister != closestBoneToControlPoint)
                {
                    return null;
                }
            }

            return boneToRegister;
        }

        public static int FindScrewGroupIndexWhereTheScrewBelongsTo(Screw screw, CMFImplantDirector director)
        {
            var screwGroups = director.ScrewGroups.Groups;
            var group = screwGroups.Find(s => s.ScrewGuids.Exists(c => c == screw.Id));

            if(group == null)
            {
                return -1;
            }

            return screwGroups.IndexOf(group);
        }

        public static Color GetScrewTypeColor(Screw screw)
        {
            return Queries.GetScrewTypeColor(screw.ScrewType);
        }

        public static Sphere CreateScrewSphere(DotPastille dot, double screwDiameter)
        {
            if (RhinoVector3dConverter.ToVector3d(dot.Direction) == Vector3d.Zero)
            {
                return new Sphere(RhinoPoint3dConverter.ToPoint3d(dot.Location), screwDiameter / 2);
            }
            var bottom = Point3d.Add(RhinoPoint3dConverter.ToPoint3d(dot.Location), Vector3d.Multiply(RhinoVector3dConverter.ToVector3d(dot.Direction), dot.Thickness / 2));
            return new Sphere(new Rhino.Geometry.Plane(bottom, RhinoVector3dConverter.ToVector3d(dot.Direction)), screwDiameter / 2);
        }

        public static string GetScrewNumberWithPhaseNumber(Screw screw, bool isGuideFixationScrew)
        {
            var screwManager = new ScrewManager(screw.Director);
            var screwNumber = isGuideFixationScrew ? screwManager.GetScrewNumberWithGuideNumber(screw) : 
                screwManager.GetScrewNumberWithImplantNumber(screw);
            return screwNumber;
        }

        public static Vector3d GetNormalMeshAtScrewPoint(Screw screw, Mesh mesh, double radius)
        {
            var centerOfRotation = PointUtilities.GetRayIntersection(mesh, screw.HeadPoint, screw.Direction);
            return VectorUtilities.FindAverageNormal(mesh, centerOfRotation, radius);
        }
    }
}