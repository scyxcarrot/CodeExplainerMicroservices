using IDS.CMF.V2.DataModel;
using IDS.CMFImplantCreation.Helpers;
using IDS.Core.V2.MTLS.Operation;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using System;
using System.Collections.Generic;

namespace IDS.CMFImplantCreation.DTO
{
    public class LandmarkFileIOComponentInfo : IFileIOComponentInfo
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

        public string ClearanceMeshSTLFilePath { get; set; }

        public string SupportRoIMeshSTLFilePath { get; set; }

        public List<string> SubtractorsSTLFilePaths { get; set; }

        public List<string> ComponentMeshesSTLFilePaths { get; set; }

        public virtual IComponentInfo ToComponentInfo(IConsole console)
        {
            var component = this.ToDefaultComponentInfo<LandmarkComponentInfo>(console);
            component.PastilleLocation = PastilleLocation;
            component.PastilleDirection = PastilleDirection;
            component.PastilleDiameter = PastilleDiameter;
            component.PastilleThickness = PastilleThickness;
            component.Type = Type;
            component.Point = Point;

            if (!string.IsNullOrEmpty(SupportRoIMeshSTLFilePath))
            {
                component.SupportRoIMesh = ImportExport.LoadFromStlFile(console, SupportRoIMeshSTLFilePath);
            }

            return component;
        }
    }
}
