using System;

namespace IDS.CMF.CasePreferences
{
    public interface ICaseData
    {
        Guid CaseGuid { get; }

        int NCase { get; }

        string CaseName { get; }

        void SetCaseNumber(int number);
    }
}