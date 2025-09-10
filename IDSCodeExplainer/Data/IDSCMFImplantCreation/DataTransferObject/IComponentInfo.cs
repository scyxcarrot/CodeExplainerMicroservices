using IDS.Interface.Geometry;
using System;
using System.Collections.Generic;

namespace IDS.CMFImplantCreation.DTO
{
    public interface IComponentInfo
    {
        Guid Id { get; set; }

        string DisplayName { get; set; }

        IMesh ClearanceMesh { get; set; }

        List<IMesh> Subtractors { get; set; }

        List<IMesh> ComponentMeshes { get; set; }

        bool IsActual { get; set; }

        bool NeedToFinalize { get; set; }
    }
}
