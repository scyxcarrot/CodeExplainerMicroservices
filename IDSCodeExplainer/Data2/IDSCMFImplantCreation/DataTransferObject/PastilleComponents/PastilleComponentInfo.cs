using IDS.CMFImplantCreation.Configurations;
using IDS.CMFImplantCreation.DataModel;
using IDS.Interface.Geometry;
using System;
using System.Collections.Generic;

namespace IDS.CMFImplantCreation.DTO
{
    public class PastilleComponentInfo : IComponentInfo
    {
        public Guid Id { get; set; }

        public string DisplayName { get; set; }
                
        public IMesh ClearanceMesh { get; set; }

        public List<IMesh> Subtractors { get; set; }

        public List<IMesh> ComponentMeshes { get; set; }

        public bool IsActual { get; set; }

        public bool NeedToFinalize { get; set; }

        public IPoint3D ScrewHeadPoint { get; set; }

        public IVector3D ScrewDirection { get; set; }

        public IPoint3D Location { get; set; }

        public IVector3D Direction { get; set; }

        public double Diameter { get; set; }

        public double Thickness { get; set; }

        public string ScrewType { get; set; }

        public IMesh SupportRoIMesh { get; set; }

        internal Pastille ToDataModel(PastilleConfiguration configuration)
        {
            return new Pastille
            {
                Location = Location,
                Direction = Direction,
                Diameter = Diameter,
                Thickness = Thickness,
                StampImprintShapeWidth = configuration.StampImprintShapeWidth,
                StampImprintShapeHeight = configuration.StampImprintShapeHeight,
                StampImprintShapeOffset = configuration.StampImprintShapeOffset,
                StampImprintShapeSectionHeightRatio = configuration.StampImprintShapeSectionHeightRatio,
                StampImprintShapeCreationMaxPastilleThickness = configuration.StampImprintShapeCreationMaxPastilleThickness
            };
        }
    }
}
