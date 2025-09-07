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
    /**
    * @class MakePlateBallparkTop
    * @brief Command to create mesh used for cutting out plate top surface.
    */

    [System.Runtime.InteropServices.Guid("db51c09b-30fd-4c67-bbe2-eb42c570ce49")]
    [IDSCommandAttributes(true, DesignPhase.Scaffold, IBB.Cup, IBB.WrapBottom)]
    public class MakeWraps : CommandBase<ImplantDirector>
    {
        public MakeWraps()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
            _dependencies = new Dependencies();
        }

        /// The one and only instance of this command
        public static MakeWraps TheCommand { get; private set; }

        /// The command name as it appears on the Rhino command line
        public override string EnglishName => "MakeWraps";

        private readonly Dependencies _dependencies;

        /*
         * Create the mesh used for cutting out the plate top surface
         */
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            WrapMaker.CreateAllWraps(director);
            return Result.Success;
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, ImplantDirector director)
        {
            // Delete dependencies
            _dependencies.DeleteBlockDependencies(director, IBB.WrapBottom);
            _dependencies.DeleteBlockDependencies(director, IBB.WrapTop);
            _dependencies.DeleteBlockDependencies(director, IBB.WrapSunkScrew);
            _dependencies.DeleteBlockDependencies(director, IBB.WrapScrewBump);

            // Set default skirt visibility
            Visibility.SkirtDefault(doc);
            doc.Views.Redraw();
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, ImplantDirector director)
        {
            Visibility.SkirtDefault(doc);
        }
    }
}