using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Operations;
using IDS.Amace.Relations;
using IDS.Amace.Visualization;
using IDS.Common;
using IDS.Core.CommandBase;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Amace.Commands
{
    /**
     * Rhino command to update screws after the cup or plate has been
     * modified.
     */

    [System.Runtime.InteropServices.Guid("ed249a7e-9211-432d-a09e-71525b167dae")]
    [IDSCommandAttributes(true, DesignPhase.Screws, IBB.Cup, IBB.WrapBottom, IBB.WrapTop)]
    public class UpdateScrews : CommandBase<ImplantDirector>
    {
        public UpdateScrews()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
            _dependencies = new Dependencies();
        }

        ///<summary>The one and only instance of this command</summary>
        public static UpdateScrews TheCommand { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string EnglishName => "UpdateScrews";

        private readonly Dependencies _dependencies;

        /**
         * Let user choose which screws to update and how to update them.
         */
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            // Let user select screws to update or not-update (default is all)
            Locking.UnlockScrews(director.Document);
            var gm = new GetObject();
            gm.SetCommandPrompt("Select screws to update or press ENTER to select all screws");
            gm.DisablePreSelect();
            gm.AcceptNothing(true);
            // Get the screws
            var selectedScrews = new List<Screw>();
            while (true)
            {
                var res = gm.Get();
                if (res == GetResult.Cancel)
                {
                    return Result.Success;
                }

                if (res == GetResult.Nothing)
                {
                    // Select all screws if none selected
                    if (selectedScrews.Count == 0)
                    {
                        ScrewManager screwManager = new ScrewManager(director.Document);
                        selectedScrews = screwManager.GetAllScrews().ToList();
                    }

                    break;
                }

                if (res == GetResult.Object)
                {
                    var selectedScrew = (Screw)gm.Object(0).Object();
                    selectedScrews.Add(selectedScrew);
                }
            }

            // recalibrate the screws
            foreach (var screw in selectedScrews)
            {
                var axialOffset = screw.AxialOffset;
                screw.CalibrateScrewHead();
                screw.AxialOffset = axialOffset;
                screw.Update();
            }

            // Delete screw dependencies
            _dependencies.DeleteBlockDependencies(director, IBB.Screw);

            // Done
            Core.Operations.Locking.LockAll(director.Document);
            Visibility.ScrewDefault(doc);
            return Result.Success;
        }
    }
}