using IDS.Interface.Geometry;
using System;

namespace IDS.CMF.V2.DataModel
{
    public enum LandmarkType
    {
        Rectangle,
        Triangle,
        Circle
    }

    public class Landmark : ICloneable
    {
        public static string SerializationLabelConst => "Landmark";
        public string SerializationLabel { get; set; }

        public IPoint3D Point { get; set; }

        public LandmarkType LandmarkType { get; set; }

        public Guid Id { get; set; }

        public Landmark()
        {
            SerializationLabel = SerializationLabelConst;
            Id = Guid.NewGuid();
        }

        public object Clone()
        {
            return new Landmark()
            {
                Point = Point,
                LandmarkType = LandmarkType,
                Id = Id
            };
        }
    }
}