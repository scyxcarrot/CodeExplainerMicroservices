using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Glenius.Enumerators;
using IDS.Glenius.FileSystem;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System.Linq;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("ed796883-2683-4e5f-8240-5d78430fdd6f"),
     CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommand(DesignPhase.Reconstruction, IBB.ScapulaDefectRegionRemoved)]
    public class GleniusExecuteReconstruction : CommandBase<GleniusImplantDirector>
    {
        public GleniusExecuteReconstruction()
        {
            Instance = this;
            VisualizationComponent = new ExecuteReconstructionVisualization();
        }

        ///<summary>The only instance of the GleniusExecuteReconstruction command.</summary>
        public static GleniusExecuteReconstruction Instance { get; private set; }

        public override string EnglishName => "GleniusExecuteReconstruction";


        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            doc.AddCustomUndoEvent("OnUndoRedo", OnUndoRedo);

            return DoOperation(doc, director) ? Result.Success : Result.Failure;
        }

        //////////////////////////////////////////////////////////////////////////

        private string GenerateReconstructionParametersCSVPath(RhinoDoc doc, string caseId, int version, int draft)
        {
            var workingDir = DirectoryStructure.GetWorkingDir(doc);
            return workingDir + "\\" + caseId + "_Reconstruction_Parameters_v" + version + "_draft" + draft + ".csv";
        }

        private bool DoOperation(RhinoDoc doc, GleniusImplantDirector director)
        {
            //Get scapula mesh
            var objManager = new GleniusObjectManager(director);
            var scapula = objManager.GetBuildingBlock(IBB.ScapulaDefectRegionRemoved).Geometry as Mesh;

            //Remove original reconstructed scapula if present [Dependency]
            var ids = objManager.GetAllBuildingBlockIds(IBB.ReconstructedScapulaBone).ToList();

            foreach (var id in ids)
            {
                objManager.DeleteObject(id);
            }

            //Reset anatomy measurements 
            director.DefaultAnatomyMeasurements = null;
            director.AnatomyMeasurements = null;

            //Prepare parameters for interop
            var workingDir = DirectoryStructure.GetWorkingDir(doc);
            var reconsStlResultPath = workingDir + "\\" + director.caseId + "_" +
                                      (director.defectIsLeft ? "SL" : "SR") + "_ScapulaReconstructed.stl";

            //Export healthy scapula model
            var exportHealthyScapulaPath = workingDir + "\\" + director.caseId + "_" +
                                           (director.defectIsLeft ? "SL" : "SR") + "_ScapulaDefectRegionRemoved.stl";

            StlUtilities.RhinoMesh2StlBinary(scapula, exportHealthyScapulaPath);

            //Execute Interop Reconstruction
            var reconsParamsCsvPath = GenerateReconstructionParametersCSVPath(doc, director.caseId, director.version, director.draft);

            if (!ExternalToolsInterop.GleniusReconstruction(exportHealthyScapulaPath, reconsStlResultPath,
                reconsParamsCsvPath, director.defectIsLeft, 2, 20))
            {
                return false;
            }

            // Read results
            Mesh reconstructedScapula;
            if (!StlUtilities.StlBinary2RhinoMesh(reconsStlResultPath, out reconstructedScapula))
            {
                return false;
            }

            var anatomicalMeasurementsLoader = new AnatomicalMeasurementsLoader(reconsParamsCsvPath, director.defectIsLeft);
            var measurements = anatomicalMeasurementsLoader.GetAnatomicalMeasurements();

            if (measurements == null)
            {
                return false;
            }

            director.DefaultAnatomyMeasurements = new AnatomicalMeasurements(measurements);
            director.AnatomyMeasurements = new AnatomicalMeasurements(measurements);

            //Add Objects into document
            objManager.AddNewBuildingBlock(IBB.ReconstructedScapulaBone, reconstructedScapula);

            doc.Views.Redraw();

            return true;
        }

        //////////////////////////////////////////////////////////////////////////
        void OnUndoRedo(object sender, Rhino.Commands.CustomUndoEventArgs e)
        {
            var director = IDSPluginHelper.GetDirector<GleniusImplantDirector>(e.Document.DocumentId);

            if (e.CreatedByRedo)
            {
                DoOperation(e.Document, director);
            }
            else //Undo
            {
                director.DefaultAnatomyMeasurements = null;
                director.AnatomyMeasurements = null;
                ReconstructionMeasurementVisualizer.Get().Reset();
                e.Document.Views.Redraw();
            }

            e.Document.AddCustomUndoEvent("OnUndoRedo", OnUndoRedo);
        }
    }
}
