using IDS.CMFImplantCreation.Helpers;
using IDS.Core.V2.Geometries;
using IDS.Core.V2.MTLS.Operation;
using IDS.Core.V2.Utilities;
using IDS.Interface.Tools;

namespace IDS.CMFImplantCreation.DTO
{
    public class ConnectionIntersectionCurveFileIOComponentInfo : ConnectionFileIOComponentInfo
    {
        public override IComponentInfo ToComponentInfo(IConsole console)
        {
            var component = this.ToDefaultComponentInfo<ConnectionIntersectionCurveComponentInfo>(console);
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
