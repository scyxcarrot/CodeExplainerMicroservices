using IDS.Interface.Geometry;
using System;
using System.Collections.Generic;

namespace IDS.CMFImplantCreation.DTO
{
    public class LandmarkComponentResult : IComponentResult
    {
        public Guid Id { get; set; }

        public IMesh ComponentMesh { get; set; }

        public IMesh FinalComponentMesh { get; set; }

        public Dictionary<string, IMesh> IntermediateMeshes { get; set; }

        public Dictionary<string, object> IntermediateObjects { get; set; }

        public Dictionary<string, double> ComponentTimeTakenInSeconds { get; set; }

        public double TimeTakenInSeconds { get; set; }

        public List<string> ErrorMessages { get; set; }

        public double FixingTimeInSeconds { get; set; }
    }
}
