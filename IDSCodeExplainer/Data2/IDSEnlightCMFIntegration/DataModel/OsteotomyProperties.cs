namespace IDS.EnlightCMFIntegration.DataModel
{
    public class OsteotomyProperties : IObjectProperties, IMeshProperties
    {
        public string Name { get; set; }
        public string Guid { get; set; }
        public int Index { get; set; }
        public string Type { get; set; }
        public string[] HandlerIdentifier { get; set; }
        public double[,] HandlerCoordinates { get; set; }
        public double[] TransformationMatrix { get; set; }
        public double[,] Vertices { get; set; }
        public ulong[,] Triangles { get; set; }
        public double Thickness { get; set; }
        public string UiName { get; set; }
        public string InternalName { get; set; }
    }
}
