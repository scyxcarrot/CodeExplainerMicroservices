using IDS.Amace;
using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Operations;
using IDS.Amace.Relations;
using IDS.Amace.Visualization;
using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Enumerators;
using IDS.Operations.Screws;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Linq;

namespace IDS.Commands.Screws
{
    /**
     * Rhino command to make a cup screw.
     */

    [System.Runtime.InteropServices.Guid("21D68CD1-F48C-4B25-8E38-7A63BD7A7BF9")]
    [IDSCommandAttributes(true, DesignPhase.Screws, IBB.Cup, IBB.WrapBottom, IBB.WrapTop, IBB.WrapSunkScrew)]
    public class PositionScrew : CommandBase<ImplantDirector>
    {
        /**
         * Initialize singleton instance representing this command.
         */

        public PositionScrew()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
        }

        /** The one and only instance of this command */

        public static PositionScrew TheCommand { get; private set; }

        /** The command name as it appears on the Rhino command line */

        public override string EnglishName => "PositionScrew";

        /**
         * Run the command to make a screw.
         */
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            // Ask screw options
            var go = new GetOption();
            go.SetCommandPrompt("Choose screw parameters: Region determines constraints, Countersunk determines laterally or medially augmented.");
            go.AcceptNothing(true);
            // Countersunk or not
            var optSunk = new OptionToggle(true, "False", "True");
            go.AddOptionToggle("Sunk", ref optSunk);
            // Screw type/shape
            var currentBrand = director.CurrentScrewBrand;
            var screwQuery = new ScrewQuery();
            var selScrewBrandType = screwQuery.GetDefaultScrewType(currentBrand);
            var availableScrewBrandTypes = screwQuery.GetAvailableScrewTypes(selScrewBrandType);
            var availableScrewTypes = availableScrewBrandTypes.Select(brand => brand.ToString()).ToList();
            var selScrewType = availableScrewTypes.IndexOf(selScrewBrandType.ToString());
            var optScrewTypeId = go.AddOptionList("ScrewType", availableScrewTypes, selScrewType);
            // Placement method
            var selPlacement = PlacementMethod.HeadTip;
            var optPlacementId = go.AddOptionEnumList<PlacementMethod>("Placement", selPlacement);

            // Get user input
            while (true)
            {
                var res = go.Get();
                if (res == GetResult.Cancel)
                {
                    return Result.Failure;
                }
                if (res == GetResult.Nothing)
                {
                    break;
                }
                if (res == GetResult.Option)
                {
                    // Process option selection
                    var optId = go.OptionIndex();
                    if (optId == optScrewTypeId)
                    {
                        selScrewBrandType = availableScrewBrandTypes[go.Option().CurrentListOptionIndex];
                    }
                    else if (optId == optPlacementId)
                    {
                        selPlacement = go.GetSelectedEnumValue<PlacementMethod>();
                    }
                }
            }
            var rc = go.CommandResult();
            if (rc != Result.Success)
            {
                return Result.Failure;
            }

            //
            var screwAlignment = ScrewAlignment.Floating;
            if (optSunk.CurrentValue)
            {
                screwAlignment = ScrewAlignment.Sunk;
            }

            var objManager = new AmaceObjectManager(director);

            // Get required data
            var wrapScrewPositioning = (Mesh)objManager.GetBuildingBlock(IBB.WrapSunkScrew).Geometry;
            var reamedPelvis = (Mesh)objManager.GetBuildingBlock(IBB.ReamedPelvis).Geometry;

            // Make interactive screw getter object based on input
            GetScrew positionScrew;
            Screw oldScrew;
            var oldScrewId = Guid.Empty;
            if (selPlacement == PlacementMethod.HeadTip || selPlacement == PlacementMethod.Camera) // New screw
            {
                oldScrew = new Screw(director, selScrewBrandType, screwAlignment);
                positionScrew = new GetScrew(director, selPlacement, wrapScrewPositioning, reamedPelvis, oldScrew);
            }
            else // Change existing
            {
                // Unlock screws
                Locking.UnlockScrews(doc);
                // Get screw
                var selectScrew = new GetObject();
                selectScrew.SetCommandPrompt("Select a screw to move it.");
                selectScrew.EnablePreSelect(true, true);
                selectScrew.AcceptNothing(true);
                // Get user input
                while (true)
                {
                    var res = selectScrew.Get();
                    if (res == GetResult.Cancel)
                    {
                        return Result.Failure;
                    }

                    if (res == GetResult.Object)
                    {
                        // Also called when object was preselected
                        oldScrew = (Screw)selectScrew.Object(0).Object();
                        oldScrewId = oldScrew.Id;
                        selectScrew.DisablePreSelect(); // prevent being called again with pre-selected screw
                        break;
                    }

                    if (res == GetResult.Nothing) // Pressed enter
                    {
                        RhinoApp.WriteLine("Please select a screw before continuing");
                    }
                }
                // Configure the screw positioning
                positionScrew = new GetScrew(director, selPlacement, wrapScrewPositioning, reamedPelvis, oldScrew);
            }

            // Let user indicate screw using getter
            var screw = positionScrew.Get();
            if (null == screw)
            {
                IDSPIAmacePlugIn.WriteLine(LogCategory.Warning, "Could not position screw. Please verify that the current position allows screws placement.");
                return Result.Failure;
            }

            // If we only adjust the length, the screw does not need a recalibration
            var doRecalibrate = selPlacement != PlacementMethod.AdjustLength;

            // Replace the old screw by the updated screw
            screw.Set(oldScrewId, doRecalibrate);

            // Success
            return Result.Success;
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, ImplantDirector director)
        {
            var dep = new Dependencies();
            dep.DeleteBlockDependencies(director, IBB.Screw);
            Visibility.ScrewDefault(doc);
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, ImplantDirector director)
        {
            Visibility.ScrewDefault(doc);
        }
    }
}