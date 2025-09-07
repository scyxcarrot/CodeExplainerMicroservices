using IDS.CMF.CasePreferences.Constants;
using IDS.CMF.Enumerators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.CasePreferences
{
    public static class CasePreferenceQuery
    {
        public static string GenerateName(int caseNumber, string implantType)
        {
            return $"Implant {caseNumber}_{implantType}";
        }
    }
}
