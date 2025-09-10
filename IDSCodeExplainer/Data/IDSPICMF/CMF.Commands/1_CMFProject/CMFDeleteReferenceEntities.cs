using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("B9CA3DA4-0DE7-4600-915E-1AF7AE11B839")]
    [IDSCMFCommandAttributes(~DesignPhase.Draft, IBB.ReferenceEntities)]
    public class CMFDeleteReferenceEntities : CmfCommandBase
    {
        public CMFDeleteReferenceEntities()
        {
            TheCommand = this;
        }

        public static CMFDeleteReferenceEntities TheCommand { get; private set; }

        public override string EnglishName => "CMFDeleteReferenceEntities";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            Locking.UnlockReferenceEntities(director.Document);

            var selectReferenceEntities = new GetObject();
            selectReferenceEntities.SetCommandPrompt("Select reference entities to delete.");
            selectReferenceEntities.EnablePreSelect(false, false);
            selectReferenceEntities.EnablePostSelect(true);
            selectReferenceEntities.AcceptNothing(true);
            selectReferenceEntities.EnableTransparentCommands(false);
            
            while (true)
            {
                var res = selectReferenceEntities.GetMultiple(0, 0);

                if (res == GetResult.Cancel || res == GetResult.Nothing)
                {
                    break;
                }

                if (res == GetResult.Object)
                {
                    var selectedEntities = doc.Objects.GetSelectedObjects(false, false).ToList();
                    DeleteReferenceEntities(director, selectedEntities);
                    // Stop user input
                    break;
                }
            }
            return Result.Success;
        }

        private void DeleteReferenceEntities(CMFImplantDirector director, List<RhinoObject> rhinoObjects)
        {
            var objectManager = new CMFObjectManager(director);
            foreach (var rhobj in rhinoObjects)
            {
                objectManager.DeleteObject(rhobj.Id);
            }
        }
    }
}
