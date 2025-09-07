namespace IDS.Interface.Geometry
{
    public interface IPoint3D
    {
        double X { get; set; }

        double Y { get; set; }

        double Z { get; set; }

        bool EpsilonEquals(IPoint3D other, double epsilon);
    }
}
