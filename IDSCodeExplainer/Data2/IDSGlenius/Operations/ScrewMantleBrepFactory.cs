using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Glenius.Enumerators;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Glenius.Operations
{
    public class ScrewMantleBrepFactory
    {
        public static readonly double ScrewMantleHeightWithoutExtension = 22.1;

        private readonly Brep screwMantle = null;
        private readonly ScrewType type;

        public ScrewMantleBrepFactory(ScrewType type)
        {
            this.type = type;

            switch (type)
            {
                case ScrewType.TYPE_3Dot5_LOCKING:
                    {
                        screwMantle = ScrewBrepComponentDatabase.Screw3Dot5Mantle;
                        break;
                    }
                case ScrewType.TYPE_4Dot0_LOCKING:
                    {
                        screwMantle = ScrewBrepComponentDatabase.Screw4Dot0LockingMantle;
                        break;
                    }
                case ScrewType.TYPE_4Dot0_NONLOCKING:
                    {
                        screwMantle = ScrewBrepComponentDatabase.Screw4Dot0NonLockingMantle;
                        break;
                    }
                default:
                    break;
            }
        }

        public Brep CreateScrewMantleBrep(double extensionLength)
        {
            return CreateScrewMantleBrepAtOrigin(extensionLength);
        }

        public Brep CreateScrewMantleBrep(Point3d extensionPoint, Vector3d orientation, double extensionLength)
        {
            var screwMantleBrep = CreateScrewMantleBrepAtOrigin(extensionLength);
            var axis = new Vector3d(orientation);
            if (!axis.IsUnitVector)
            {
                axis.Unitize();
            }

            var screwFactory = new ScrewBrepFactory(type);
            var headHeight = screwFactory.GetHeadHeight();

            var screwHeadPoint = Point3d.Subtract(extensionPoint, axis * headHeight);
            screwMantleBrep.Transform(ScrewBrepFactory.GetAlignmentTransform(orientation, screwHeadPoint));
            return screwMantleBrep;
        }

        private double GetScrewMantleTopBodyRadius()
        {
            switch (type)
            {
                case ScrewType.TYPE_3Dot5_LOCKING:
                {
                    return 4.8;
                }
                case ScrewType.TYPE_4Dot0_LOCKING:
                case ScrewType.TYPE_4Dot0_NONLOCKING:
                {
                    return 5.10; //10.20/2
                    }
                default:
                {
                    throw new IDSException("Screw type is not valid!");
                }
            }
        }

        private double GetScrewMantleBottomBodyRadius()
        {
            switch (type)
            {
                case ScrewType.TYPE_3Dot5_LOCKING:
                {
                    return 3.95; //7.9/2
                }
                case ScrewType.TYPE_4Dot0_LOCKING:
                case ScrewType.TYPE_4Dot0_NONLOCKING:
                {
                    return 4.435; //8.87/2
                }
                default:
                {
                    throw new IDSException("Screw type is not valid!");
                }
            }
        }

        private Brep CreateScrewMantleBrepAtOrigin(double extensionLength)
        {
            var screwMantleBrep = screwMantle.DuplicateBrep();

            if (extensionLength > 0)
            {
                var screwFactory = new ScrewBrepFactory(type);
                var headHeight = screwFactory.GetHeadHeight();

                var startExtension = new Point3d(0, 0, 0);
                var endExtension = Point3d.Add(startExtension, (ScrewBrepFactory.ScrewAxis * (extensionLength + headHeight)));

                var topBodyRadius = GetScrewMantleTopBodyRadius();
                var bottomBodyRadius = GetScrewMantleBottomBodyRadius();
                var topCircle = new Circle(startExtension, topBodyRadius);
                var bottomCircle = new Circle(endExtension, bottomBodyRadius);

                var roundingRadius = extensionLength >= 1 ? 0.5 : 0.0;

                var shapeCurve = CreateMantleExtensionCurve(topCircle, bottomCircle, roundingRadius, screwMantleBrep);

                var axis = new Line(topCircle.Center, bottomCircle.Center);
                var revsrf = RevSurface.Create(shapeCurve, axis);
                var screwMantleExtension = Brep.CreateFromRevSurface(revsrf, false, true);

                //union to remove internal cap of screwMantleHead
                var breps = Brep.CreateBooleanUnion(new List<Brep> { screwMantle, screwMantleExtension }, 0.1);
                screwMantleBrep = breps[0];
            }

            return screwMantleBrep;
        }

        private Curve CreateMantleExtensionCurve(Circle topCircle, Circle bottomCircle, double roundingRadius, Brep screwMantleBrepBase)
        {
            var originTopPoint = topCircle.PointAt(0);
            var originBottomPoint = bottomCircle.PointAt(0);

            var sideCurve = screwMantleBrepBase.Curves3D.OrderBy(curve => curve.GetLength()).Last();
            var sideLine = new Line(sideCurve.PointAtStart, sideCurve.PointAtEnd);

            double t1, t2;
            Point3d p1, p2;
            if (Intersection.LineCircle(sideLine, topCircle, out t1, out p1, out t2, out p2) == LineCircleIntersection.Single)
            {
                originTopPoint = p1;
                originBottomPoint = bottomCircle.ClosestPoint(originTopPoint);
            }

            if (roundingRadius <= 0)
            {
                return CurveUtilities.CreateLinearCurve(originTopPoint, originBottomPoint);
            }

            var fullTopBottomCurve = CurveUtilities.CreateLinearCurve(originTopPoint, originBottomPoint);
            var fullBottomCenterCurve = CurveUtilities.CreateLinearCurve(originBottomPoint, bottomCircle.Center);
            var bottomRoundedCurve = Curve.CreateFillet(fullTopBottomCurve, fullBottomCenterCurve, roundingRadius, 0.0, 0.0).ToNurbsCurve();

            var bottomPointBeforeRounding = CurveUtilities.GetNearestEndPointToPoint(bottomRoundedCurve, originTopPoint);
            var bottomPointAfterRounding = CurveUtilities.GetNearestEndPointToPoint(bottomRoundedCurve, bottomCircle.Center);

            var trimmedTopBottomCurve = CurveUtilities.CreateLinearCurve(originTopPoint, bottomPointBeforeRounding);
            var trimmedBottomCenterCurve = CurveUtilities.CreateLinearCurve(bottomPointAfterRounding, bottomCircle.Center);

            return Curve.JoinCurves(new[] { trimmedTopBottomCurve, bottomRoundedCurve, trimmedBottomCenterCurve }).FirstOrDefault();
        }
    }
}
