namespace IDS.Glenius.Enumerators
{
    [System.Flags]
    public enum DesignPhase : int
    {
        Any = ~0,
        None = 0,
        Initialization = 1,
        Reconstruction = 2,
        Head = 4,
        Screws = 8,
        ScrewQC = 16,
        Plate = 32,
        Scaffold = 64,
        ScaffoldQC = 128,
        Draft = 1024,
    }
}