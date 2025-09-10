using IDS.CMFImplantCreation.Helpers;
using IDS.Interface.Tools;

namespace IDS.CMFImplantCreation.DTO
{
    public class ScrewStampImprintFileIOComponentInfo : PastilleFileIOComponentInfo
    {
        public override IComponentInfo ToComponentInfo(IConsole console)
        {
            var component = this.ToDefaultComponentInfo<ScrewStampImprintComponentInfo>(console);
            component.ScrewHeadPoint = ScrewHeadPoint;
            component.ScrewDirection = ScrewDirection;
            component.Location = Location;
            component.Direction = Direction;
            component.Diameter = Diameter;
            component.Thickness = Thickness;
            component.ScrewType = ScrewType;

            return component;
        }
    }
}
