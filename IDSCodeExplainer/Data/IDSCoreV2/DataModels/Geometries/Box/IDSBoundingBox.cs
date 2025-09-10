using IDS.Interface.Geometry;

namespace IDS.Core.V2.DataModels.Geometries
{
    public class IDSBoundingBox: IBoundingBox
    {
        public IPoint3D Min { get; }
        public IPoint3D Max { get; }

        public IDSBoundingBox(IPoint3D min, IPoint3D max)
        {
            Min = min;
            Max = max;
        }
    }
}
