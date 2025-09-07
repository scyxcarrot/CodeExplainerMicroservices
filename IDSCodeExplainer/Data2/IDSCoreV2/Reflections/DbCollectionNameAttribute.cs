using System;

namespace IDS.Core.V2.Reflections
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class DbCollectionNameAttribute : Attribute
    {
        public string Name { get; }

        public DbCollectionNameAttribute(string name)
        {
            Name = name;
        }
    }
}
