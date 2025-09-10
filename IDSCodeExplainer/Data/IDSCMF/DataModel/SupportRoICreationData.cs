namespace IDS.CMF.DataModel
{
    public class SupportRoICreationData
    {
        public bool HasTeethIntegration { get; set; }
        public double ResultingOffsetForTeeth { get; set; }
    }

    public class ImplantSupportRoICreationData : SupportRoICreationData
    {
        public bool HasMetalIntegration { get; set; }
        public double ResultingOffsetForRemovedMetal { get; set; }
        public double ResultingOffsetForRemainedMetal { get; set; }
    }

    public class GuideSupportRoICreationData : SupportRoICreationData
    {
        public bool HasMetalIntegration { get; set; }
        public double ResultingOffsetForMetal { get; set; }
    }
}
