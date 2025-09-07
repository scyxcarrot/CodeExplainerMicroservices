using IDS.Amace.Enumerators;
using IDS.Amace.FileSystem;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Operations;
using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Fea;
using IDS.Core.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.UI;
using System.IO;
using System.Windows.Forms;

namespace IDS.Amace.Commands
{
    [System.Runtime.InteropServices.Guid("34a97c31-8282-4464-a475-605a232d856f")]
    [IDSCommandAttributes(true, DesignPhase.ImplantQC | DesignPhase.Export, IBB.PlateHoles, IBB.SolidPlateBottom, IBB.OriginalReamedPelvis)]
    public class PerformFea : CommandBase<ImplantDirector>
    {
        static PerformFea _instance;

        public PerformFea()
        {
            _instance = this;

            Proxies.PerformFea.FeaConduit = null;
        }

        ///<summary>The only instance of the PerformFea command.</summary>
        public static PerformFea Instance => _instance;

        public override string EnglishName => "PerformFea";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            var commandResult = Result.Success;

            // Ask the user if the FEA should be deleted.
            var go = new GetOption();
            go.SetCommandPrompt("VBT Options");
            go.AcceptNothing(true);
            // Delete temporary data option
            var optDeleteVbtFolder = new OptionToggle(true, "False", "True");
            go.AddOptionToggle("DeleteVbtFolder", ref optDeleteVbtFolder);
            var optRecalculate = new OptionToggle(false, "False", "True");
            go.AddOptionToggle("AlwaysRecalculate", ref optRecalculate);
            // Material properties
            var optMaterialEmodulus = new OptionDouble(Materials.Titanium.ElasticityEModulus);
            go.AddOptionDouble("MaterialEmodulus", ref optMaterialEmodulus);
            var optMaterialPoisson = new OptionDouble(Materials.Titanium.ElasticityPoissonRatio);
            go.AddOptionDouble("MaterialPoissonRatio", ref optMaterialPoisson);
            // Remeshing target edge length
            var optTargetEdgeLength = new OptionDouble(0.7);
            go.AddOptionDouble("TargetEdgeLength", ref optTargetEdgeLength);
            // Remeshing target edge length
            var optLoadMagnitude = new OptionDouble(5400);
            go.AddOptionDouble("LoadMagnitude", ref optLoadMagnitude);
            // Load Mesh Selection
            var optLoadMeshAngleDegrees = new OptionDouble(32);
            go.AddOptionDouble("LoadMeshDegreesThreshold", ref optLoadMeshAngleDegrees);
            // Boundary Condition Selection
            var optBoundaryConditionDistanceThreshold = new OptionDouble(0.5);
            go.AddOptionDouble("BoundaryConditionDistance", ref optBoundaryConditionDistanceThreshold);
            var optBoundaryConditionNoiseShellThreshold = new OptionDouble(25);
            go.AddOptionDouble("BoundaryConditionNoiseThreshold", ref optBoundaryConditionNoiseShellThreshold);

            // Get user input
            while (true)
            {
                GetResult res = go.Get();
                if (res == GetResult.Cancel)
                {
                    commandResult = Result.Failure;
                    break;
                }

                if (res == GetResult.Nothing)
                {
                    break;
                }
            }

            if (commandResult == Result.Failure)
            {
                return commandResult;
            }

            // If in script mode or manual override, always redo the FEA, otherwise ask the user if no FEA is avaiable
            var result = DialogResult.None;
            if (IDSPIAmacePlugIn.ScriptMode || optRecalculate.CurrentValue)
            {
                result = DialogResult.Yes;
            }
            else if (director.AmaceFea == null)
            {
                // Ask the user to confirm this action, since it takes some time
                result = Dialogs.ShowMessageBox("Running a Virtual Bench Test takes a few minutes. Do you want to continue?",
                                                                "Start Virtual Bench Test",
                                                                MessageBoxButtons.YesNo,
                                                                MessageBoxIcon.Question);
            }

            // Only start FEA if dialog result is yes
            if (result == DialogResult.Yes)
            {
                // Create the material
                var material = new Material(Materials.Titanium.Name, optMaterialEmodulus.CurrentValue,
                    optMaterialPoisson.CurrentValue, Materials.Titanium.UltimateTensileStrength, Materials.Titanium.FatigueLimit);

                // Recalculate the complete FEA
                var succesfulSimulation = RecalculateAmaceFea(director, material, optTargetEdgeLength.CurrentValue,
                    optLoadMagnitude.CurrentValue, optLoadMeshAngleDegrees.CurrentValue, 
                    optBoundaryConditionDistanceThreshold.CurrentValue, optBoundaryConditionNoiseShellThreshold.CurrentValue);
                commandResult = succesfulSimulation ? Result.Success : Result.Failure;
                // Remove FEA directory
                if (optDeleteVbtFolder.CurrentValue)
                {
                    Directory.Delete(DirectoryStructure.GetFeaFolder(director.Document), true);
                }
            }

            // Setup visualisation
            if (director.AmaceFea != null && Proxies.PerformFea.FeaConduit == null)
            {
                // Create fea conduit to visualise results
                Proxies.PerformFea.FeaConduit = new FeaConduit(director.AmaceFea);
                Proxies.PerformFea.FeaConduit.SetVisualisationParameters(TuneFeaVisualisation.SafetyFactorLow,
                    TuneFeaVisualisation.SafetyFactorMiddle, TuneFeaVisualisation.SafetyFactorHigh,
                    director.AmaceFea.material.FatigueLimit, director.AmaceFea.material.UltimateTensileStrength);
            }

            // Disable / enable. Depending on the current state
            if (Proxies.PerformFea.FeaConduit != null)
            {
                ToggleFeaVisibility(doc);
            }
            


            return commandResult;
        }

        private static void ToggleFeaVisibility(RhinoDoc doc)
        {
            if (Proxies.PerformFea.FeaConduit.Enabled)
            {
                // If boundary conditions are shown, hide them
                if (Proxies.PerformFea.FeaConduit.drawBoundaryConditions && Proxies.PerformFea.FeaConduit.drawLoadMesh)
                {
                    Proxies.PerformFea.FeaConduit.drawBoundaryConditions = false;
                    Proxies.PerformFea.FeaConduit.drawLoadMesh = false;
                    doc.Views.Redraw();
                }
                // If they are not shown, hide the conduit
                else
                {
                    DisableConduit(doc);
                }
            }
            else
            {
                // Show with boundary conditions and load maesh
                Proxies.PerformFea.FeaConduit.drawBoundaryConditions = true;
                Proxies.PerformFea.FeaConduit.drawLoadMesh = true;
                EnableConduit(doc);
            }
        }

        private static bool RecalculateAmaceFea(ImplantDirector director, Material material,
            double targetEdgeLength, double loadMagnitude, double loadMeshDegreesThreshold,
            double boundaryConditionDistanceThreshold, double boundaryConditionNoiseShellThreshold)
        {
            // Target directory
            var feaDirectory = DirectoryStructure.GetFeaFolder(director.Document);
            // Remove any existing FEA conduit
            InvalidateFeaConduit();
            // Perform the FEA
            var fea = FeaMaker.DoFea(director, material, targetEdgeLength, loadMagnitude,
                loadMeshDegreesThreshold, boundaryConditionDistanceThreshold,
                boundaryConditionNoiseShellThreshold, feaDirectory);

            if (fea != null)
            {
                // Report parameters
                IDSPIAmacePlugIn.WriteLine(Core.Enumerators.LogCategory.Diagnostic, "Cup Vector: {0}", director.cup.orientation.ToString());
                IDSPIAmacePlugIn.WriteLine(Core.Enumerators.LogCategory.Diagnostic, "Load Vector: {0}", fea.LoadVector.ToString());
                // Set in director
                director.SetFea(fea);
            }
            else
            {
                director.InvalidateFea();
            }

            return fea != null;

        }

        public static bool ConduitEnabled => Proxies.PerformFea.FeaConduit != null && Proxies.PerformFea.FeaConduit.Enabled;

        public static void EnableConduit(RhinoDoc doc)
        {
            if (Proxies.PerformFea.FeaConduit == null)
            {
                return;
            }

            Proxies.PerformFea.FeaConduit.Enabled = true;
            Visibility.HideAll(doc);
        }

        public static void DisableConduit(RhinoDoc doc)
        {
            Proxies.PerformFea.DisableConduit(doc);
        }

        public static void InvalidateFeaConduit()
        {
            Proxies.PerformFea.InvalidateFeaConduit();
        }

        public override bool CheckCommandCanExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            if (!base.CheckCommandCanExecute(doc, mode, director))
            {
                return false;
            }
            var objManager = new AmaceObjectManager(director);
            return objManager.IsTransitionPreviewAvailable();
        }
    }
}
