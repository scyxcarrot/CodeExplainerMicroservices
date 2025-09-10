using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.CustomMainObjects;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("5EE7B8DE-1ABD-4725-B5FE-3FF6339336D3")]
    [IDSCMFCommandAttributes(DesignPhase.Implant, IBB.ImplantTransition)]
    public class CMFDeleteImplantTransition : CmfCommandBase
    {
        public CMFDeleteImplantTransition()
        {
            TheCommand = this;
            VisualizationComponent = new CMFImplantTransitionVisualization();
        }

        public static CMFDeleteImplantTransition TheCommand { get; private set; }

        public override string EnglishName => "CMFDeleteImplantTransition";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            Locking.UnlockImplantTransition(doc);

            var selectImplantTransition = new GetObject();
            selectImplantTransition.SetCommandPrompt("Select implant transition(s) to delete. Press <Enter> to delete the selected transitions");
            selectImplantTransition.EnablePreSelect(false, false);
            selectImplantTransition.EnablePostSelect(true);
            selectImplantTransition.AcceptNothing(true);
            selectImplantTransition.EnableHighlight(true);
            selectImplantTransition.EnableTransparentCommands(false);

            var result = Result.Failure;
            
            while (true)
            {
                var res = selectImplantTransition.GetMultiple(0, 0);

                if (res == GetResult.Cancel)
                {
                    result = Result.Cancel;
                    break;
                }

                if (res == GetResult.Object)
                {
                    var selectedImplantTransition = doc.Objects.GetSelectedObjects(false, false).ToList();
                    var objectManager = new CMFObjectManager(director);

                    var allRemoved = DeleteImplantTransition(objectManager, selectedImplantTransition, out var deletedTransitionIds);
                    var implantSupportManager = new ImplantSupportManager(objectManager);
                    implantSupportManager.SetDependentImplantSupportsOutdated(deletedTransitionIds);

                    if (!allRemoved)
                    {
                        IDSPluginHelper.WriteLine(LogCategory.Warning, "Some implant transition(s) failed to be removed, "+ 
                                                                       "it might be due to the implant transition(s) already been removed twice");
                    }

                    result = Result.Success;
                    break;
                }

                if (res == GetResult.Nothing)
                {
                    result = Result.Nothing;
                    break;
                }
            }

            CleanUp(doc);
            return result;
        }

        private bool DeleteImplantTransition(CMFObjectManager objectManager, List<RhinoObject>rhinoObjects, out List<Guid> deletedImplantIds)
        {
            var result = true;
            deletedImplantIds = new List<Guid>();

            foreach (var rhinoObject in rhinoObjects)
            {
                if (!objectManager.DeleteObject(rhinoObject.Id))
                {
                    result = false;
                    continue;
                }
                deletedImplantIds.Add(rhinoObject.Id);
            }
            return result;
        }

        private void CleanUp(RhinoDoc doc)
        {
            doc.Objects.UnselectAll();
            doc.Views.Redraw();
        }
    }
}
