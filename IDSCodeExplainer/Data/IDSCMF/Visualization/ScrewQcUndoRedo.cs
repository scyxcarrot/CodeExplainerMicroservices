using IDS.CMF.CasePreferences;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.Core.V2.TreeDb.Model;

namespace IDS.CMF.Visualization
{
    public struct ScrewQcUndoRedo
    {
        public Screw NewScrew { get; set; }
        public Screw OldScrew { get; set; }
        public ImplantDataModel NewImplantDataModel { get; set; }
        public ImplantDataModel OldImplantDataModel { get; set; }
        public CasePreferenceDataModel CasePreferenceDataModel { get; set; }
        public IDSDocument IdsDocument;
    }
}