namespace IDS.Amace.Enumerators
{
    /**
     * Distinct phases in the implant design process
     *
     * Values can be used as bit flags, e.g. to indicate that
     * an item is valid in specific design phases. For example:
     * `allowable_phases = CupSkirt | PlateScrews`
     */

    [System.Flags]
    public enum DesignPhase : int
    {
        // WARNING: VALUES MUST BE INTEGER POWERS OF TWO to be usable as bit flags
        //None = 0, // Items with this flag are valid in all design phases
        //Initialization = 1,
        //Cup = 2, // Cup positioning, skirt creation, scaffold creation, reaming
        //PlateScrews = 4, // Plate creation, Screw positioning
        //Any = ~0, // All bits are 1: all phases

        Any = ~0,
        None = 0,
        Initialization = 1,
        Cup = 2,
        Reaming = 4,
        Undercut = 8,
        Skirt = 16,
        Scaffold = 32,
        CupQC = 64,
        Screws = 128,
        Plate = 256,
        ImplantQC = 512,
        Draft = 1024,
        Export = 2048,

        Development = 4096,

        QC = 64 + 512,
    }
}