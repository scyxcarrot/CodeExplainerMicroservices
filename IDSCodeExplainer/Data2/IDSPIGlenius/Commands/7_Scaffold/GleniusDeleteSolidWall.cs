using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("70559353-1c58-47ee-b403-92af0bb63a3f")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommand(DesignPhase.Scaffold, IBB.SolidWallCurve, IBB.SolidWallWrap)]
    public class GleniusDeleteSolidWall : CommandBase<GleniusImplantDirector>
    {
        static GleniusDeleteSolidWall _instance;
        public GleniusDeleteSolidWall()
        {
            _instance = this;
            VisualizationComponent = new SolidWallGenericVisualization();
        }

        ///<summary>The only instance of the GleniusDeleteSolidWall command.</summary>
        public static GleniusDeleteSolidWall Instance => _instance;
     
        public override string EnglishName => "GleniusDeleteSolidWall";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            List<Guid> solidWallWrapIds;

            var getObject = new GetObject();
            getObject.SetCommandPrompt("Select SolidWallWrap to delete");
            getObject.EnablePreSelect(false, false);
            getObject.EnablePostSelect(true);
            getObject.AcceptNothing(true);
            getObject.EnableTransparentCommands(false);

            Locking.UnlockSolidWallWrap(doc);

            var res = getObject.GetMultiple(0, 0);

            if (res == GetResult.Object)
            {
                solidWallWrapIds = doc.Objects.GetSelectedObjects(false, false).Select(x => x.Id).ToList();
            }
            else
            {
                return Result.Failure;
            }

            // Ask confirmation and delete if user clicks 'Yes'
            var result = Rhino.UI.Dialogs.ShowMessageBox(
                "Are you sure you want to delete the selected solid wall(s)?",
                "Delete solid wall(s)?",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Exclamation);

            if (result == DialogResult.Yes)
            {
                foreach (var id in solidWallWrapIds)
                {
                    if (!director.SolidWallObjectManager.DeleteSolidWall(id))
                    {
                        doc.Views.Redraw();
                        return Result.Failure;
                    }
                }

                //Because when Solid wall manager delete it, both of these two got modified/deleted
                HandleDependencyManagement(director);
                doc.Views.Redraw();
                return Result.Success;
            }
            else
            {
                doc.Objects.UnselectAll();
                doc.Views.Redraw();
                return Result.Failure;
            }
        }

        private void HandleDependencyManagement(GleniusImplantDirector director)
        {
            //Dependency Managements
            var graph = director.Graph;
            graph.NotifyBuildingBlockHasChanged(IBB.SolidWallCurve, IBB.SolidWallWrap);
            graph.InvalidateGraph();
        }

    }
}
