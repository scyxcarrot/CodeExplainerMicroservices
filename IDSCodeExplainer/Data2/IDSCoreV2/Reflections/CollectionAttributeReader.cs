using IDS.Core.V2.DataModels;
using IDS.Core.V2.TreeDb.Interface;
using System;
using System.Linq;

namespace IDS.Core.V2.Reflections
{
    public static class CollectionAttributeReader
    {
        public static bool Read(IDbCollection collection, out DbCollectionVersion version, out string name)
        {
            return Read(collection.GetType(), out version, out name);
        }

        public static bool Read(Type type, out DbCollectionVersion version, out string name)
        {
            version = null;
            name = string.Empty;

            if (!(type.GetCustomAttributes(typeof(DbCollectionVersionAttribute), false) is
                    DbCollectionVersionAttribute[] versionAttribute) ||
                !versionAttribute.Any())
            {
                return false;
            }

            if (!(type.GetCustomAttributes(typeof(DbCollectionNameAttribute), false) is
                    DbCollectionNameAttribute[] nameAttribute) ||
                !nameAttribute.Any())
            {
                return false;
            }

            version = versionAttribute.First().Version;
            name = nameAttribute.First().Name;

            return true;
        }
    }
}
