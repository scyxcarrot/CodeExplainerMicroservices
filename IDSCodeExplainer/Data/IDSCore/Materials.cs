namespace IDS.Core.Fea
{
    public static class Materials
    {
        public static Material Titanium => new Material(name: "Ti4Al6V",
            elasticityEModulus: 110000,
            elasticityPoissonRatio: 0.3,
            ultimateTensileStrength: 860,
            fatigueLimit: 130
        );
    }
}