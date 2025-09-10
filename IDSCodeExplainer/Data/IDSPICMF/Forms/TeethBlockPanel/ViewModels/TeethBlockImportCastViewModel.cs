using IDS.CMF;

namespace IDS.PICMF.Forms
{
    public class TeethBlockImportCastViewModel : TeethBlockViewModel
    {
        public TeethBlockImportCastViewModel()
        {
            ColumnTitle = "Import Cast";
        }

        public override bool SetEnabled(CMFImplantDirector director)
        {
            // Import cast functionality is always enabled
            return true;
        }
    }
}
