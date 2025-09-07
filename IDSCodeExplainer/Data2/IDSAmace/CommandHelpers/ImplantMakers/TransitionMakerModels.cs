using Rhino.Geometry;

namespace IDS.Amace.Operations
{
    public class ScrewBumpTransitionModel
    {
        public Mesh BaseModelInput;
        public Mesh ScrewBumpTransitions;
    }

    public class QcApprovedExportTransitionModel
    {
        public Mesh PlateWithTransitionForReporting;
        public Mesh FlangeTransitionForFinalization;
        public Mesh BumpTransitionForFinalization;
    }
}
