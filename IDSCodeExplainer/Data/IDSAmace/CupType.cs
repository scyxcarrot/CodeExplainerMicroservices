namespace IDS.Amace.ImplantBuildingBlocks
{
    public struct CupType
    {
        public double CupThickness { get; set; }
        public double PorousThickness { get; set; }
        public CupDesign CupDesign { get; set; }

        public CupType(double cupThickness, double porousThickness, CupDesign cupDesign)
        {
            CupThickness = cupThickness;
            PorousThickness = porousThickness;
            CupDesign = cupDesign;
        }
    }

}
