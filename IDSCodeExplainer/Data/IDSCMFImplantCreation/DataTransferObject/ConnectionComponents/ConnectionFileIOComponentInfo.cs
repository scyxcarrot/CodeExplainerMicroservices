using IDS.CMFImplantCreation.Helpers;
using IDS.Core.V2.Geometries;
using IDS.Core.V2.MTLS.Operation;
using IDS.Core.V2.Utilities;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using System;
using System.Collections.Generic;

namespace IDS.CMFImplantCreation.DTO
{
    public class ConnectionFileIOComponentInfo : IFileIOComponentInfo
    {
        public Guid Id { get; set; }

        public string DisplayName { get; set; }

        public string ClearanceMeshSTLFilePath { get; set; }

        public List<string> SubtractorsSTLFilePaths { get; set; }

        public List<string> ComponentMeshesSTLFilePaths { get; set; }

        public bool IsActual { get; set; }

        public bool NeedToFinalize { get; set; }

        public string ConnectionCurveJsonFilePath { get; set; }

        public double Width { get; set; }

        public double Thickness { get; set; }

        public IVector3D AverageConnectionDirection { get; set; }

        public string SupportRoIMeshSTLFilePath { get; set; }

        public virtual IComponentInfo ToComponentInfo(IConsole console)
        {
            var component = this.ToDefaultComponentInfo<ConnectionComponentInfo>(console);
            component.Width = Width;
            component.Thickness = Thickness;
            component.AverageConnectionDirection = AverageConnectionDirection;

            if (!string.IsNullOrEmpty(SupportRoIMeshSTLFilePath))
            {
                component.SupportRoIMesh = ImportExport.LoadFromStlFile(console, SupportRoIMeshSTLFilePath);
            }

            if (!string.IsNullOrEmpty(ConnectionCurveJsonFilePath))
            {
                component.ConnectionCurve = JsonUtilities.DeserializeFile<IDSCurveForJson>(ConnectionCurveJsonFilePath).GetICurve();
            }

            return component;
        }
    }
}
