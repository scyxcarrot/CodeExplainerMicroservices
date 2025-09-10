using IDS.Core.V2.Geometries;
using IDS.RhinoInterfaces.Converter;
using Newtonsoft.Json;

namespace IDS.CMF.ScrewQc
{
    public class OsteotomyDistanceSerializableContent
    {
        [JsonProperty("ok")]
        public bool IsOk { get; set; }
        [JsonProperty("d")]
        public double Distance { get; set; }
        [JsonProperty("fs")]
        public bool IsFloatingScrew { get; set; }
        [JsonProperty("pf")]
        public IDSPoint3D PtFrom { get; set; }
        [JsonProperty("pt")]
        public IDSPoint3D PtTo { get; set; }

        public OsteotomyDistanceSerializableContent()
        {
            IsOk = true;
            Distance = double.NaN;
            IsFloatingScrew = false;
            PtFrom = IDSPoint3D.Unset;
            PtTo = IDSPoint3D.Unset;
        }

        public OsteotomyDistanceSerializableContent(
            OsteotomyDistanceContent content)
        {
            IsOk = content.IsOk;
            Distance = content.Distance;
            IsFloatingScrew = content.IsFloatingScrew;
            PtFrom = RhinoPoint3dConverter.ToIDSPoint3D(content.PtFrom);
            PtTo = RhinoPoint3dConverter.ToIDSPoint3D(content.PtTo);
        }
    }
}
