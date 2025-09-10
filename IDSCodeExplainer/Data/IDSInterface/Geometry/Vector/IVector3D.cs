namespace IDS.Interface.Geometry
{
    public interface IVector3D
    {
        double X { get; set; }

        double Y { get; set; }

        double Z { get; set; }

        double GetLength();

        bool EpsilonEquals(IVector3D other, double epsilon);

        void Unitize();

        bool IsUnitized();
    }
}
