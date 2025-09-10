using IDS.CMF;
using IDS.CMF.ImplantBuildingBlocks;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using System;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("AA811088-08FA-4292-BA0C-FEE6B8494D17")]
    [CommandStyle(Style.ScriptRunner)]
    public class CMFNotifyImplantBuildingBlockHasChanged : CmfCommandBase
    {
        public CMFNotifyImplantBuildingBlockHasChanged()
        {
            Instance = this;
        }
        
        public static CMFNotifyImplantBuildingBlockHasChanged Instance { get; private set; }

        public override string EnglishName => "CMFNotifyImplantBuildingBlockHasChanged";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var caseId = GetCasePreferenceId();
            var block = GetChangedBuildingBlock();
            var data = director.CasePrefManager.GetCase(caseId);

            data.Graph.NotifyBuildingBlockHasChanged(new[] {block});

            doc.Views.Redraw();
            return Result.Success;
        }

        private Guid GetCasePreferenceId()
        {
            var casePreferenceId = Guid.Empty;
            var casePreferenceIdStr = string.Empty;
            var result = RhinoGet.GetString("CasePreferenceId", false, ref casePreferenceIdStr);
            if (result != Result.Success)
            {
                return casePreferenceId;
            }
            if (!Guid.TryParse(casePreferenceIdStr, out casePreferenceId))
            {
                casePreferenceId = Guid.Empty;
            }
            return casePreferenceId;
        }

        private IBB GetChangedBuildingBlock()
        {
            var changedProperty = IBB.Generic;
            var changedPropertyStr = string.Empty;
            var result = RhinoGet.GetString("ChangedProperty", false, ref changedPropertyStr);
            if (result != Result.Success)
            {
                return changedProperty;
            }

            switch (changedPropertyStr)
            {
                case "ScrewType":
                case "Pastille":
                    changedProperty = IBB.Screw;
                    break;
            }

            return changedProperty;
        }
    }
}
