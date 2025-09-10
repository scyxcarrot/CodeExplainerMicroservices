using IDS.Core.V2.TreeDb.Interface;
using System;
using System.Text.RegularExpressions;

namespace IDS.Core.V2.DataModels
{
    public class DbCollectionVersion : IVersion
    {
        public uint Major { get; }

        public uint Minor { get; }

        public uint Patch { get; }

        public DbCollectionVersion(uint major, uint minor, uint patch)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
        }

        public bool NeedBackwardCompatible(IVersion savedVersion)
        {
            // Patch version will avoid backward compatible
            return savedVersion.Major < Major||
                   savedVersion.Minor < Minor;
        }

        public static bool TryParse(string versionInString, out DbCollectionVersion result)
        {
            result = null;
            var match = Regex.Match(versionInString, @"^(\d).(\d).(\d)$");
            if (!match.Success)
            {
                return false;
            }

            var major = Convert.ToUInt32(match.Groups[1].Value);
            var minor = Convert.ToUInt32(match.Groups[2].Value); 
            var patch = Convert.ToUInt32(match.Groups[3].Value);
            result = new DbCollectionVersion(major, minor, patch);
            return true;
        }

        public override string ToString()
        {
            return $"{Major}.{Minor}.{Patch}";
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (!(obj is DbCollectionVersion))
            {
                return false;
            }

            var compareObject = (DbCollectionVersion)obj;
            return (Major == compareObject.Major)
                && (Minor == compareObject.Minor)
                && (Patch == compareObject.Patch);
        }
    }
}
