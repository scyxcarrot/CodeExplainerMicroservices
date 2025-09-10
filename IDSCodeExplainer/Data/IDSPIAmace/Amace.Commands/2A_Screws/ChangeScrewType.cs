using IDS.Amace.Enumerators;
using IDS.Amace.GUI;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Operations;
using IDS.Amace.Relations;
using IDS.Amace.Visualization;
using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Enumerators;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Linq;

namespace IDS.Commands.Screws
{
    /**
     * Rhino command to make a cup screw.
     */

    [System.Runtime.InteropServices.Guid("05C92B7A-4E3D-4CC4-BEB6-34EC4B34E7EC")]
    [IDSCommandAttributes(true, DesignPhase.Screws, IBB.Cup, IBB.WrapBottom, IBB.WrapTop, IBB.WrapSunkScrew)]
    public class ChangeScrewType : CommandBase<ImplantDirector>
    {
        /**
         * Initialize singleton instance representing this command.
         */

        public ChangeScrewType()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
        }

        /** The one and only instance of this command */

        public static ChangeScrewType TheCommand { get; private set; }

        /** The command name as it appears on the Rhino command line */

        public override string EnglishName => "ChangeScrewType";

        /**
         * Run the command to make a screw.
         */
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            // Unlock screws
            Locking.UnlockScrews(doc);
            // Get screw
            var selectScrew = new GetObject();
            selectScrew.SetCommandPrompt("Select a screw to edit it.");
            selectScrew.EnablePreSelect(true, true);
            selectScrew.AcceptNothing(true);
            // Get user input
            Screw screw;
            while (true)
            {
                GetResult res = selectScrew.Get();
                if (res == GetResult.Cancel)
                {
                    Core.Operations.Locking.LockAll(doc);
                    return Result.Failure;
                }

                if (res == GetResult.Object)
                {
                    // Also called when object was preselected
                    var rhobj = doc.Objects.Find(selectScrew.Object(0).ObjectId);
                    screw = (Screw)rhobj;

                    selectScrew.DisablePreSelect(); // prevent being called again with pre-selected screw
                    break;
                }

                if (res == GetResult.Nothing) // Pressed enter
                {
                    RhinoApp.WriteLine("Please select a screw before continuing");
                }
            }
            Core.Operations.Locking.LockAll(doc);

            // Ask screw options
            var go = new GetOption();
            go.SetCommandPrompt("Choose screw parameters.");
            go.AcceptNothing(true);
            // Screw type/shape
            var screwBrandType = screw.screwBrandType;
            var screwQuery = new ScrewQuery();
            var availableScrewBrandTypes = screwQuery.GetAvailableScrewTypes(screwBrandType);
            var availableScrewTypes = availableScrewBrandTypes.Select(brand => brand.ToString()).ToList();
            var selScrewType = availableScrewTypes.IndexOf(screwBrandType.ToString());
            var optScrewTypeId = go.AddOptionList("Type", availableScrewTypes, selScrewType);
            // Screw alignment
            var selScrewAlignment = screw.screwAlignment;
            var optScrewAlignmentId = go.AddOptionEnumList<ScrewAlignment>("Alignment", selScrewAlignment);
            // Axial offset
            var optAxialOffset = new OptionDouble(screw.AxialOffset);
            var optAxialOffsetId = go.AddOptionDouble("AxialOffset", ref optAxialOffset);
            // Get user input
            var recalibrate = false;
            while (true)
            {
                var res = go.Get();
                if (res == GetResult.Cancel)
                {
                    break;
                }
                if (res == GetResult.Nothing)
                {
                    break;
                }
                if (res == GetResult.Option)
                {
                    // Unlock to change
                    Locking.UnlockScrews(doc);

                    // Process option selection
                    var optId = go.OptionIndex();
                    var newScrew = new Screw(screw, false, true); // copy original to new
                    screw.Delete(); // delete original from document
                    // Screw type
                    if (optId == optScrewTypeId)
                    {
                        newScrew.screwBrandType = availableScrewBrandTypes[go.Option().CurrentListOptionIndex]; // set type
                        recalibrate = true;
                    }
                    // Screw alignment
                    else if (optId == optScrewAlignmentId)
                    {
                        newScrew.screwAlignment = go.GetSelectedEnumValue<ScrewAlignment>();
                        if (newScrew.screwAlignment == ScrewAlignment.Floating)
                        {
                            newScrew.AxialOffset = 0.0;
                            optAxialOffset.CurrentValue = 0.0; // does not work
                        }
                        recalibrate = true;
                    }
                    // Axial offset
                    else if (optId == optAxialOffsetId)
                    {
                        if (newScrew.screwAlignment == ScrewAlignment.Floating)
                        {
                            optAxialOffset.CurrentValue = 0.0; // does not work
                            IDSPIAmacePlugIn.WriteLine(LogCategory.Warning, "The axial offset of a floating screw cannot be changed. Use a sunk screw if an offset is required.");
                        }
                        else
                        {
                            newScrew.AxialOffset = optAxialOffset.CurrentValue;
                            recalibrate = false;
                        }
                    }

                    // Delete dependencies
                    var dep = new Dependencies();
                    dep.DeleteBlockDependencies(director, IBB.Screw);

                    // Set in document
                    newScrew.Set(Guid.Empty, recalibrate); // add new to document
                    screw = newScrew; // set new as original for next iteration

                    // Lock again
                    Core.Operations.Locking.LockAll(doc);
                    Visibility.ScrewDefault(doc);

                    // Update screw checks
                    ScrewInfo.Update(doc, false);

                    // Refresh panel
                    var screwPanel = ScrewPanel.GetPanel();
                    screwPanel?.RefreshPanelInfo();
                }
            }

            return Result.Success;
        }
    }
}