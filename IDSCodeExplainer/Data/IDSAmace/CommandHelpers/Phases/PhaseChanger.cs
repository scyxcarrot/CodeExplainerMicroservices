using IDS.Amace.CommandHelpers;
using IDS.Amace.Enumerators;
using IDS.Amace.GUI;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Operations;
using IDS.Amace.Proxies;
using IDS.Core.Enumerators;
using IDS.Core.ImplantDirector;
using IDS.Core.Operations;
using IDS.Core.PluginHelper;
using IDS.Operations.CupPositioning;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.UI;
using System;
using System.Linq;
using Visibility = IDS.Amace.Visualization.Visibility;

namespace IDS.Amace.Relations
{
    public class PhaseChanger : Core.Relations.PhaseChanger
    {
        /// <summary>
        /// Starts the cup phase.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <returns></returns>
        public static bool StartCupPhase(ImplantDirector director)
        {
            // TODO Understand what this does arent these dependencies being deleted when the cup is
            // moved/changed Delete reamer dependencies to make sure cup reaming is not redone on the
            // reamed pelvis if the cup is not repositioned
            Dependencies dependencies = new Dependencies();
            dependencies.DeleteBlockDependencies(director, IBB.CupReamingEntity);

            // Show cup panel
            CupPanel panel = CupPanel.GetPanel();
            if (panel != null)
            {
                panel.Enable();
            }

            // Set visualization
            Visibility.CupDefault(director.Document);

            // Success
            return true;
        }

        /// <summary>
        /// Starts the cup phase from lower phase.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <returns></returns>
        public static bool StartCupPhaseFromLowerPhase(ImplantDirector director)
        {
            // Create a default cup
            bool success = CupMaker.CreateCup(director);
            if (!success)
            {
                return false;
            }

            // Success
            return true;
        }

        /// <summary>
        /// Starts the cup qc phase.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <returns></returns>
        public static bool StartCupQcPhase(ImplantDirector director)
        {
            // Visualization
            Visibility.CupQcDefault(director.Document);

            return true;
        }

        /// <summary>
        /// Starts the development phase.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <returns></returns>
        public static bool StartDevelopmentPhase(ImplantDirector director)
        {
            // Visualization
            Visibility.CupDefault(director.Document);

            // Success
            return true;
        }

        /// <summary>
        /// Starts the export phase.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <returns></returns>
        public static bool StartExportPhase(ImplantDirector director)
        {
            // Visualization
            Visibility.ExportPhase(director.Document);

            // Success
            return true;
        }

        /// <summary>
        /// Starts the implant qc phase.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <returns></returns>
        public static bool StartImplantQcPhase(ImplantDirector director)
        {
            // Visualization
            Visibility.ImplantQcDefault(director.Document);

            // Success
            return true;
        }

        /// <summary>
        /// Starts the plate phase.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <returns></returns>
        public static bool StartPlatePhase(ImplantDirector director)
        {
            var doc = director.Document;

            var objectManager = new AmaceObjectManager(director);

            if (!objectManager.HasBuildingBlock(IBB.ROIContour))
            {
                var helper = new RegionOfInterestCommandHelper(objectManager);
                director.ContourPlane = helper.GetContourPlaneBasedOnAcetabularPlane(doc, director.cup.cupRimPlane.Origin);
                var curvesProjectedOnPlane = helper.GetRoiContourBasedOnSkirtBoneCurve(director.ContourPlane);
                curvesProjectedOnPlane.ForEach(c => helper.AddRoiContour(c, director.ContourPlane));
                director.InvalidateFea();
            }

            // Visualization
            Visibility.PlateDefault(doc);

            // Success
            return true;
        }

        /// <summary>
        /// Starts the plate phase from lower phase.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <returns></returns>
        public static bool StartPlatePhaseFromLowerPhase(ImplantDirector director)
        {
            // Success
            return true;
        }

        /// <summary>
        /// Starts the reaming phase.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <returns></returns>
        public static bool StartReamingPhase(ImplantDirector director)
        {
            // Visualization
            Visibility.ReamingDefault(director.Document);

            // Success
            return true;
        }

        /// <summary>
        /// Starts the scaffold phase.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <returns></returns>
        public static bool StartScaffoldPhase(ImplantDirector director)
        {
            // Visualization
            Visibility.ScaffoldDefault(director.Document);

            // Success
            return true;
        }

        /// <summary>
        /// Starts the scaffold phase from lower phase.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <returns></returns>
        public static bool StartScaffoldPhaseFromLowerPhase(ImplantDirector director)
        {
            AmaceObjectManager objectManager = new AmaceObjectManager(director);

            // Recreate the scaffold if it does not exist create scaffold can fail if not all
            // building blocks are there, therefore it should not affect the success fo the action
            if (objectManager.GetBuildingBlockId(IBB.ScaffoldVolume) == Guid.Empty)
            {
                ScaffoldMaker.CreateScaffold(director);
            }

            // Success
            return true;
        }

        public static bool StartScrewsPhase(ImplantDirector director)
        {
            // Visualization
            ScrewPanelHelper.ShowScrewPanel(director);

            // Success
            return true;
        }

        /// <summary>
        /// Starts the screws phase from higher phase.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <returns></returns>
        public static bool StartScrewsPhaseFromHigherPhase(ImplantDirector director)
        {
            // Visualization
            Visibility.ScrewsAndPlateFlat(director.Document);

            // Success
            return true;
        }

        /// <summary>
        /// Starts the screws phase from lower phase.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <returns></returns>
        public static bool StartScrewsPhaseFromLowerPhase(ImplantDirector director)
        {
            // Visualization
            Visibility.ScrewsAndCup(director.Document);

            // Success
            return true;
        }

        /// <summary>
        /// Starts the skirt phase.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <returns></returns>
        public static bool StartSkirtPhase(ImplantDirector director)
        {
            // Set visualization
            Visibility.SkirtDefault(director.Document);

            // Success
            return true;
        }

        /// <summary>
        /// Starts the skirt phase from lower phase.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <returns></returns>
        public static bool StartSkirtPhaseFromLowerPhase(ImplantDirector director)
        {
            var objectManager = new AmaceObjectManager(director);

            // If the skirt mesh does not exists, it needs to be built create skirt can fail if not
            // all building blocks are there, therefore it should not affect the success fo the action
            if (objectManager.GetBuildingBlockId(IBB.SkirtMesh) == Guid.Empty)
            {
                SkirtMaker.CreateSkirt(director);
            }

            // Success
            return true;
        }

        /// <summary>
        /// Stops the cup phase.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <param name="targetPhase">The target phase.</param>
        /// <returns></returns>
        public static bool StopCupPhase(ImplantDirector director, DesignPhase targetPhase)
        {
            // Disable cup panel
            CupPanel cup_panel = CupPanel.GetPanel();
            if (cup_panel == null)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, 
                    "The cup panel needs to be docked before you can leave the cup phase. Right click " +
                    "on the Tab titles of the side bar and activate the cup panel. " +
                    "Then try to start the new phase again.");
                return false;
            }

            cup_panel.Enabled = false;

            // Only do these things when coming from lower phase
            if (director.CurrentDesignPhase < targetPhase)
            {
                var dep = new Dependencies();
                dep.UpdateCupAndAdditionalReaming(director);
            }

            // Show layers panel
            Panels.OpenPanel(Rhino.UI.PanelIds.Layers);

            // Hide rbv preview
            CupPanel cupPanel = CupPanel.GetPanel();
            cupPanel.orientationConduit.Enabled = false;

            // Hide Measurement preview if necessary
            if (Measure.MeasurementConduit != null)
            {
                Measure.MeasurementConduit.Enabled = false;
                // Move back to perspective view
                RhinoView viewPerspective = director.Document.Views.ToDictionary(v => v.ActiveViewport.Name, v => v)["Perspective"];
                director.Document.Views.ActiveView = viewPerspective;
                director.Document.Views.ActiveView.ActiveViewport.ChangeToParallelProjection(true);
            }

            // Success
            return true;
        }

        /// <summary>
        /// Stops the development phase.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <param name="targetPhase">The target phase.</param>
        /// <returns></returns>
        public static bool StopDevelopmentPhase(ImplantDirector director, DesignPhase targetPhase)
        {
            // sorry message
            IDSPluginHelper.WriteLine(LogCategory.Default, "Once in development, always in development...");

            // Once in develoment, always in development
            return false;
        }

        /// <summary>
        /// Stops the implant qc phase.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <param name="targetPhase">The target phase.</param>
        /// <returns></returns>
        public static bool StopImplantQcPhase(ImplantDirector director, DesignPhase targetPhase)
        {
            // Hide screw conduit
            ScrewInfo.DesignPhase = DesignPhase.None; // reset to make sure the conduit is refreshed when it is called again
            ScrewInfo.Disable(director.Document);
            // Hide Fea conduit
            PerformFea.DisableConduit(director.Document);

            // Show layers panel
            Rhino.UI.Panels.OpenPanel(Rhino.UI.PanelIds.Layers);

            // Success
            return true;
        }

        /// <summary>
        /// Stops the plate phase.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <param name="targetPhase">The target phase.</param>
        /// <returns></returns>
        public static bool StopPlatePhase(ImplantDirector director, DesignPhase targetPhase)
        {
            // init
            bool success;

            // Turn of the conduit
            if (TogglePlateAnglesVisualisation.Enabled)
            {
                TogglePlateAnglesVisualisation.Disable(director);
            }

            // When going to a lower phase, we dont need to do this
            if (director.CurrentDesignPhase > targetPhase)
            {
                return true;
            }

            // Create trimmed bumps that have been deleted
            ScrewManager screwManager = new ScrewManager(director.Document);
            screwManager.CreateMissingTrimmedLateralBumps();
            screwManager.CreateMissingTrimmedMedialBumps();

            // Create the cup studs
            IDSPluginHelper.WriteLine(LogCategory.Default, "Creating the studs, please wait...");
            success = StudMaker.GenerateAmaceStuds(director);
            if (!success)
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, "Studs could not be created, abort.");
                return false;
            }
            IDSPluginHelper.WriteLine(LogCategory.Default, "Done creating the studs...");

            // Perform the final reaming on the design pelvis (including medial bumps)
            IDSPluginHelper.WriteLine(LogCategory.Default, "Creating the final reaming, please wait...");
            ReamingManager reamingManager = new ReamingManager(director);
            success = reamingManager.PerformFinalReaming();
            if (!success)
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, "Final reaming could not be created, abort.");
                return false;
            }
            IDSPluginHelper.WriteLine(LogCategory.Default, "Done creating the final reaming...");

            // Create the scaffold
            IDSPluginHelper.WriteLine(LogCategory.Default, "Creating the finalized scaffold, please wait...");
            success = ScaffoldMaker.CreateFinalizedScaffold(director);
            if (!success)
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, "Scaffold could not be created, abort.");
                return false;
            }
            IDSPluginHelper.WriteLine(LogCategory.Default, "Done creating the scaffold...");

            // Implant clearance mesh
            IDSPluginHelper.WriteLine(LogCategory.Default, "Creating the implant clearance mesh, please wait...");

            AmaceObjectManager objectManager = new AmaceObjectManager(director);
            Mesh reamedPelvis = objectManager.GetBuildingBlock(IBB.OriginalReamedPelvis).Geometry as Mesh;
            Mesh solidPlateBottom = objectManager.GetBuildingBlock(IBB.SolidPlateBottom).Geometry as Mesh;
            Mesh implantClearance = AnalysisMeshMaker.CreateImplantClearanceMesh(solidPlateBottom, reamedPelvis);
            if (implantClearance == null)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Could not create implant clearance mesh.");
                return false;
            }
            // Set it as building block
            Guid plateClearanceID = objectManager.GetBuildingBlockId(IBB.PlateClearance);
            objectManager.SetBuildingBlock(IBB.PlateClearance, implantClearance, plateClearanceID);
            IDSPluginHelper.WriteLine(LogCategory.Default, "Done creating the implant clearance mesh");

            // Success
            return true;
        }

        /// <summary>
        /// Stops the reaming phase.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <param name="targetPhase">The target phase.</param>
        /// <returns></returns>
        public static bool StopReamingPhase(ImplantDirector director, DesignPhase targetPhase)
        {
            // Do nothing if the target phase is lower
            if (director.CurrentDesignPhase > targetPhase)
            {
                return true;
            }


            // Perform the final reaming on the design pelvis (including medial bumps)
            IDSPluginHelper.WriteLine(LogCategory.Default, "Creating the final reaming, please wait...");
            ReamingManager reamingManager = new ReamingManager(director);
            bool success = reamingManager.PerformFinalReaming();
            if (!success)
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, "Final reaming could not be created, abort.");
                return false;
            }
            IDSPluginHelper.WriteLine(LogCategory.Default, "Done creating the final reaming...");

            // Success
            return true;
        }

        /// <summary>
        /// Stops the scaffold phase.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <param name="targetPhase">The target phase.</param>
        /// <returns></returns>
        public static bool StopScaffoldPhase(ImplantDirector director, DesignPhase targetPhase)
        {
            // Do nothing if the target phase is lower
            if (director.CurrentDesignPhase > targetPhase)
            {
                return true;
            }

            AmaceObjectManager objectManager = new AmaceObjectManager(director);
            // Create wraps if necessary
            bool allExist = (objectManager.GetBuildingBlockId(IBB.WrapTop) != Guid.Empty &&
                                objectManager.GetBuildingBlockId(IBB.WrapBottom) != Guid.Empty &&
                                objectManager.GetBuildingBlockId(IBB.WrapSunkScrew) != Guid.Empty &&
                                objectManager.GetBuildingBlockId(IBB.WrapScrewBump) != Guid.Empty);
            if (!allExist)
            {
                if (!WrapMaker.CreateAllWraps(director))
                {
                    return false;
                }
            }

            //If it is not present, it will create it, else it will retrieve from document.
            var intersectionEntity =
                TransitionIntersectionEntityCommandHelper.HandleGetIntersectionEntity(director, Constants.ImplantTransitions.IntersectionEntityResolution, true);

            return intersectionEntity != null;
        }

        /// <summary>
        /// Stops the screws phase.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <param name="targetPhase">The target phase.</param>
        /// <returns></returns>
        public static bool StopScrewsPhase(ImplantDirector director, DesignPhase targetPhase)
        {
            // Hide display conduits
            ScrewInfo.DesignPhase = DesignPhase.None; // reset to make sure the conduit is refreshed when it is called again
            ScrewInfo.Disable(director.Document);

            // Show layers panel
            Panels.OpenPanel(PanelIds.Layers);

            // Success
            return true;
        }

        public static bool ChangePhase(IImplantDirector director, DesignPhase targetPhase)
        {
            return ChangePhase(director, targetPhase, true);
        }

        public static bool ChangePhase(IImplantDirector director, DesignPhase targetPhase, bool askConfirmation)
        {
            return ChangePhase(director, DesignPhases.Phases[targetPhase], askConfirmation);
        }
    }
}