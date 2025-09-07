using IDS.CMF.V2.DataModel;
using IDS.Interface.Geometry;
using System;
using System.Collections.Generic;

namespace IDS.CMFImplantCreation.DTO
{
    public class LandmarkComponentInfo : IComponentInfo
    {
        public Guid Id { get; set; }

        public string DisplayName { get; set; }
                
        public IMesh ClearanceMesh { get; set; }

        public List<IMesh> Subtractors { get; set; }

        public List<IMesh> ComponentMeshes { get; set; }

        public bool IsActual { get; set; }

        public bool NeedToFinalize { get; set; }       

        public IPoint3D PastilleLocation { get; set; }

        public IVector3D PastilleDirection { get; set; }

        public double PastilleDiameter { get; set; }

        public double PastilleThickness { get; set; }

        public IMesh SupportRoIMesh { get; set; }

        public LandmarkType Type { get; set; }

        public IPoint3D Point { get; set; }
    }
}
