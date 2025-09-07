using System;
using IDS.CMF;
using IDS.CMF.DataModel;

namespace IDS.PICMF.Forms
{
    public interface ITeethBlockViewModel
    {
        string ColumnTitle { get; set; } 
        Delegate CommandExecuted { get; set; }
        ProPlanImportPartType SelectedPartType { get; set; }
        bool SetEnabled(CMFImplantDirector director);
    }

    public class TeethBlockViewModel : ITeethBlockViewModel
    {
        public string ColumnTitle { get; set; }
        public Delegate CommandExecuted { get; set; }
        public ProPlanImportPartType SelectedPartType { get; set; }

        public virtual bool SetEnabled(CMFImplantDirector director)
        {
            return false;
        }
    }
}
