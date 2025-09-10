using IDS.Core.PluginHelper;
using IDS.Glenius.Enumerators;
using Rhino;
using Rhino.Geometry;

namespace IDS.Glenius.Operations
{
    public class ScrewBrepFactory
    {
        public static readonly Vector3d ScrewAxis = -Vector3d.ZAxis;

        //Below should be in config file
        public readonly static double[] Screw3Dot5Lengths = { 16, 18, 20, 22, 24, 26, 28, 30, 32, 34, 35, 36, 38, 40, 42, 45, 48, 50, 55, 60, 65 };
        public readonly static double[] Screw4Dot0Lengths = { 20, 23, 26, 29, 32, 35, 38, 41, 44, 47, 50, 55, 60, 65 };

        private readonly Brep screwHead = null;
        private readonly ScrewType type;

        public ScrewBrepFactory(ScrewType type)
        {
            this.type = type;

            switch (type)
            {
                case ScrewType.TYPE_3Dot5_LOCKING:
                    {
                        screwHead = ScrewBrepComponentDatabase.Screw3Dot5Head;
                        break;
                    }
                case ScrewType.TYPE_4Dot0_LOCKING:
                    {
                        screwHead = ScrewBrepComponentDatabase.Screw4Dot0LockingHead;
                        break;
                    }
                case ScrewType.TYPE_4Dot0_NONLOCKING:
                    {
                        screwHead = ScrewBrepComponentDatabase.Screw4Dot0NonLockingHead;
                        break;
                    }
                default:
                    break;
            }
        }

        public double GetHeadHeight()
        {
            switch (type)
            {
                case ScrewType.TYPE_3Dot5_LOCKING:
                {
                    return 3.3;
                }
                case ScrewType.TYPE_4Dot0_LOCKING:
                case ScrewType.TYPE_4Dot0_NONLOCKING:
                {
                    return 3.6;
                }
                default:
                    break;
            }

            return -1;
        }

        public string GetScrewLockingType()
        {
            switch (type)
            {
                case ScrewType.TYPE_3Dot5_LOCKING:
                case ScrewType.TYPE_4Dot0_LOCKING:
                    {
                        return "L";
                    }
                case ScrewType.TYPE_4Dot0_NONLOCKING:
                    {
                        return "NL";
                    }
                default:
                    {
                        throw new Core.PluginHelper.IDSException("Screw type is not valid!");
                    }
            }
        }

        public double GetScrewBodyRadius()
        {
            switch (type)
            {
                case ScrewType.TYPE_3Dot5_LOCKING:
                    {
                        return 1.75;
                    }
                case ScrewType.TYPE_4Dot0_LOCKING:
                case ScrewType.TYPE_4Dot0_NONLOCKING:
                    {
                        return 2.0;
                    }
                default:
                    {
                        throw new Core.PluginHelper.IDSException("Screw type is not valid!");
                    }
            }
        }

        public double GetHeadCenterOffsetFromHeadPoint()
        {
            var headHeight = GetHeadHeight();
            var offset = headHeight / 2;
            return -offset; //-{offset}mm from head center (0,0,0)
        }

        public static double[] GetAvailableScrewLengths(ScrewType type)
        {
            switch (type)
            {
                case ScrewType.TYPE_3Dot5_LOCKING:
                    {
                        return Screw3Dot5Lengths;
                    }
                case ScrewType.TYPE_4Dot0_LOCKING:
                case ScrewType.TYPE_4Dot0_NONLOCKING:
                    {
                        return Screw4Dot0Lengths;
                    }
                default:
                    {
                        throw new IDSException("Screw type is not valid!");
                    }
            }
        }

        public Brep CreateScrewBrep(Point3d headPoint, Point3d tipPoint)
        {
            Vector3d orientation = tipPoint - headPoint;
            orientation.Unitize();

            //Screw are created in the origin of WCS, where -Z Axis is the screw direction.
            Point3d headCenter = new Point3d(0, 0, GetHeadCenterOffsetFromHeadPoint());
            double bodyRadius = GetScrewBodyRadius();
            Point3d bodyOriginOffsetted = new Point3d(0, 0, -GetHeadHeight());
            Point3d bodyStart = bodyOriginOffsetted + (Vector3d.XAxis * bodyRadius);

            //Get closest number
            double bodyLength = (headPoint - tipPoint).Length;
            bodyLength = bodyLength - GetHeadHeight(); //minus off the head height, since body length starts from Head

            // Create full contour and add head contour as first part
            PolyCurve fullContour = new PolyCurve();

            // Make part of contour representing the body
            Point3d bodyLineEnd = bodyStart + (ScrewAxis * (bodyLength - bodyRadius));
            Line bodyLine = new Line(bodyStart, bodyLineEnd);
            fullContour.Append(bodyLine);

            // Make part of contour representing the tip
            Point3d tipEnd = bodyOriginOffsetted + (ScrewAxis * bodyLength);
            Line tipLine = new Line(bodyLineEnd, tipEnd);
            fullContour.Append(tipLine);

            // Create revolution surface (closed surface = solid)
            Line revAxis = new Line(headCenter, tipEnd);
            RevSurface revSurf = RevSurface.Create(fullContour, revAxis);
            var solidBody = Brep.CreateFromRevSurface(revSurf, false, false);

            var tolerance = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            var solidHead = screwHead.DuplicateBrep();
            solidHead = solidHead.CapPlanarHoles(tolerance);
            if (solidHead.SolidOrientation == BrepSolidOrientation.Inward)
            {
                solidHead.Flip();
            }

            // Transform to align with screw
            var solidScrew = Brep.CreateBooleanUnion(new[] { solidHead, solidBody }, tolerance)[0];

            //Calibrate the placement
            solidScrew.Transform(GetAlignmentTransform(orientation, headPoint));

            return solidScrew;
        }

        /// <summary>
        /// Gets the alignment transform.
        /// </summary>
        /// <param name="orientation">The orientation.</param>
        /// <param name="headPoint">The head point.</param>
        /// <returns></returns>
        public static Transform GetAlignmentTransform(Vector3d orientation, Point3d headPoint)
        {
            Transform rotation = Transform.Rotation(-Plane.WorldXY.ZAxis, orientation, Plane.WorldXY.Origin);
            Transform translation = Transform.Translation(headPoint - Plane.WorldXY.Origin);
            Transform fullTransform = Transform.Multiply(translation, rotation);
            return fullTransform;
        }
    }
}
