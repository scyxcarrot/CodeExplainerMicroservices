using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.CustomMainObjects;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Utilities;
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
    [System.Runtime.InteropServices.Guid("F926D7A7-5A37-42F0-8AF4-0E9A1E195200")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Implant, IBB.ImplantMargin)]
    public class CMFDeleteMargin : CmfCommandBase
    {
        public CMFDeleteMargin()
        {
            TheCommand = this;
            VisualizationComponent = new CMFDeleteImplantMarginVisualization();
        }

        public static CMFDeleteMargin TheCommand { get; private set; }
        public override string EnglishName => "CMFDeleteImplantMargin";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            Locking.UnlockImplantMargin(doc);

            var selectImplantMargin = new GetObject();
            selectImplantMargin.SetCommandPrompt("Select implant margin(s) to delete. Press <Enter> to delete the selected margins");
            selectImplantMargin.EnablePreSelect(false, false);
            selectImplantMargin.EnablePostSelect(true);
            selectImplantMargin.AcceptNothing(true);
            selectImplantMargin.EnableHighlight(true);
            selectImplantMargin.EnableTransparentCommands(false);

            var result = Result.Failure;
            
            while (true)
            {
                var res = selectImplantMargin.GetMultiple(0, 0);

                if (res == GetResult.Cancel)
                {
                    result = Result.Cancel;
                    break;
                }

                if (res == GetResult.Object)
                {
                    var selectedImplantMargin = doc.Objects.GetSelectedObjects(false, false).ToList();
                    var objectManager = new CMFObjectManager(director);

                    var allRemoved = DeleteImplantMargin(objectManager, selectedImplantMargin, out var deletedMarginIds);
                    var implantSupportManager = new ImplantSupportManager(objectManager);
                    implantSupportManager.SetDependentImplantSupportsOutdated(deletedMarginIds);

                    if (!allRemoved)
                    {
                        IDSPluginHelper.WriteLine(LogCategory.Warning, "Some implant margin failed to removed, " +
                                                                       "it might cause by the implant margins already been removed twice");
                    }
                    else
                    {
                        var marginIds = selectedImplantMargin.Select(m => m.Id).ToList();
                        var marginHelper = new ImplantMarginHelper(director);
                        marginHelper.InvalidateDependentTransitions(marginIds, out var dependentTransitionIds);

                        implantSupportManager.SetDependentImplantSupportsOutdated(dependentTransitionIds);
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

        private bool DeleteImplantMargin(CMFObjectManager objectManager, List<RhinoObject>rhinoObjects, out List<Guid> deletedMarginIds)
        {
            var result = true;
            deletedMarginIds = new List<Guid>();

            foreach (var rhinoObject in rhinoObjects)
            {
                var deleted = objectManager.DeleteObject(rhinoObject.Id);
                if (deleted)
                {
                    deletedMarginIds.Add(rhinoObject.Id);
                }

                result &= deleted;
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
