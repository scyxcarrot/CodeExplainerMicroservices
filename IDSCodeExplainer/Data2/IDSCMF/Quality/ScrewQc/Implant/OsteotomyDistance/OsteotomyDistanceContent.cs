using IDS.RhinoInterfaces.Converter;
using Rhino.Geometry;
using System;

namespace IDS.CMF.ScrewQc
{
    public class OsteotomyDistanceContent
    {
        public bool IsOk { get; set; }
        public double Distance { get; set; }
        public bool IsFloatingScrew { get; set; }
        public Point3d PtFrom { get; set; }
        public Point3d PtTo { get; set; }

        public OsteotomyDistanceContent()
        {
            IsOk = true;
            Distance = Double.NaN;
            IsFloatingScrew = false;
            PtFrom = Point3d.Unset;
            PtTo = Point3d.Unset;
        }

        public OsteotomyDistanceContent(OsteotomyDistanceSerializableContent serializableContent)
        {
            IsOk = serializableContent.IsOk;
            Distance = serializableContent.Distance;
            IsFloatingScrew = serializableContent.IsFloatingScrew;
            PtFrom = RhinoPoint3dConverter.ToPoint3d(serializableContent.PtFrom);
            PtTo = RhinoPoint3dConverter.ToPoint3d(serializableContent.PtTo);
        }
    }
}
