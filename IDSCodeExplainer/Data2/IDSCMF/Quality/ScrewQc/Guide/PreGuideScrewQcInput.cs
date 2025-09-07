using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.V2.ScrewQc;
using System.Collections.Immutable;

namespace IDS.CMF.ScrewQc
{
    public class PreGuideScrewQcInput
    {
        public ImmutableDictionary<Screw, ScrewInfoRecord> ImplantScrewAtOriginalPosWithRecords { get; }

        public PreGuideScrewQcInput(ImmutableDictionary<Screw, ScrewInfoRecord> implantScrewAtOriginalPosWithRecords)
        {
            ImplantScrewAtOriginalPosWithRecords = implantScrewAtOriginalPosWithRecords;
        }
    }
}
