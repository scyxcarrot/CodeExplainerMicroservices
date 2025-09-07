namespace IDS.EnlightCMFIntegration.DataModel
{
    public interface IMeshProperties
    {
        double[,] Vertices { get; set; }
        ulong[,] Triangles { get; set; }
    }
}
