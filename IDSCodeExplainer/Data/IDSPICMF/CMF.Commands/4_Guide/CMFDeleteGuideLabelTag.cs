using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Utilities;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("B9CCAC32-6751-4255-81D6-3A6259FCED77")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Guide, IBB.GuideFixationScrewLabelTag)]
    public class CMFDeleteGuideLabelTag : CmfCommandBase
    {
        public CMFDeleteGuideLabelTag()
        {
            TheCommand = this;
            VisualizationComponent = new CMFGuideLabelTagVisualization();
        }
        
        public static CMFDeleteGuideLabelTag TheCommand { get; private set; }
        
        public override string EnglishName => "CMFDeleteGuideLabelTag";
        
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            Locking.UnlockGuideLabelTags(director.Document);
            
            var selectLabelTag = new GetObject();
            selectLabelTag.SetCommandPrompt("Select label tag(s) to delete.");
            selectLabelTag.EnablePreSelect(false, false);
            selectLabelTag.EnablePostSelect(true);
            selectLabelTag.AcceptNothing(true);
            selectLabelTag.EnableTransparentCommands(false);

            var result = Result.Failure;

            while (true)
            {
                var res = selectLabelTag.GetMultiple(0, 0);

                if (res == GetResult.Cancel || res == GetResult.Nothing)
                {
                    break;
                }

                if (res == GetResult.Object)
                {
                    var selectedLabelTags = doc.Objects.GetSelectedObjects(false, false).ToList();
                    var removed = DeleteLabelTags(director, selectedLabelTags);
                    result = removed ? Result.Success : Result.Failure;
                    
                    doc.ClearUndoRecords(true);
                    doc.ClearRedoRecords();

                    // Stop user input
                    break;
                }
            }

            return result;
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, CMFImplantDirector director)
        {
            doc.Objects.UnselectAll();
            doc.Views.Redraw();
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, CMFImplantDirector director)
        {
            doc.Objects.UnselectAll();
            doc.Views.Redraw();
        }

        private bool DeleteLabelTags(CMFImplantDirector director, List<RhinoObject> rhinoObjects)
        {
            var objManager = new CMFObjectManager(director);

            var screwLabelTagHelper = new ScrewLabelTagHelper(director);

            var rhinoObj = objManager.GetBuildingBlock(IBB.GuideSurfaceWrap);

            Mesh lowLoDConstraintMesh;
            objManager.GetBuildingBlockLoDLow(rhinoObj.Id, out lowLoDConstraintMesh);

            foreach (var rhobj in rhinoObjects)
            {
                var screw = screwLabelTagHelper.GetScrewOfLabelTag(rhobj.Id);

                var screwItSharedWith = screw.GetScrewItSharedWith();
                screwLabelTagHelper.DoDeleteLabelTag(screw, lowLoDConstraintMesh);
                DoDependencyManagement(objManager, screw);

                screwItSharedWith.ForEach(s =>
                {
                    screwLabelTagHelper.DoDeleteLabelTag(s, lowLoDConstraintMesh);
                    DoDependencyManagement(objManager, s);
                });
            }

            return true;
        }

        private void DoDependencyManagement(CMFObjectManager objManager, Screw screw)
        {
            var casePref = objManager.GetGuidePreference(screw);
            casePref.Graph.NotifyBuildingBlockHasChanged(new[] { IBB.GuideFixationScrew, IBB.GuideFixationScrewLabelTag });
        }
    }
}