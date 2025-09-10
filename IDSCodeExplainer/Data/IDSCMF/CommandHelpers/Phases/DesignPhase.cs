namespace IDS.CMF.Enumerators
{
    [System.Flags]
    public enum DesignPhase : int
    {
        Any = ~0,
        None = 0,
        Initialization = 1,
        Planning = 2,
        PlanningQC = 4,
        Implant = 8,
        //ImplantQC?
        TeethBlock = 16,
        Guide = 32,
        MetalQC = 64,

        Draft = 1024,
    }
}