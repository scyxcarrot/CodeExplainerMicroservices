using IDS.Amace.Preferences;

namespace IDS.Amace.Constants
{
    public static class AcetabularPlane
    {
        public const double Size = 50.0;
    }

    public static class ContourPlane
    {
        public const double Size = 1000.0;
    }

    public static class ImplantTransitions
    {
        public static double IntersectionEntityResolution => AmacePreferences.GetTransitionIntermediatesParams().IntersectionEntityWrapResolution;
    }
}
