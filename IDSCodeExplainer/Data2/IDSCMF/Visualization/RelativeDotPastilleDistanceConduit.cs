using IDS.CMF.CasePreferences;
using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.Utilities;
using IDS.RhinoInterfaces.Converter;
using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Line = Rhino.Geometry.Line;

namespace IDS.CMF.Visualization
{
    public class RelativeDotPastilleDistanceConduit : DisplayConduit
    {
        private static readonly Color LineColor = Color.Red;
        private static readonly Color FontColor = Color.Gold;

        private readonly CMFImplantDirector _director;

        private readonly ImplantDataModelBase _activeCaseDataModel;

        private readonly Dictionary<Guid, double> _acceptanceMinDistanceInCases;

        private readonly Guid _activeCaseId;

        private readonly List<Tuple<Line, double>> _distanceLineInfoTuples = new List<Tuple<Line, double>>();

        public RelativeDotPastilleDistanceConduit(CMFImplantDirector director, ImplantDataModelBase activeCaseDataModel)
        {
            _director = director;
            _acceptanceMinDistanceInCases = new Dictionary<Guid, double>();

            var activeCase = _director.CasePrefManager.CasePreferences.Where(casePreference =>
            {
                var activeDots = activeCaseDataModel.DotList;
                var curDots = casePreference.ImplantDataModel.DotList;

                if (activeDots.Count != curDots.Count)
                {
                    return false;
                }

                return activeDots.Any(d1 => curDots.Any(d2 => d1.Location.EpsilonEquals(d2.Location, DistanceParameters.Epsilon2Decimal)));
            });

            _activeCaseId = activeCase?.First().CaseGuid ?? Guid.Empty;
            _activeCaseDataModel = activeCaseDataModel;
        }

        private double GetAcceptableMinDistance(CasePreferenceDataModel casePreference)
        {
            var screwBrand = _director.CasePrefManager.SurgeryInformation.ScrewBrand;
            return CasePreferencesHelper.GetAcceptableMinScrewDistance(screwBrand, casePreference.CasePrefData.ImplantTypeValue);
        }

        private void Update()
        {
            var dotPastilleInfoList = new List<KeyValuePair<DotPastille, double>>();

            _director.CasePrefManager.CasePreferences.ForEach(casePreference =>
            {
                if (!_acceptanceMinDistanceInCases.ContainsKey(casePreference.CaseGuid))
                {
                    _acceptanceMinDistanceInCases.Add(casePreference.CaseGuid, GetAcceptableMinDistance(casePreference));
                }

                var acceptableMinDistance = _acceptanceMinDistanceInCases[casePreference.CaseGuid];

                if (_activeCaseId == casePreference.CaseGuid)
                {
                    dotPastilleInfoList.AddRange(_activeCaseDataModel.DotList.Where(dot => dot is DotPastille)
                        .Select(dot => new KeyValuePair<DotPastille, double>((DotPastille)dot, acceptableMinDistance)));
                    return;
                }

                casePreference.ImplantDataModel.DotList.ForEach(dot =>
                {
                    if (dot is DotPastille pastille)
                    {
                        dotPastilleInfoList.Add(new KeyValuePair<DotPastille, double>(pastille, acceptableMinDistance));
                    }
                });
            });

            var failedRelativeDistance = CheckAllRelativeDistance(dotPastilleInfoList);
            _distanceLineInfoTuples.Clear();

            if (!failedRelativeDistance.Any())
            {
                return;
            }

            var distanceLineInfoTuples = failedRelativeDistance.Select(c =>
                new Tuple<Line, double>(new Line(c.Item1, c.Item2), c.Item3));

            _distanceLineInfoTuples.AddRange(distanceLineInfoTuples);
        }

        private List<Tuple<Point3d, Point3d, double>> CheckAllRelativeDistance(List<KeyValuePair<DotPastille, double>> dotPastilleInfoList)
        {
            var failedRelativeDistance = new List<Tuple<Point3d, Point3d, double>>();
            var count = dotPastilleInfoList.Count;

            for (var i = 0; i < count; i++)
            {
                var dotPastilleA = dotPastilleInfoList[i].Key;
                var acceptableMinDistanceA = dotPastilleInfoList[i].Value;
                var pointA = RhinoPoint3dConverter.ToPoint3d(dotPastilleA.Location);

                for (var j = 1 + i; j < count; j++)
                {
                    var dotPastilleB = dotPastilleInfoList[j].Key;
                    var acceptableMinDistanceB = dotPastilleInfoList[j].Value;
                    var pointB = RhinoPoint3dConverter.ToPoint3d(dotPastilleB.Location);
                    var relativeDistance = pointA.DistanceTo(pointB);

                    var greaterAcceptableMinDistance = acceptableMinDistanceA > acceptableMinDistanceB
                        ? acceptableMinDistanceA
                        : acceptableMinDistanceB;

                    if (relativeDistance < greaterAcceptableMinDistance)
                    {
                        failedRelativeDistance.Add(
                            new Tuple<Point3d, Point3d, double>(pointA, pointB, relativeDistance));
                    }
                }
            }

            return failedRelativeDistance;
        }

        protected override void DrawOverlay(DrawEventArgs e)
        {
            base.DrawOverlay(e);

            foreach (var distanceLineInfoTuple in _distanceLineInfoTuples)
            {
                var line = distanceLineInfoTuple.Item1;
                var distance = distanceLineInfoTuple.Item2;

                e.Display.DrawDottedLine(line, LineColor);
                var midPoint = (line.From + line.To) / 2;
                e.Display.DrawDot(midPoint, $"{distance:F3}", LineColor, FontColor);
            }
        }

        protected override void PreDrawObject(DrawObjectEventArgs e)
        {
            base.PreDrawObject(e);
            Update();
        }
    }
}
