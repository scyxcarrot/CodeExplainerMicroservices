namespace IDS.EnlightCMFIntegration.DataModel
{
    public class StlProperties : IObjectProperties, IMeshProperties
    {
        public string Name { get; set; }
        public string Guid { get; set; }
        public int Index { get; set; }
        public double[] TransformationMatrix { get; set; }
        public double[,] Vertices { get; set; }
        public ulong[,] Triangles { get; set; }
        public string UiName { get; set; }
        public string InternalName { get; set; }
    }
}
