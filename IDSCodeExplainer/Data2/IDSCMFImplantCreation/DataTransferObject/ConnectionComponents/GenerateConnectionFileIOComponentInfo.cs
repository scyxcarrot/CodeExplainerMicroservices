using IDS.CMFImplantCreation.Helpers;
using IDS.Core.V2.Geometries;
using IDS.Core.V2.MTLS.Operation;
using IDS.Core.V2.Utilities;
using IDS.Interface.Tools;

namespace IDS.CMFImplantCreation.DTO
{
    public class GenerateConnectionFileIOComponentInfo : ConnectionFileIOComponentInfo
    {
        public string IntersectionCurveJsonFilePath { get; set; }

        public double WrapBasis { get; set; }

        public bool IsSharpConnection { get; set; }

        public override IComponentInfo ToComponentInfo(IConsole console)
        {
            var component = this
                .ToDefaultComponentInfo<GenerateConnectionComponentInfo>(
                    console);
            component.Width = Width;
            component.Thickness = Thickness;
            component.WrapBasis = WrapBasis;
            component.IsSharpConnection = IsSharpConnection;

            if (!string.IsNullOrEmpty(SupportRoIMeshSTLFilePath))
            {
                component.SupportRoIMesh = ImportExport.LoadFromStlFile(
                    console, SupportRoIMeshSTLFilePath);
            }

            if (!string.IsNullOrEmpty(ConnectionCurveJsonFilePath))
            {
                component.ConnectionCurve = 
                    JsonUtilities.DeserializeFile<IDSCurveForJson>(
                    ConnectionCurveJsonFilePath).GetICurve();
            }

            if (!string.IsNullOrEmpty(IntersectionCurveJsonFilePath))
            {
                component.IntersectionCurve =
                    JsonUtilities.DeserializeFile<IDSCurveForJson>(
                        IntersectionCurveJsonFilePath).GetICurve();
            }

            return component;
        }
    }
}
