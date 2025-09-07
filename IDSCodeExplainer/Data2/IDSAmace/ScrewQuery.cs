using IDS.Amace.ImplantBuildingBlocks;
using System.Collections.Generic;
using System.Linq;

namespace IDS
{
    public class ScrewQuery
    {
        private readonly ScrewDatabaseQuery _databaseQuery;

        public ScrewQuery()
        {
            _databaseQuery = new ScrewDatabaseQuery();
        }

        public double[] GetScrewLengths(ScrewBrandType screwBrandType)
        {
            var lengths = _databaseQuery.GetAvailableScrewLengths(screwBrandType.Brand, screwBrandType.Type);
            lengths = lengths.Distinct().OrderBy(length => length);
            return lengths.ToArray();
        }

        public double GetDiameter(ScrewBrandType screwBrandType)
        {
            return screwBrandType.Diameter;
        }

        public List<ScrewBrandType> GetAvailableScrewTypes(ScrewBrandType screwBrandType)
        {
            var brand = screwBrandType.Brand;
            var screwTypes = _databaseQuery.GetAvailableScrewTypes(brand).ToList();
            var screwBrandTypes = screwTypes.Select(screwType => ScrewBrandType.Parse(brand, screwType));
            return screwBrandTypes.ToList();
        }

        public ScrewBrandType GetDefaultScrewType(string brand)
        {
            var screwType = _databaseQuery.GetDefaultScrewType(brand);
            var defaultScrewBrandType = ScrewBrandType.Parse(brand, screwType);
            return defaultScrewBrandType;
        }

    }
}
