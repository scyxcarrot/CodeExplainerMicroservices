using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.V2.ScrewQc;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using System;

namespace IDS.CMF.ScrewQc
{
    public class ScrewInfoRecordHelper
    {
        private readonly CMFImplantDirector _director;

        public ScrewInfoRecordHelper(CMFImplantDirector director)
        {
            _director = director;
        }

        private Screw GetScrewById(Guid id)
        {
            var rhinoObject = _director.Document.Objects.Find(id);
            // If null, 'is' will be false, so skip null check
            if (!(rhinoObject is Screw screw))
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "The screw might be removed or not exist");
                return null;
            }

            return screw;
        }

        public Screw GetScrewByRecord(ScrewInfoRecord record)
        {
            return GetScrewById(record.Id);
        }

        public ScrewInfoRecord GetRecordById(Guid id)
        {
            var screw = GetScrewById(id);
            if (screw == null)
            {
                return null;
            }

            return ScrewQcUtilities.IsGuideScrew(screw) ?
                (ScrewInfoRecord)new GuideScrewInfoRecord(screw) :
                (ScrewInfoRecord)new ImplantScrewInfoRecord(screw);
        }
    }
}
