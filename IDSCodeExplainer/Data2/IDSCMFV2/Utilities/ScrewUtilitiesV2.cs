namespace IDS.CMF.V2.Utilities
{
    public static class ScrewUtilitiesV2
    {
        public static string GetScrewNumberWithImplantNumber(int screwIndex, int numCase)
        {
            return $"{screwIndex}.I{numCase}";
        }
    }
}
