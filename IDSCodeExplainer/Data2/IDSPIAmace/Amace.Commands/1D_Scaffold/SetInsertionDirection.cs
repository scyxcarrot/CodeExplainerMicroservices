using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Operations;
using IDS.Amace.Relations;
using IDS.Amace.Visualization;
using IDS.Common;
using IDS.Core.CommandBase;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;

namespace IDS.Amace.Commands
{
    /**
     * Command to interactivrely set the insertion direction.
     */

    [System.Runtime.InteropServices.Guid("B23E6DFA-3EDC-4850-8159-5FA2901A3362")]
    [IDSCommandAttributes(true, DesignPhase.Scaffold, IBB.Cup)]
    public class SetInsertionDirection : CommandBase<ImplantDirector>
    {
        public SetInsertionDirection()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
            _dependencies = new Dependencies();
        }

        ///<summary>The one and only instance of this command</summary>
        public static SetInsertionDirection TheCommand { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string EnglishName => "SetInsertionDirection";

        private readonly Dependencies _dependencies;

        /**
         * Load the MBV volume from an existing mesh in the document
         * instead of creating it.
         */
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {

            // Turn Ortho off (if necessary)
            var currentOrthoStatus = Rhino.ApplicationSettings.ModelAidSettings.Ortho;
            Rhino.ApplicationSettings.ModelAidSettings.Ortho = false; // turn it off

            // Set the view so that user looks into cup
            View.SetCupInsertionView(doc);

            // Let user indicate an insertion direction
            var gp = new GetInsertDir(director);
            gp.SetCommandPrompt("Position camera along insertion direction, ");
            var insertionDirection = gp.GetDirection();

            // Prepare data for the next command
            if (insertionDirection != Vector3d.Unset)
            {
                director.InsertionDirection = insertionDirection;
            }

            // Reached end
            Rhino.ApplicationSettings.ModelAidSettings.Ortho = currentOrthoStatus; // set original ortho state

            // Delete dependencies
            _dependencies.DeleteBlockDependencies(director, IBB.ScaffoldSupport);

            // Regenerate scaffold if possible
            var success = ScaffoldMaker.CreateScaffold(director);
            Visibility.ScaffoldDefault(doc);

            return success ? Result.Success : Result.Failure;
        }
    }
}