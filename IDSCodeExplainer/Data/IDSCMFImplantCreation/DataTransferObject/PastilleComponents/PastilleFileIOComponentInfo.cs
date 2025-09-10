using IDS.CMFImplantCreation.Helpers;
using IDS.Core.V2.MTLS.Operation;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using System;
using System.Collections.Generic;

namespace IDS.CMFImplantCreation.DTO
{
    public class PastilleFileIOComponentInfo : IFileIOComponentInfo
    {
        public Guid Id { get; set; }

        public string DisplayName { get; set; }

        public bool IsActual { get; set; }

        public bool NeedToFinalize { get; set; }

        public IPoint3D ScrewHeadPoint { get; set; }

        public IVector3D ScrewDirection { get; set; }

        public IPoint3D Location { get; set; }

        public IVector3D Direction { get; set; }

        public double Diameter { get; set; }

        public double Thickness { get; set; }

        public string ScrewType { get; set; }

        public string ClearanceMeshSTLFilePath { get; set; }

        public string SupportRoIMeshSTLFilePath { get; set; }

        public List<string> SubtractorsSTLFilePaths { get; set; }

        public List<string> ComponentMeshesSTLFilePaths { get; set; }

        public virtual IComponentInfo ToComponentInfo(IConsole console)
        {
            var component = this.ToDefaultComponentInfo<PastilleComponentInfo>(console);
            component.ScrewHeadPoint = ScrewHeadPoint;
            component.ScrewDirection = ScrewDirection;
            component.Location = Location;
            component.Direction = Direction;
            component.Diameter = Diameter;
            component.Thickness = Thickness;
            component.ScrewType = ScrewType;

            if (!string.IsNullOrEmpty(SupportRoIMeshSTLFilePath))
            {
                component.SupportRoIMesh = ImportExport.LoadFromStlFile(console, SupportRoIMeshSTLFilePath);
            }

            return component;
        }
    }
}
