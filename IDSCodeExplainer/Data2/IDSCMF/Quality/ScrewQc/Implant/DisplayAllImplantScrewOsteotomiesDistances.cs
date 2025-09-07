using IDS.CMF.V2.ScrewQc;
using IDS.CMF.Visualization;
using IDS.Core.Utilities;
using IDS.Core.Visualization;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace IDS.CMF.ScrewQc
{
    public class DisplayAllImplantScrewOsteotomiesDistances: IDisplay
    {
        private readonly List<ImplantScrewOsteotomiesDistanceConduit> _conduits;
        private readonly CMFImplantDirector _director;
        private bool _enabled;

        public DisplayAllImplantScrewOsteotomiesDistances(CMFImplantDirector director)
        {
            _conduits = new List<ImplantScrewOsteotomiesDistanceConduit>();
            _director = director;
        }

        private void Reset()
        {
            _conduits.ForEach(c => c.Enabled = false);
            _conduits.ForEach(c => c.Dispose());
            _conduits.Clear();
        }

        private IEnumerable<LinearDimension> GenerateMeasurements(IEnumerable<IImmutableList<IScrewQcResult>> allScrewQcResults)
        {
            foreach (var screwQcResults in allScrewQcResults)
            {
                foreach (var screwQcResult in screwQcResults)
                {
                    if (!(screwQcResult is OsteotomyDistanceResult osteotomyDistanceResult) ||
                        !osteotomyDistanceResult.GetMeasurementPoint(out var pointFrom, out var pointTo))
                    {
                        continue;
                    }

                    if (pointFrom != Point3d.Unset && pointTo != Point3d.Unset &&
                        pointFrom.DistanceTo(pointTo) > 0.0001)
                    {
                        yield return RhinoObjectUtilities.CreateDimension(pointFrom, pointTo, _director.Document);
                    }
                }
            }
        }

        public void Update(IEnumerable<IImmutableList<IScrewQcResult>> allScrewQcResults)
        {
            var enabled = Enabled;
            Reset();
            var dimensions = GenerateMeasurements(allScrewQcResults);
            _conduits.AddRange(dimensions.Select(d => new ImplantScrewOsteotomiesDistanceConduit(d)));
            Enabled = enabled;
        }

        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                _conduits.ForEach(c => c.Enabled = _enabled);
            }
        }
    }
}
