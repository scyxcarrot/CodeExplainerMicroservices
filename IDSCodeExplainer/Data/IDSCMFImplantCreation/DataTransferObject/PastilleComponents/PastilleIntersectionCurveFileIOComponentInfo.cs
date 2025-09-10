using IDS.CMFImplantCreation.Helpers;
using IDS.Core.V2.MTLS.Operation;
using IDS.Interface.Tools;

namespace IDS.CMFImplantCreation.DTO
{
    public class PastilleIntersectionCurveFileIOComponentInfo : PastilleFileIOComponentInfo
    {
        public override IComponentInfo ToComponentInfo(IConsole console)
        {
            var component = this.ToDefaultComponentInfo<PastilleIntersectionCurveComponentInfo>(console);
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
