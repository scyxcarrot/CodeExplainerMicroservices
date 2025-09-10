namespace IDS.EnlightCMFIntegration.DataModel
{
    public class PlaneProperties : IObjectProperties
    {
        public string Name { get; set; }
        public string Guid { get; set; }
        public int Index { get; set; }
        public double[] TransformationMatrix { get; set; }
        public double[] Origin { get; set; }
        public double[] Normal { get; set; }
        public string UiName { get; set; }
        public string InternalName { get; set; }
    }
}
