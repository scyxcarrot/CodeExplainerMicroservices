namespace IDS.EnlightCMFIntegration.DataModel
{
    public interface IObjectProperties
    {
        string Name { get; set; }
        string Guid { get; set; }
        int Index { get; set; }
        double[] TransformationMatrix { get; set; }
        string UiName { get; set; }
        string InternalName { get; set; }
    }
}
