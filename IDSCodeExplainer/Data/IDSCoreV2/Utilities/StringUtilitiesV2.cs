using System;
using System.Text.RegularExpressions;

namespace IDS.Core.V2.Utilities
{
    public static class StringUtilitiesV2
    {
        private static readonly Regex caseIdRegex = new Regex(
            @"(?<ID>(?:ME|MU|OB|MC)[0-9]{2}-[A-Z]{3}-[A-Z]{3})",
            RegexOptions.Compiled);

        public static string ElapsedTimeSpanToString(TimeSpan time)
        {
            return $"{time:mm\\:ss}";
        }

        public static string MemorySizeFormat(long size)
        {
            string[] memoryUnits = { "BYTE", "KB", "MB", "GB" };

            for (var i = 0; i < memoryUnits.Length - 1; i++)
            {
                var memoryUnit = memoryUnits[i];
                if (size < 1024)
                {
                    return $"{size} ({memoryUnit})";
                }

                size /= 1024;
            }

            return $"{size} ({memoryUnits[memoryUnits.Length - 1]})";
        }

        public static string ExtractCaseId(string input)
        {
            var match = caseIdRegex.Match(input);
            return match.Success ? match.Value.Trim() : input;
        }
    }
}
