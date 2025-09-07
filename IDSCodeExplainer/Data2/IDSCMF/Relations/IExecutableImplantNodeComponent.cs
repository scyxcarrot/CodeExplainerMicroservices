using IDS.CMF.CasePreferences;
using IDS.Core.Graph;
using System;
using System.Collections.Generic;

namespace IDS.CMF.Relations
{
    public interface IExecutableImplantNodeComponent : IExecutableNodeComponent
    {
        ICaseData CaseData { get; set; }

        List<Guid> Guids { get; set; }

        bool Execute(ICaseData data);
    }
}
