using System.Collections.Generic;
using IDS.CMF;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;

namespace IDS.PICMF.Forms
{
    public class TeethBlockMarkSurfaceViewModel : TeethBlockViewModel
    {
        public TeethBlockMarkSurfaceViewModel()
        {
            ColumnTitle = "Mark Surface";
        }

        public override bool SetEnabled(CMFImplantDirector director)
        {
            var isIbbPresent = false;

            if (SelectedPartType == ProPlanImportPartType.MaxillaCast)
            { 
                isIbbPresent = TeethSupportedGuideUtilities.CheckIfIbbsArePresent(director, new List<IBB>() { IBB.LimitingSurfaceMaxilla }, true);
            }
            else
            {
                isIbbPresent = TeethSupportedGuideUtilities.CheckIfIbbsArePresent(director, new List<IBB>() { IBB.LimitingSurfaceMandible }, true);
            }

            return isIbbPresent;
        }
    }
}
