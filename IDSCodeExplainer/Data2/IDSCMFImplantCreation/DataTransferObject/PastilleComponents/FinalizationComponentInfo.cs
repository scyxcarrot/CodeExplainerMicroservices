using IDS.Interface.Geometry;
using System;
using System.Collections.Generic;

namespace IDS.CMFImplantCreation.DTO
{
    public class FinalizationComponentInfo : IComponentInfo
    {
        public Guid Id { get; set; }

        public string DisplayName { get; set; }

        public bool IsActual { get; set; }

        public bool NeedToFinalize { get; set; }

        public IMesh ClearanceMesh { get; set; }

        public List<IMesh> Subtractors { get; set; }

        public List<IMesh> ComponentMeshes { get; set; }

        public FinalizationComponentInfo()
        {

        }

        public FinalizationComponentInfo(PastilleComponentInfo componentInfo)
        {
            Id = componentInfo.Id;
            DisplayName = componentInfo.DisplayName;
            ClearanceMesh = componentInfo.ClearanceMesh;
            Subtractors = componentInfo.Subtractors;
            ComponentMeshes = componentInfo.ComponentMeshes;
            IsActual = componentInfo.IsActual;
            NeedToFinalize = componentInfo.NeedToFinalize;            
        }
    }
}
