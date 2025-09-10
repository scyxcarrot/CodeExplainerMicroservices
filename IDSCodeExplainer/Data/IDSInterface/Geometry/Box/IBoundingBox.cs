namespace IDS.Interface.Geometry
{
    public interface IBoundingBox
    {
        IPoint3D Min { get; }

        IPoint3D Max { get; }
    }
}
