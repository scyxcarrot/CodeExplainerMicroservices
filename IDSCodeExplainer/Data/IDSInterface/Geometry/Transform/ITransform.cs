namespace IDS.Interface.Geometry
{
    public interface ITransform
    {
        double M00 { get; set; }
        double M01 { get; set; }
        double M02 { get; set; }
        double M03 { get; set; }
        double M10 { get; set; }
        double M11 { get; set; }
        double M12 { get; set; }
        double M13 { get; set; }
        double M20 { get; set; }
        double M21 { get; set; }
        double M22 { get; set; }
        double M23 { get; set; }
        double M30 { get; set; }
        double M31 { get; set; }
        double M32 { get; set; }
        double M33 { get; set; }
    }
}
