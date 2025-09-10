using IDS.CMF.CasePreferences;
using IDS.CMF.Factory;
using IDS.CMF.Preferences;
using IDS.CMF.V2.CasePreferences;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System.Linq;

namespace IDS.CMF.Utilities
{
    public class BarrelHelper
    {
        private readonly CMFImplantDirector _director;
        private readonly GuideBarrelLevelingParams _parameters;
        private const double NaNBarrelHeight = 10.0;

        public BarrelHelper(CMFImplantDirector director)
        {
            this._director = director;
            _parameters = CMFPreferences.GetGuideBarrelLevelingParameters();
        }

        public Curve GetBarrelCenterline(CasePreferenceDataModel dataModel, Transform transformation, Mesh levelingMesh)
        {
            var pointDistance = GetBarrelLevelingPointDistance(dataModel, transformation, levelingMesh);
            return new LineCurve(pointDistance.SourcePt, pointDistance.TargetPt);
        }

        public Mesh ConvertCurveToMesh(Curve centerline)
        {
            var pipeBrep = BrepUtilities.Append(Brep.CreatePipe(centerline, 0.1, false, PipeCapMode.Flat, false, 0.1, 0.1));
            return MeshUtilities.ConvertBrepToMesh(pipeBrep, true);
        }

        public static PointUtilities.PointDistance GetBarrelLevelingPointDistance(Curve barrelRef, Point3d screwHeadPoint, Vector3d screwDirection, Mesh levelingMesh, double additionalLevelingOffset)
        {
            var barrelRefPts = PointUtilities.PointsOnCurve(barrelRef, 0.01);

            var planeOnBarrelRef = new Plane(barrelRefPts[0], barrelRefPts[1], barrelRefPts[2]);
            double lineParameter;
            var line = new Line(screwHeadPoint, -screwDirection, 100);
            var foundIntersection = Intersection.LinePlane(line, planeOnBarrelRef, out lineParameter);
            if (!foundIntersection)
            {
                return new PointUtilities.PointDistance()
                {
                    SourcePt = screwHeadPoint,
                    TargetPt = Point3d.Add(screwHeadPoint, screwDirection * NaNBarrelHeight),
                    Distance = double.NaN
                };
            }

            var centerLinePoint = line.PointAt(lineParameter);
            if (levelingMesh != null)
            {
                var ray = new Ray3d(centerLinePoint, screwDirection);
                var rayParam = Intersection.MeshRay(levelingMesh, ray) - additionalLevelingOffset;
                //Less than 0.0 could be pointing towards a hole, potentially to infinite space. Avoid that being taken into account.
                if (rayParam >= 0.0)
                {
                    var projectedPt = ray.PointAt(rayParam);
                    return new PointUtilities.PointDistance()
                    {
                        SourcePt = centerLinePoint,
                        TargetPt = projectedPt,
                        Distance = (centerLinePoint - projectedPt).Length
                    };
                }
            }
 
            return new PointUtilities.PointDistance()
            {
                SourcePt = centerLinePoint,
                TargetPt = Point3d.Add(centerLinePoint, screwDirection * NaNBarrelHeight),
                Distance = double.NaN
            };
        }

        private PointUtilities.PointDistance GetBarrelLevelingPointDistance(CasePreferenceDataModel dataModel, Transform transformation, Mesh levelingMesh)
        {
            var screwAideDictionary = dataModel.BarrelAideData.GenerateBarrelAideDictionary();

            var leveledBarrelRef = (screwAideDictionary[Constants.BarrelAide.BarrelRef] as Curve).DuplicateCurve();
            leveledBarrelRef.Transform(transformation);
            var leveledScrewHead = ScrewBrepFactory.ScrewHeadPointAtOrigin;
            leveledScrewHead.Transform(transformation);
            var leveledScrewDir = ScrewBrepFactory.ScrewAxis;
            leveledScrewDir.Transform(transformation);

            return GetBarrelLevelingPointDistance(leveledBarrelRef, leveledScrewHead, leveledScrewDir, levelingMesh, _parameters.AdditonalOffset);
        }

        public struct LevelingLimitInfo
        {
            public double AdditonalOffset { get; private set; }
            public double Default { get; private set; }

            public LevelingLimitInfo(double additonalOffset, double defaultValue)
            {
                AdditonalOffset = additonalOffset;
                Default = defaultValue;
            }
        }

        public static LevelingLimitInfo GetLevelingLimit(EScrewBrand screwBrand, string barrelType)
        {
            var param = CMFPreferences.GetGuideBarrelLevelingParameters();
            double defaultValue;

            switch (screwBrand)
            {
                case EScrewBrand.Synthes:
                    defaultValue = param.DefaultRoW;
                    break;
                case EScrewBrand.SynthesUsCanada:
                    defaultValue = param.DefaultUsCanada;
                    break;
                case EScrewBrand.MtlsStandardPlus:
                    defaultValue = param.DefaultFrance;
                    break;
                default:
                    throw new IDSException("Guide Barrel Leveling - GetLevelingLimit not available!");
            }

            var barrelTypeStr = barrelType.ToLower().Trim();
            if (param.AdditionalRanges.Any(range => range.Type.ToLower().Trim() == barrelTypeStr))
            {
                var additionalRange = param.AdditionalRanges.First(range => range.Type.ToLower().Trim() == barrelTypeStr);
                defaultValue = additionalRange.Default;
            }

            var res = new LevelingLimitInfo(param.AdditonalOffset, defaultValue);
           
            return res;
        }
    }
}