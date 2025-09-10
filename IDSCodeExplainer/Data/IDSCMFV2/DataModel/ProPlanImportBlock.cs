using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Drawing;
using System.Runtime.Serialization;

namespace IDS.CMF.DataModel
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ProPlanImportPartType
    {
        [EnumMember(Value = "OsteotomyPlane")]
        OsteotomyPlane,
        [EnumMember(Value = "Nerve")]
        Nerve, 
        [EnumMember(Value = "NerveRegistered")]
        NerveRegistered,
        [EnumMember(Value = "Teeth")]
        Teeth,
        [EnumMember(Value = "Other")]
        Other,
        [EnumMember(Value = "Metal")]
        Metal,
        [EnumMember(Value = "Graft")]
        Graft,
        [EnumMember(Value = "Bone")]
        Bone,
        [EnumMember(Value = "NonProPlanItem")]
        NonProPlanItem,
        [EnumMember(Value = "MandibleCast")]
        MandibleCast,
        [EnumMember(Value = "MaxillaCast")]
        MaxillaCast
    }

    public struct ProPlanImportBlock
    {
        public string Description { get; set; }
        public string PartNamePattern { get; set; }
        public Color Color { get; set; }
        public ProPlanImportPartType PartType { get; set; }
        public string SubLayer { get; set; }
        public bool IsImplantPlacable { get; set; }
        public bool IsDefaultAnatomicalObstacle { get; set; }
        public bool ImportInIDS { get; set; }
    }
}
