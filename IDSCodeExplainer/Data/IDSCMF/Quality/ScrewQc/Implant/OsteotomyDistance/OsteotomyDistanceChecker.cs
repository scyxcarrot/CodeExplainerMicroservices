using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.CMF.V2.ScrewQc;
using IDS.Core.Utilities;
using IDS.RhinoInterfaces.Converter;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
#if INTERNAL
using IDS.Core.NonProduction;
#endif

namespace IDS.CMF.ScrewQc
{
    public class OsteotomyDistanceChecker : ImplantScrewQcProxyChecker
    {
        private readonly CMFImplantDirector _director;
        private readonly ScrewAtOriginalPosOptimizer _screwAtOriginalPosOptimizer;

        public override string ScrewQcCheckTrackerName => "Osteotomy Distance Check";

        public OsteotomyDistanceChecker(CMFImplantDirector director, ScrewAtOriginalPosOptimizer screwAtOriginalPosOptimizer) : 
            base(ImplantScrewQcCheck.OsteotomyDistance)
        {
            _director = director;
            _screwAtOriginalPosOptimizer = screwAtOriginalPosOptimizer;
        }

        public override IScrewQcResult Check(Screw screw)
        {
            var objectManager = new CMFObjectManager(_director);
            var casePreference = objectManager.GetCasePreference(screw);
            var screwBrand = _director.CasePrefManager.SurgeryInformation.ScrewBrand;
            var acceptableRadiusDist = CasePreferencesHelper.GetAcceptableMinimumImplantScrewDistanceToOsteotomy(screwBrand,
                casePreference.CasePrefData.ImplantTypeValue);

            var content = PerformOsteotomyDistanceCheck(screw, acceptableRadiusDist);
            return new OsteotomyDistanceResult(ScrewQcCheckName, content);
        }

        public OsteotomyDistanceContent PerformOsteotomyDistanceCheck(Screw screw, double acceptableRadiusDist, bool isOutputIntermediates = false)
        {
            if (_screwAtOriginalPosOptimizer.NoOsteotomy)
            {
                return new OsteotomyDistanceContent();
            }

            var originalPositionedScrew =
                _screwAtOriginalPosOptimizer.GetScrewAtOriginalPosition(screw, out _, out _, out var plannedBone);

            if (originalPositionedScrew == null)
            {
                return new OsteotomyDistanceContent()
                {
                    IsFloatingScrew = true
                };
            }

            var pastille = ImplantCreationUtilities.GetDotPastille(screw);
            return IsAcceptableOsteotomyDistance(screw, plannedBone, acceptableRadiusDist, pastille,
                isOutputIntermediates);
        }

        private OsteotomyDistanceContent IsAcceptableOsteotomyDistance(Screw screwOnPlanned, Mesh plannedBone,
            double acceptableMinDistance, DotPastille pastille, bool isOutputIntermediates = false)
        {
            var pastilleLocation = RhinoPoint3dConverter.ToPoint3d(pastille.Location);
            var points = Intersection.ProjectPointsToMeshes(new List<Mesh> { plannedBone }, new List<Point3d> { pastilleLocation }, screwOnPlanned.Direction, 0.0);
            if (points == null || !points.Any())
            {
                return new OsteotomyDistanceContent();
            }

            var centerOfRotationOnPlannedBone = points.OrderBy(point => point.DistanceTo(pastilleLocation)).First();

#if INTERNAL
            if (isOutputIntermediates)
            {
                InternalUtilities.AddObject(screwOnPlanned.BrepGeometry, $"Testing::Screw-{screwOnPlanned.Index}");
                InternalUtilities.AddPoint(pastilleLocation, $"Pastille location", "Testing", Color.DeepPink);
                InternalUtilities.AddPoint(centerOfRotationOnPlannedBone, $"Screw rotation point", "Testing", Color.DeepPink);
            }
#endif

            var objectManager = new CMFObjectManager(screwOnPlanned.Director);
            var guidingOutlineBlocks = objectManager.GetAllBuildingBlocks(IBB.ImplantSupportGuidingOutline);
            var guidingOutlines = guidingOutlineBlocks.Select(o => (Curve)o.Geometry);

            if (guidingOutlines.Any())
            {
                var inter = guidingOutlines.ToList();

                var curves = new List<Curve>();
                inter.ForEach(x => curves.Add(x.ToNurbsCurve()));

                var closestPoint = CurveUtilities.GetClosestPointFromCurves(curves, centerOfRotationOnPlannedBone, out var closestCurve,
                   out _);

#if INTERNAL
                if (isOutputIntermediates)
                {
                    InternalUtilities.AddCurve(closestCurve, $"Testing::IntersectionCurve", "", Color.OrangeRed);
                    InternalUtilities.AddPoint(closestPoint, $"ClosestPointOnCurve", "Testing", Color.GreenYellow);
                    InternalUtilities.AddObject(Brep.CreateFromSphere(new Sphere(centerOfRotationOnPlannedBone, 0.5)),
                        $"Testing for Screw {screwOnPlanned.Index}-CenterOfRotationOnPlannedBone");
                    InternalUtilities.AddObject(Brep.CreateFromSphere(new Sphere(closestPoint, 0.5)),
                        $"Testing for Screw {screwOnPlanned.Index}-OsteotomyClosestPoint");
                }
#endif

                var dist = closestPoint.DistanceTo(centerOfRotationOnPlannedBone);

                if (dist < acceptableMinDistance)
                {
                    return new OsteotomyDistanceContent
                    {
                        IsOk = false,
                        Distance = dist,
                        PtFrom = centerOfRotationOnPlannedBone,
                        PtTo = closestPoint
                    };
                }

                return new OsteotomyDistanceContent
                {
                    IsOk = true,
                    Distance = dist,
                    PtFrom = centerOfRotationOnPlannedBone,
                    PtTo = closestPoint
                };
            }

            return new OsteotomyDistanceContent();
        }
    }
}
