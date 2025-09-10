using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using Rhino.Collections;
using System;

namespace IDS
{
    public static class ScrewBrandTypeConverter
    {
        private const string KeyBrand = "screw_brand";

        private const string KeyDiameter = "screw_diameter";

        private const string KeyLocking = "screw_locking";

        //for transition from ScrewType to ScrewBrandType
        public static ScrewBrandType ConvertFromScrewType(ScrewType screwType)
        {
            ScrewBrandType brandType;

            if (!ScrewBrandType.TryParse($"{screwType}", out brandType))
            {
                throw new Exception($"ScrewType {screwType} is not supported.");
            }

            return brandType;
        }

        //for transition from ScrewBrandType to ScrewType
        public static ScrewType ConvertToScrewType(ScrewBrandType screwBrandType)
        {
            ScrewType screwType;

            if (!Enum.TryParse($"{screwBrandType}", out screwType))
            {
                throw new Exception($"ScrewBrandType {screwBrandType} is not supported.");
            }

            return screwType;
        }

        public static ScrewBrandType ConvertFromArchivableDictionary(ArchivableDictionary dictionary)
        {
            if (!(dictionary.ContainsKey(KeyBrand) && dictionary.ContainsKey(KeyDiameter) &&
                  dictionary.ContainsKey(KeyLocking)))
            {
                throw new Exception("Error in ScrewBrandType\'s ConvertToArchivableDictionary");
            }

            var brand = dictionary.GetString(KeyBrand);
            var diameter = dictionary.GetDouble(KeyDiameter);
            var locking = dictionary.GetEnumValue<ScrewLocking>(KeyLocking);
            return new ScrewBrandType(brand, diameter, locking);
        }

        public static ArchivableDictionary ConvertToArchivableDictionary(ScrewBrandType screwBrandType)
        {
            var dictionary = new ArchivableDictionary();
            dictionary.Set(KeyBrand, screwBrandType.Brand);
            dictionary.Set(KeyDiameter, screwBrandType.Diameter);
            dictionary.SetEnumValue(KeyLocking, screwBrandType.Locking);
            return dictionary;
        }
    }
}