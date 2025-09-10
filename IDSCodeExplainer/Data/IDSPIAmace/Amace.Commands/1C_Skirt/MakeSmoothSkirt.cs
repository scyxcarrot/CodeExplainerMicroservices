using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Operations;
using IDS.Amace.Relations;
using IDS.Common;
using IDS.Core.CommandBase;
using Rhino;
using Rhino.Commands;

namespace IDS.Amace.Commands
{
    [System.Runtime.InteropServices.Guid("7BE72869-76AD-47A8-9A3C-2FE18666F07F"), CommandStyle(Style.ScriptRunner)]
    [IDSCommandAttributes(true, DesignPhase.Skirt, IBB.Cup, IBB.SkirtBoneCurve, IBB.SkirtCupCurve)]
    public class MakeSmoothTransitionSurface : CommandBase<ImplantDirector>
    {
        public MakeSmoothTransitionSurface()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
            _dependencies = new Dependencies();
        }

        ///<summary>The one and only instance of this command</summary>
        public static MakeSmoothTransitionSurface TheCommand { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string EnglishName => "MakeSmoothSkirt";

        private readonly Dependencies _dependencies;

        /**
         * Make the transition surface. The transition surface is a spline
         * surface connecting the lif-off curve on the cup medial surface
         * and the touchdown curve on the defect border.
         */
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            // Create the skirt
            var success = SkirtMaker.CreateSweepSkirt(doc);
            return success ? Result.Success : Result.Failure;
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, ImplantDirector director)
        {
            // Delete skirt dependencies
            _dependencies.DeleteBlockDependencies(director, IBB.SkirtMesh);

            // Set visibility
            doc.Views.Redraw();
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, ImplantDirector director)
        {
            // Set visibility
            doc.Views.Redraw();
        }
    }
}