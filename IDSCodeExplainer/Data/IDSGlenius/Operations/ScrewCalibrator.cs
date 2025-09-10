using IDS.Core.Utilities;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Linq;

namespace IDS.Glenius.Operations
{
    public class ScrewCalibrator
    {
        private readonly Screw screw;
        private readonly Mesh bone;
        private readonly GleniusImplantDirector director;

        public Screw CalibratedScrew { get; private set; }

        public ScrewCalibrator(Screw screw, Mesh bone, GleniusImplantDirector director)
        {
            this.screw = screw;
            this.bone = bone;
            this.director = director;
        }

        public static double GetBoneProtrusionLength(ScrewType type)
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
                        return 2.6;
                    }
                default:
                    {
                        throw new Core.PluginHelper.IDSException("Screw type is not valid!");
                    }
            }
        }

        public static double AdjustLengthToAvailableScrewLength(ScrewType type, double length, out bool exceeded)
        {
            double[] availableLengths = ScrewBrepFactory.GetAvailableScrewLengths(type);
            //Get closest with available lengths
            //Should there be 2 lengths with same differences, take the larger length
            var group = availableLengths.GroupBy(x => Math.Abs(x - length)).OrderBy(x => x.Key).First();
            var adjustedLength = group.OrderBy(x => x).Last();

            exceeded = length > availableLengths.Max();

            return adjustedLength;
        }

        public bool DoCalibration()
        {
            var headPoint = AdjustHeadPointSoHeadCenterIsOnPlane();
            var boneProtrusionStartPoint = FindBoneProtrusionStartPoint();
            var screwType = screw.ScrewType;
            var protrusionLength = GetBoneProtrusionLength(screwType);
            var protrudedTipPoint = boneProtrusionStartPoint + (screw.Direction * protrusionLength);

            var protrudedScrewLength = (headPoint - protrudedTipPoint).Length;
            bool protrudedScrewHasLengthExceeded;
            var targetScrewLength = AdjustLengthToAvailableScrewLength(screwType, protrudedScrewLength, out protrudedScrewHasLengthExceeded);
            var lengthToAdjust = protrudedScrewLength - targetScrewLength; //If number is +ve, screw becomes shorter and vice versa 

            if (!protrudedScrewHasLengthExceeded)
            {
                var adjustedHeadPoint = headPoint + (screw.Direction * lengthToAdjust); 
                CalibratedScrew = new Screw(director, adjustedHeadPoint, protrudedTipPoint, screw.ScrewType, screw.Index);
            }
            else
            {
                var adjustedTipPoint = protrudedTipPoint - (screw.Direction * lengthToAdjust);
                CalibratedScrew = new Screw(director, headPoint, adjustedTipPoint, screw.ScrewType, screw.Index);
            }

            return true;
        }

        private Point3d FindBoneProtrusionStartPoint()
        {
            Line line = new Line(screw.HeadPoint, screw.Direction, 500);

            int[] faceIds;
            var intersectionPoints = Intersection.MeshLine(bone, line, out faceIds);

            var point = PointUtilities.FindFurthermostPointAlongVector(intersectionPoints, screw.Direction);

            if (point != Point3d.Unset)
            {
                return point;
            }
            else
            {
                double maxScrewLength = ScrewBrepFactory.GetAvailableScrewLengths(screw.ScrewType).Max();
                return screw.HeadPoint + screw.Direction * maxScrewLength; 
            }
        }

        private Point3d AdjustHeadPointSoHeadCenterIsOnPlane()
        {
            var adjustmentDistance = (screw.HeadPoint - screw.headCenter).Length;
            return screw.HeadPoint - (screw.Direction * adjustmentDistance);
        }
    }
}
