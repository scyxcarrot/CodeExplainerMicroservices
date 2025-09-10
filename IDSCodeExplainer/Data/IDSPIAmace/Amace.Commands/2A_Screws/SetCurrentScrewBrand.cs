using IDS.Amace;
using IDS.Amace.Enumerators;
using IDS.Amace.GUI;
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
using System;
using System.Linq;

namespace IDS.Commands.Screws
{
    [System.Runtime.InteropServices.Guid("41E73261-14DB-440A-AACC-B16C9D0F2133")]
    [IDSCommandAttributes(true, DesignPhase.Screws)]
    public class SetCurrentScrewBrand : CommandBase<ImplantDirector>
    {
        public SetCurrentScrewBrand()
        {
            TheCommand = this;
        }

        public static SetCurrentScrewBrand TheCommand { get; private set; }

        public override string EnglishName => "SetCurrentScrewBrand";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            // Check input data
            var go = new GetOption();
            go.SetCommandPrompt("Choose screw brand.");
            go.AcceptNothing(true);

            var currentScrewBrand = director.CurrentScrewBrand;
            var screwDatabaseQuery = new ScrewDatabaseQuery();
            var availableScrewBrands = screwDatabaseQuery.GetAvailableScrewBrands().ToList();
            var currentScrewBrandIndex = availableScrewBrands.IndexOf(currentScrewBrand);
            go.AddOptionList("Brand", availableScrewBrands, currentScrewBrandIndex);

            var res = go.Get();
            if (res == GetResult.Option)
            {
                var selectedBrand = availableScrewBrands[go.Option().CurrentListOptionIndex];
                if (selectedBrand != currentScrewBrand)
                {
                    director.CurrentScrewBrand = selectedBrand;

                    // Unlock to change
                    Locking.UnlockScrews(doc);

                    var screwManager = new ScrewManager(doc);
                    var screws = screwManager.GetAllScrews();
                    foreach (var screw in screws)
                    {
                        var newScrew = new Screw(screw, false, true); // copy original to new
                        screw.Delete(); // delete original from document

                        var screwQuery = new ScrewQuery();
                        var selectedScrewBrandType = screwQuery.GetDefaultScrewType(selectedBrand);
                        newScrew.screwBrandType = selectedScrewBrandType;

                        // Set in document
                        newScrew.Set(Guid.Empty); // add new to document
                    }

                    // Delete dependencies
                    var dependencies = new Dependencies();
                    dependencies.DeleteBlockDependencies(director, IBB.Screw);

                    // Lock again
                    Core.Operations.Locking.LockAll(doc);
                    Visibility.ScrewDefault(doc);

                    // Update screw checks
                    ScrewInfo.Update(doc, false);

                    // Refresh panel
                    ScrewPanel.GetPanel()?.RefreshPanelInfo();

                    return Result.Success;
                }
            }

            return Result.Cancel;
        }
    }
}