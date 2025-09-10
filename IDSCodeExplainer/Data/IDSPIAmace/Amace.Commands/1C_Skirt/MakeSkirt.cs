using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Operations;
using IDS.Amace.Relations;
using IDS.Amace.Visualization;
using IDS.Common;
using IDS.Core.CommandBase;
using Rhino;
using Rhino.Commands;

namespace IDS.Amace.Commands
{
    [System.Runtime.InteropServices.Guid("899e2043-21a9-49a2-b3b3-153661027527")]
    [IDSCommandAttributes(true, DesignPhase.Skirt, IBB.SkirtBoneCurve, IBB.SkirtCupCurve)]
    public class MakeTransitionSurface : CommandBase<ImplantDirector>
    {
        public MakeTransitionSurface()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
            _dependencies = new Dependencies();
        }

        ///<summary>The one and only instance of this command</summary>
        public static MakeTransitionSurface TheCommand { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string EnglishName => "MakeSkirt";

        private readonly Dependencies _dependencies;

        /**
         * Make the transition surface. The transition surface is a spline
         * surface connecting the lif-off curve on the cup medial surface
         * and the touchdown curve on the defect border.
         */

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            // Create skirt
            var success = SkirtMaker.CreateSkirt(director);
            return success ? Result.Success : Result.Failure;
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, ImplantDirector director)
        {
            // Delete skirt dependencies
            _dependencies.DeleteBlockDependencies(director, IBB.SkirtMesh);

            // Set visibility
            Visibility.SkirtDefault(doc);
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, ImplantDirector director)
        {
            // Set visibility
            doc.Views.Redraw();
            Visibility.SkirtDefault(doc);
        }
    }
}