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

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("15cc4d6e-c468-4c88-b199-19a6e38114ec")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommand(DesignPhase.Scaffold, IBB.ScaffoldGuides)]
    public class GleniusDeleteScaffoldGuide : CommandBase<GleniusImplantDirector>
    {
        static GleniusDeleteScaffoldGuide _instance;
        public GleniusDeleteScaffoldGuide()
        {
            _instance = this;
            VisualizationComponent = new ScaffoldGuideGenericVisualization();
        }

        ///<summary>The only instance of the GleniusDeleteScaffoldGuide command.</summary>
        public static GleniusDeleteScaffoldGuide Instance => _instance;

        public override string EnglishName => "GleniusDeleteScaffoldGuide";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            var objectManager = new GleniusObjectManager(director);

            var selectedCurveId = Guid.Empty;

            while (true)
            {
                Locking.UnlockScaffoldGuides(doc);

                var getObject = new GetObject();
                getObject.SetCommandPrompt("Select a Scaffold guide curve and press Enter to delete");
                getObject.EnablePreSelect(false, false);
                getObject.EnablePostSelect(true);
                getObject.AcceptNothing(true);
                getObject.EnableTransparentCommands(false);

                var res = getObject.Get();

                if (res == GetResult.Object)
                {
                    selectedCurveId = getObject.Object(0).ObjectId;
                }
                else if (res == GetResult.Nothing)
                {
                    break;
                }
                else
                {
                    return Result.Failure;
                }
            }

            objectManager.DeleteObject(selectedCurveId);

            HandleDependencyManagement(director);
            return Result.Success;
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, GleniusImplantDirector director)
        {
            doc.Views.Redraw();
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, GleniusImplantDirector director)
        {
            doc.Views.Redraw();
        }

        private void HandleDependencyManagement(GleniusImplantDirector director)
        {
            //Dependency Managements
            var graph = director.Graph;
            graph.NotifyBuildingBlockHasChanged(IBB.ScaffoldGuides);
            graph.InvalidateGraph();
        }
    }
}
