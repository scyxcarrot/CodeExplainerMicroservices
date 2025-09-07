using IDS.Core.V2.DataModels;
using System;

namespace IDS.Core.V2.Reflections
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class DbCollectionVersionAttribute: Attribute
    {
        public DbCollectionVersion Version { get; }

        public DbCollectionVersionAttribute(uint major, uint minor, uint patch)
        {
            Version = new DbCollectionVersion(major, minor, patch);
        }
    }
}
