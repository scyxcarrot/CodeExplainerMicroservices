using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Linq;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("85152188-1C18-4186-8593-CC32B0933BBA")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommand(DesignPhase.Scaffold, IBB.ScrewMantle)]
    public class GleniusAdjustScrewMantleLength : CommandBase<GleniusImplantDirector>
    {
        public GleniusAdjustScrewMantleLength()
        {
            Instance = this;
            VisualizationComponent = new AdjustScrewMantleLengthVisualization();
        }

        public static GleniusAdjustScrewMantleLength Instance { get; private set; }
        
        public override string EnglishName => "GleniusAdjustScrewMantleLength";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            if (!director.IsCommandRunnable(this, true))
            {
                return Result.Failure;
            }

            Locking.UnlockScrewMantles(director.Document);

            var selectScrewMantle = new GetObject();
            selectScrewMantle.SetCommandPrompt("Select a screw mantle");
            selectScrewMantle.EnablePreSelect(false, false);
            selectScrewMantle.EnablePostSelect(true);
            selectScrewMantle.AcceptNothing(true);
            selectScrewMantle.EnableTransparentCommands(false);

            var result = Result.Cancel;
            var res = selectScrewMantle.Get();
            if (res == GetResult.Object)
            {
                var screwMantle = (ScrewMantle) selectScrewMantle.Object(0).Object();
                var oldScrewMantleId = screwMantle.Id;
                var associatedScrew = GetAssociatedScrew(oldScrewMantleId, director);

                var operation = new AdjustScrewMantleLength(director, screwMantle);
                result = operation.Adjust();

                if (result == Result.Success)
                {
                    var objectManager = new GleniusObjectManager(director);
                    var adjustedScrewMantleGuid = objectManager.AddNewBuildingBlock(IBB.ScrewMantle, operation.AdjustedScrewMantle, true);
                    objectManager.DeleteObject(screwMantle.Id);

                    associatedScrew.UpdateScrewMantleGuid(adjustedScrewMantleGuid);
                }
            }

            doc.Objects.UnselectAll();
            doc.Views.Redraw();
            Core.Operations.Locking.LockAll(director.Document);
            return result;
        }

        private Screw GetAssociatedScrew(Guid screwMantleId, GleniusImplantDirector director)
        {
            var objectManager = new GleniusObjectManager(director);
            var screws = objectManager.GetAllBuildingBlocks(IBB.Screw).Select(screw => screw as Screw);

            Screw associatedScrew = null;
            foreach (var screw in screws)
            {
                if (!IsAssociatedScrew(screw, screwMantleId))
                {
                    continue;
                }
                associatedScrew = screw;
                break;
            }

            return associatedScrew;
        }

        private bool IsAssociatedScrew(Screw screw, Guid screwMantleGuid)
        {
            return screw != null && screw.ScrewAides[ScrewAideType.Mantle] == screwMantleGuid;
        }
    }
}