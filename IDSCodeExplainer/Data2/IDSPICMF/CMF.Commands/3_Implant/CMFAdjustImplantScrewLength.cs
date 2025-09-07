using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.Core.V2.TreeDb.Model;
using IDS.PICMF.Operations;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using System;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("AB4871C7-3B46-4D50-A963-275A7C21E6B1")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Implant, IBB.ImplantSupport, IBB.Screw)]
    public class CMFAdjustImplantScrewLength : CMFImplantScrewBaseCommand
    {
        public CMFAdjustImplantScrewLength()
        {
            TheCommand = this;
            VisualizationComponent = new CMFManipulateImplantScrewVisualization();
            IsUseBaseCustomUndoRedo = false;
        }
        
        public static CMFAdjustImplantScrewLength TheCommand { get; private set; }
        
        public override string EnglishName => "CMFAdjustImplantScrewLength";
        
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            // Get selected screw
            var screw = SelectScrew(doc, "Select an implant screw to adjust it's length.");
            if (screw != null)
            {
                var availableLengths = ScrewUtilities.GetAvailableScrewLengths(screw, false);
                var registeredBarrelId = screw.ScrewGuideAidesInDocument.ContainsKey(IBB.RegisteredBarrel) ? screw.ScrewGuideAidesInDocument[IBB.RegisteredBarrel] : Guid.Empty;
                var operation = new AdjustImplantScrewLength(screw, availableLengths);
                var result = operation.AdjustLength();
                if (result == Result.Success)
                {
                    var screwId = screw.Id;
                    var newScrew = (Screw)director.Document.Objects.Find(screwId);

                    var screwUndoRedo = new ScrewUndoRedo
                    {
                        Undo = () => Undo(director.IdsDocument),
                        Redo = () => Redo(newScrew, registeredBarrelId, director.IdsDocument)
                    };
                    doc.AddCustomUndoEvent("OnUndoRedo", OnUndoRedo, screwUndoRedo);
                }

                AddImplantScrewTrackingParameter(ScrewUtilities.GetScrewNumberWithPhaseNumber(screw, false));

                doc.Objects.UnselectAll();
                doc.Views.Redraw();
                return result;
            }

            return Result.Failure;
        }

        private void Undo(IDSDocument document)
        {
            document.Undo();
        }

        private void Redo(Screw screw, Guid registeredBarrelId, IDSDocument document)
        {
            if (registeredBarrelId != Guid.Empty)
            {
                screw.ScrewGuideAidesInDocument[IBB.RegisteredBarrel] = registeredBarrelId;
            }
            document.Redo();
        }
    }
}