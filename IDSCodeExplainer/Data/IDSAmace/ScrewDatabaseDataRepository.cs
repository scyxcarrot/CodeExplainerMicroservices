using IDS.Amace.ImplantBuildingBlocks;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace IDS
{
    public class ScrewDatabaseDataRepository
    {
        private Dictionary<string, Point3d> _headCalibrationRepo = new Dictionary<string, Point3d>();
        private Dictionary<string, Point3d> _headRepo = new Dictionary<string, Point3d>();

        private static ScrewDatabaseDataRepository _instance;

        public static ScrewDatabaseDataRepository Get()
        {
            return _instance ?? (_instance = new ScrewDatabaseDataRepository());
        }

        public static void Reset()
        {
            _instance = new ScrewDatabaseDataRepository();
        }

        public Point3d GetHeadPoint(ScrewBrandType screwbrandType)
        {
            if (_headRepo.ContainsKey(screwbrandType.ToString()))
            {
                return _headRepo[screwbrandType.ToString()];
            }

            return ProcessHeadCurveInfo(ref _headRepo, false, screwbrandType, c =>
            {
                var start2OriginDist = c.PointAtStart - Point3d.Origin;
                var end2OriginDist = c.PointAtEnd - Point3d.Origin;

                //Assuming the nearest one is where it supposed to be the starting point.
                if (end2OriginDist < start2OriginDist)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Warning, "Head Curve in Database seems to be in reversed order, Please double check your database! ");
                    c.Reverse();
                }
            });
        }

        public Point3d GetHeadCalibrationPoint(ScrewBrandType screwbrandType)
        {
            if (_headCalibrationRepo.ContainsKey(screwbrandType.ToString()))
            {
                return _headCalibrationRepo[screwbrandType.ToString()];
            }

            return ProcessHeadCurveInfo(ref _headCalibrationRepo, true, screwbrandType, c =>
            {
                if (c.PointAtStart != Point3d.Origin)
                    c.Reverse();

                if (c.PointAtStart != Point3d.Origin)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Warning, "Head Calibration Curve in Database does not start at Origin of " +
                                                             "WCS as expected! Please double check your database!");

                    var start2OriginDist = c.PointAtStart - Point3d.Origin;
                    var end2OriginDist = c.PointAtEnd - Point3d.Origin;

                    //Assuming the nearest one is where it supposed to be the starting point, to recover from false curve reversion.
                    if (end2OriginDist < start2OriginDist)
                    {
                        c.Reverse();
                    }
                }
            });
        } 

        private Point3d ProcessHeadCurveInfo(ref Dictionary<string, Point3d> repo, bool useHeadCalibration,ScrewBrandType screwbrandType, Action<Curve> handleCurveValidation)
        {
            var database = ImplantDirector.LoadScrewDatabase();

            var curve = useHeadCalibration ? 
                ScrewAideManager.GetHeadCalibrationCurve(database, screwbrandType).DuplicateCurve() :
                ScrewAideManager.GetHeadCurve(database, screwbrandType).DuplicateCurve();

            if (repo.ContainsKey(screwbrandType.ToString()))
            {
                return repo[screwbrandType.ToString()];
            }

            handleCurveValidation?.Invoke(curve);

            repo.Add(screwbrandType.ToString(), curve.PointAtStart);

            return repo[screwbrandType.ToString()];
        }
    }
}
