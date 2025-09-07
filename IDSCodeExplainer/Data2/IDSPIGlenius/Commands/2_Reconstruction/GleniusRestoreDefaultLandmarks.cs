using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.PluginHelper;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Relations;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("5D5475D7-D806-4A4C-BDB6-2788D12DF99A")]
    [IDSGleniusCommand(DesignPhase.Reconstruction, IBB.ReconstructedScapulaBone)]
    public class GleniusRestoreDefaultLandmarks : CommandBase<GleniusImplantDirector>
    {
        public GleniusRestoreDefaultLandmarks()
        {
            TheCommand = this;
            VisualizationComponent = new RestoreDefaultLandmarksVisualization();
        }

        public static GleniusRestoreDefaultLandmarks TheCommand { get; private set; }

        public override string EnglishName => "GleniusRestoreDefaultLandmarks";

        private AnatomyMeasurementsChanger changer;

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {

            if (!director.IsCommandRunnable(this, true) || director.DefaultAnatomyMeasurements == null || director.AnatomyMeasurements == null)
            {
                return Result.Failure;
            }

            if (changer != null)
            {
                changer.UndoneRedone -= UndoneRedone;
            }

            changer = new AnatomyMeasurementsChanger(director, "Restore Default Landmark");
            changer.UndoneRedone += UndoneRedone;
            changer.SubscribeUndoRedoEvent(doc);

            director.AnatomyMeasurements = new AnatomicalMeasurements(director.DefaultAnatomyMeasurements);
            return Result.Success;
        }

        private void UndoneRedone(object sender, CustomUndoEventArgs e)
        {
            var director = IDSPluginHelper.GetDirector<GleniusImplantDirector>(e.Document.DocumentId);
            ReconstructionMeasurementVisualizer.Get().Initialize(director);
            e.Document.Views.Redraw();
        }

    }
}