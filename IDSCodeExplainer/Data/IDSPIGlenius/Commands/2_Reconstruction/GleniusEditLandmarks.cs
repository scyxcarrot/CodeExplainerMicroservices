using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using IDS.Glenius.Relations;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input.Custom;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("A9383DCD-519C-4F89-8DE1-A09C85BA1E68")]
    [IDSGleniusCommand(DesignPhase.Reconstruction, IBB.ReconstructedScapulaBone)]
    public class GleniusEditLandmarks : CommandBase<GleniusImplantDirector>
    {
        public GleniusEditLandmarks()
        {
            TheCommand = this;
            VisualizationComponent = new EditLandmarkVisualization();
        }

        public static GleniusEditLandmarks TheCommand { get; private set; }

        public override string EnglishName => "GleniusEditLandmarks";

        private AnatomyMeasurementsChanger _changer;

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            if (!director.IsCommandRunnable(this, true) || director.AnatomyMeasurements == null)
            {
                return Result.Failure;
            }

            bool isEditTrigonum;
            var response = GetOption(out isEditTrigonum);
            if (response != Result.Success)
            {
                return response;
            }

            var objectManager = new GleniusObjectManager(director);
            var scapulaMesh = objectManager.GetBuildingBlock(IBB.Scapula);
            var anatomyMeasurements = director.AnatomyMeasurements;
            var operation = new EditLandmark(isEditTrigonum ? anatomyMeasurements.Trig : anatomyMeasurements.AngleInf, scapulaMesh.Geometry as Mesh);
            var result = operation.Edit();
            if (result == Result.Success)
            {
                var newPoint = operation.Point;
                if (newPoint != Point3d.Unset)
                {
                    var angleInf = !isEditTrigonum ? newPoint : anatomyMeasurements.AngleInf;
                    var trig = isEditTrigonum ? newPoint : anatomyMeasurements.Trig;
                    var measurements = new AnatomicalMeasurements(angleInf, trig, anatomyMeasurements.PlGlenoid.Origin, anatomyMeasurements.PlGlenoid.Normal, director.defectIsLeft);

                    if (_changer != null)
                    {
                        _changer.UndoneRedone -= UndoneRedone;
                    }

                    _changer = new AnatomyMeasurementsChanger(director, "Edit Landmarks");
                    _changer.UndoneRedone += UndoneRedone;
                    _changer.SubscribeUndoRedoEvent(doc);

                    director.AnatomyMeasurements = measurements;
                }
            }

            doc.Views.Redraw();
            return result;
        }

        private static void UndoneRedone(object sender, CustomUndoEventArgs e)
        {
            ReconstructionMeasurementVisualizer.Get().Reset();
            e.Document.Views.Redraw();
        }

        public Result GetOption(out bool isEditTrigonum)
        {
            isEditTrigonum = true;

            var getOption = new GetOption();
            getOption.SetCommandPrompt("Choose to edit Trigonum or Angulus Inferior landmark");
            var editTrigonum = getOption.AddOption("Trigonum");
            getOption.AddOption("AngInf");
            getOption.EnableTransparentCommands(false);
            getOption.Get();

            if (getOption.CommandResult() != Result.Success)
            {
                return getOption.CommandResult();
            }

            var option = getOption.Option();
            if (option == null)
            {
                return Result.Failure;
            }

            var optionSelected = option.Index;
            isEditTrigonum = optionSelected == editTrigonum;
            return Result.Success;
        }
    }
}