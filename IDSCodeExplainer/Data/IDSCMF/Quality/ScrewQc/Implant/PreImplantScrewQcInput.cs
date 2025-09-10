using IDS.CMF.Quality;
using IDS.CMF.Utilities;
using Rhino.Geometry;
using System.Collections.Immutable;

namespace IDS.CMF.ScrewQc
{
    public class PreImplantScrewQcInput
    {
        public ImmutableList<Mesh> OriginalOsteotomyParts { get; }

        public ScrewRegistration ScrewRegistration { get; }

        public PreImplantScrewQcInput(CMFImplantDirector director)
        {
            ScrewRegistration = new ScrewRegistration(director, true);
            var helper = new OriginalPositionedScrewAnalysisHelper(director);
            OriginalOsteotomyParts = helper.GetAllOriginalOsteotomyParts().ToImmutableList();
        }
    }
}
