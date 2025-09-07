using Rhino.Geometry;

namespace IDS.Amace.Operations
{
    public class ScrewBumpTransitionCreationCommandHelper
    {
        public double RoiOffset { get; set; }
        public double TransitionResolution { get; set; }
        public double GapClosingDistance { get; set; }
        public double TransitionOffset { get; set; }
        public bool IsIntersectWithIntersectionEntity { get; set; } 
        public ImplantDirector Director { get; set; }

        public ScrewBumpTransitionCreationCommandHelper(ImplantDirector director)
        {
            IsIntersectWithIntersectionEntity = true;
            Director = director;
        }

        public ScrewBumpTransitionModel CreateScrewBumpTransition(Mesh[] baseParts, Mesh[] medialBumps)
        {
            ScrewBumpTransitionModel result;

            if (IsIntersectWithIntersectionEntity)
            {
                var intersectionEntity = 
                    TransitionIntersectionEntityCommandHelper.HandleGetIntersectionEntity(Director, 
                        Constants.ImplantTransitions.IntersectionEntityResolution, true);

                result = TransitionMaker.CreateScrewBumpTransition(Director, baseParts, medialBumps, intersectionEntity,
                    RoiOffset, TransitionResolution, GapClosingDistance, TransitionOffset);
            }
            else
            {
                result = TransitionMaker.CreateScrewBumpTransition(Director, baseParts, medialBumps, RoiOffset, 
                    TransitionResolution, GapClosingDistance, TransitionOffset);
            }

            return result;
        }


    }
}
