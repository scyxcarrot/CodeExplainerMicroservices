using IDS.Core.V2.Geometries;
using IDS.Interface.Implant;
using System;

namespace IDS.CMFImplantCreation.DataModel
{
    public class DotForJson
    {
        public IDSPoint3D Location { get; set; }
        public IDSVector3D Direction { get; set; }
        public string SerializationLabel { get; set; }

        public IDot GetDot()
        {
            var objectType = ImplantDotType.Unset;
            switch (SerializationLabel)
            {
                case "DotControlPoint":
                    objectType = ImplantDotType.DotControlPoint;
                    break;
                case "DotPastille":
                    objectType = ImplantDotType.DotPastille;
                    break;
                default:
                    throw new Exception(
                        $"Unknown SerializationLabel = {SerializationLabel}");
            }

            return new Dot()
            {
                Location = Location,
                Direction = Direction,
                DotType = objectType
            };
        }
    }
}
