using IDS.Interface.Geometry;
using System;
using System.Collections.Generic;

namespace IDS.CMFImplantCreation.DTO
{
    public interface IComponentResult
    {
        Guid Id { get; set; }

        IMesh ComponentMesh { get; set; }

        IMesh FinalComponentMesh { get; set; }

        Dictionary<string, IMesh> IntermediateMeshes { get; set; }

        Dictionary<string, object> IntermediateObjects { get; set; }

        Dictionary<string, double> ComponentTimeTakenInSeconds { get; set; }

        double TimeTakenInSeconds { get; set; }

        List<string> ErrorMessages { get; set; }

        double FixingTimeInSeconds { get; set; }
    }
}
