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
    [System.Runtime.InteropServices.Guid("3c1bc56b-faf4-4ce0-9b33-8cc9f5d8676a")]
    [IDSCommandAttributes(true, DesignPhase.Scaffold, IBB.ScaffoldSupport, IBB.ReamedPelvis, IBB.SkirtBoneCurve, IBB.SkirtCupCurve, IBB.SkirtMesh, IBB.Cup)]
    public class MakeScaffoldVolume : CommandBase<ImplantDirector>
    {
        /// Make the singleton instance of this command
        public MakeScaffoldVolume()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
            _dependencies = new Dependencies();
        }

        /// Access the singleton instance of this command
        public static MakeScaffoldVolume TheCommand { get; private set; }

        /// The command name as it appears on the Rhino command line
        public override string EnglishName => "MakeScaffoldVolume";

        private readonly Dependencies _dependencies;

        /**
         * Execute command to make the mesh representing the non-porous
         * scaffold volume.
         */
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            // Create scaffold volume
            var success = ScaffoldMaker.CreateScaffold(director);
            return success ? Result.Success : Result.Failure;
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, ImplantDirector director)
        {
            _dependencies.DeleteBlockDependencies(director, IBB.ScaffoldVolume);

            // Set default skirt visibility
            Visibility.ScaffoldDefault(doc);
            doc.Views.Redraw();
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, ImplantDirector director)
        {
            // Set default skirt visibility
            Visibility.ScaffoldDefault(doc);
            doc.Views.Redraw();
        }
    }
}