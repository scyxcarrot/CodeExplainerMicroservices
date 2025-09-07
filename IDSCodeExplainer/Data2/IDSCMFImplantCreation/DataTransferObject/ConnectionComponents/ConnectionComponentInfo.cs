using IDS.Interface.Geometry;
using System;
using System.Collections.Generic;

namespace IDS.CMFImplantCreation.DTO
{
    public class ConnectionComponentInfo : IComponentInfo
    {
        public Guid Id { get; set; }

        public string DisplayName { get; set; }
                
        public IMesh ClearanceMesh { get; set; }

        public List<IMesh> Subtractors { get; set; }

        public List<IMesh> ComponentMeshes { get; set; }

        public bool IsActual { get; set; }

        public bool NeedToFinalize { get; set; }

        public ICurve ConnectionCurve { get; set; }

        public double Width { get; set; }

        public double Thickness { get; set; }

        public IVector3D AverageConnectionDirection { get; set; }

        public IMesh SupportRoIMesh { get; set; }

        public IMesh SupportMeshFull { get; set; }
    }
}
