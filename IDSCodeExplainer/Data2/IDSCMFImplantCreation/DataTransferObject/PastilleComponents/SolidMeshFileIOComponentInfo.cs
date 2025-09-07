using IDS.CMFImplantCreation.Helpers;
using IDS.Core.V2.MTLS.Operation;
using IDS.Interface.Tools;

namespace IDS.CMFImplantCreation.DTO
{
    public class SolidMeshFileIOComponentInfo : PastilleFileIOComponentInfo
    {
        public string ExtrusionMesSTLFilePathh { get; set; }
        public string TopMeshSTLFilePath { get; set; }
        public string BottomMeshSTLFilePath { get; set; }

        public override IComponentInfo ToComponentInfo(IConsole console)
        {
            var component = this.ToDefaultComponentInfo<SolidMeshComponentInfo>(console);
            component.ScrewHeadPoint = ScrewHeadPoint;
            component.ScrewDirection = ScrewDirection;
            component.Location = Location;
            component.Direction = Direction;
            component.Diameter = Diameter;
            component.Thickness = Thickness;
            component.ScrewType = ScrewType;

            if (!string.IsNullOrEmpty(ExtrusionMesSTLFilePathh))
            {
                component.ExtrusionMesh = ImportExport.LoadFromStlFile(console, ExtrusionMesSTLFilePathh);
            }

            if (!string.IsNullOrEmpty(TopMeshSTLFilePath))
            {
                component.TopMesh = ImportExport.LoadFromStlFile(console, TopMeshSTLFilePath);
            }

            if (!string.IsNullOrEmpty(BottomMeshSTLFilePath))
            {
                component.BottomMesh = ImportExport.LoadFromStlFile(console, BottomMeshSTLFilePath);
            }

            return component;
        }
    }
}
