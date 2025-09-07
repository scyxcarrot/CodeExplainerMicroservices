using IDS.CMFImplantCreation.Helpers;
using IDS.Core.V2.Geometries;
using IDS.Core.V2.MTLS.Operation;
using IDS.Core.V2.Utilities;
using IDS.Interface.Tools;

namespace IDS.CMFImplantCreation.DTO
{
    public class PatchFileIOComponentInfo : PastilleFileIOComponentInfo
    {
        public string SupportRoIMeshSTLFilePath { get; set; }

        public string IntersectionCurveJsonFilePath { get; set; }

        public override IComponentInfo ToComponentInfo(IConsole console)
        {
            var component = this.ToDefaultComponentInfo<PatchComponentInfo>(console);
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

            if (!string.IsNullOrEmpty(IntersectionCurveJsonFilePath))
            {
                component.IntersectionCurve = JsonUtilities.DeserializeFile<IDSCurveForJson>(IntersectionCurveJsonFilePath).GetICurve();
            }

            return component;
        }
    }
}
