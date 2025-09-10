using System.Linq;
using IDS.CMF;
using IDS.CMF.Utilities;

namespace IDS.PICMF.Forms
{
    public class TeethBlockCreateLimitingSurfaceViewModel : TeethBlockViewModel
    {
        public TeethBlockCreateLimitingSurfaceViewModel()
        {
            ColumnTitle = "Create Limiting Surface";
        }

        public override bool SetEnabled(CMFImplantDirector director)
        {
            var objManager = new CMFObjectManager(director);
            TeethSupportedGuideUtilities.GetCastPartAvailability(objManager, out var availableParts, out var _, SelectedPartType);
            return availableParts.Any();
        }
    }
}
