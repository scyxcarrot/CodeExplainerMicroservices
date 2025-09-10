using IDS.CMFImplantCreation.Helpers;
using IDS.Core.V2.MTLS.Operation;
using IDS.Interface.Tools;

namespace IDS.CMFImplantCreation.DTO
{
    public class StitchMeshFileIOComponentInfo : PastilleFileIOComponentInfo
    {
        public string TopMeshSTLFilePath;

        public string BottomMeshSTLFilePath;

        public override IComponentInfo ToComponentInfo(IConsole console)
        {
            var component = this.ToDefaultComponentInfo<StitchMeshComponentInfo>(console);
            component.ScrewHeadPoint = ScrewHeadPoint;
            component.ScrewDirection = ScrewDirection;
            component.Location = Location;
            component.Direction = Direction;
            component.Diameter = Diameter;
            component.Thickness = Thickness;
            component.ScrewType = ScrewType;

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
