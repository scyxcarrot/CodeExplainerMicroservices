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
     * Rhino Command to make the initial solid plate by cutting out the
     * top and bottom surface from the ballpark meshes and stitching them together.
     */

    [System.Runtime.InteropServices.Guid("d2733e1c-ca71-40e7-ba67-c07a4487df06")]
    [IDSCommandAttributes(true, DesignPhase.Plate, IBB.Cup, IBB.WrapBottom, IBB.WrapTop, IBB.PlateContourBottom, IBB.PlateContourTop)]
    public class MakePlate : CommandBase<ImplantDirector>
    {
        public MakePlate()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
        }

        ///<summary>The one and only instance of this command</summary>
        public static MakePlate TheCommand { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string EnglishName => "MakePlate";

        /**
         * Rhino command to cut out the top and bottom plate surfaces and
         * stitch them together to create the initial solid plate.
         */
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            // Make the QCPlate
            var success = PlateMaker.CreateImplantForQualityControl(director);


            if (!success)
            {
                Visibility.PlateDefault(doc);
                return Result.Failure;
            }

            var dependencies = new Dependencies();
            success = dependencies.UpdateTransitionPreview(director, false);

            Visibility.PlateDefault(doc);
            doc.Views.Redraw();

            return success ? Result.Success : Result.Failure;
        }
    }
}