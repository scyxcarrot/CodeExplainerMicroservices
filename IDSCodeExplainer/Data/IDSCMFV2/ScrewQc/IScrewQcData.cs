using IDS.Interface.Geometry;
using System;

namespace IDS.CMF.V2.ScrewQc
{
    public interface IScrewQcData
    {
        Guid Id { get; }

        int Index { get; }

        string ScrewType { get; }

        IPoint3D HeadPoint { get; }

        IPoint3D TipPoint { get; }

        IPoint3D BodyOrigin { get; }

        Guid CaseGuid { get; }

        string CaseName { get; }

        int NCase { get; }

        double CylinderDiameter { get; }
    }
}
