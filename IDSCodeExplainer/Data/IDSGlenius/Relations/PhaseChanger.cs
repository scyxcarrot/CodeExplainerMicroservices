using IDS.Core.ImplantDirector;
using IDS.Glenius.Enumerators;
using IDS.Glenius.Forms;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using IDS.Glenius.Visualization;
using Rhino.Geometry;

namespace IDS.Glenius.Relations
{
    public class PhaseChanger : Core.Relations.PhaseChanger
    {
        public static bool StartReconstructionPhase(GleniusImplantDirector director)
        {
            var objManager = new GleniusObjectManager(director);
            
            var scapula = objManager.GetBuildingBlock(IBB.Scapula).Geometry as Mesh;
            if (scapula != null)
            {
                var scapulaDesignReamedBlock = objManager.GetBuildingBlock(IBB.ScapulaDesignReamed);
                if (scapulaDesignReamedBlock == null)
                {
                    var scapulaDesignReamed = new Mesh();
                    scapulaDesignReamed.CopyFrom(scapula);
                    objManager.AddNewBuildingBlock(IBB.ScapulaDesignReamed, scapulaDesignReamed);
                }

                var scapulaReamedBlock = objManager.GetBuildingBlock(IBB.ScapulaReamed);
                if (scapulaReamedBlock == null)
                {
                    var scapulaReamed = new Mesh();
                    scapulaReamed.CopyFrom(scapula);
                    objManager.AddNewBuildingBlock(IBB.ScapulaReamed, scapulaReamed);
                }

                var scapulaDesignBlock = objManager.GetBuildingBlock(IBB.ScapulaDesign);
                if (scapulaDesignBlock == null)
                {
                    var scapulaDesign = new Mesh();
                    scapulaDesign.CopyFrom(scapula);
                    objManager.AddNewBuildingBlock(IBB.ScapulaDesign, scapulaDesign);
                }

                director.Graph.InvalidateGraph();
            }
            else
            {
                return false;
            }

            Visibility.ReconstructionDefault(director.Document);
            return true;
        }

        public static bool StopReconstructionPhase(GleniusImplantDirector director, DesignPhase targetPhase)
        {
            ReconstructionMeasurementVisualizer.Get().ShowAll(false);

            return true;
        }

        public static bool StartHeadPhase(GleniusImplantDirector director)
        {
            Visibility.HeadDefault(director.Document);

            HeadPanel.OpenPanel();
            var panelVM = HeadPanel.GetPanelViewModel();
            if (panelVM != null)
            {
                panelVM.Director = director;
                panelVM.AnatomicalMeasurements = director.AnatomyMeasurements;
                HeadPanel.SetEnabled(true);
                panelVM.UpdateAllVisualizations();
            }

            return true;
        }

        public static bool StartHeadPhaseFromLowerPhase(GleniusImplantDirector director)
        {
            var objectManager = new GleniusObjectManager(director);
            if (!objectManager.HasBuildingBlock(IBB.Head))
            {
                var maker = new HeadMaker(director);
                if (maker.CreateHead(HeadType.TYPE_36_MM))
                {
                    var alignment = new HeadAlignment(director.AnatomyMeasurements, objectManager, director.Document, director.defectIsLeft);
                    alignment.AlignHeadToDefaultPosition();
                }
                else
                {
                    return false;
                }
            }

            return true;            
        }

        public static bool StopHeadPhase(GleniusImplantDirector director, DesignPhase targetPhase)
        {
            //Disable head panel
            HeadPanel.SetEnabled(false);

            return true;
        }

        public static bool StartScrewsPhase(GleniusImplantDirector director)
        {
            Visibility.ScrewsDefault(director.Document);
            return true;
        }

        public static bool StartScrewsPhaseFromLowerPhase(GleniusImplantDirector director)
        {
            var objectManager = new GleniusObjectManager(director);
            if (!objectManager.HasBuildingBlock(IBB.M4ConnectionScrew))
            {
                var maker = new M4ConnectionScrewMaker(director);
                if (maker.CreateM4ConnectionScrew())
                {
                    maker.AlignM4ConnectionScrewToDefaultPosition();
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        public static bool StopScrewsPhase(GleniusImplantDirector director, DesignPhase targetPhase)
        {
            GlobalScrewIndexVisualizer.SetVisible(false);
            return true;
        }

        public static bool StartPlatePhase(GleniusImplantDirector director)
        {
            Visibility.PlateDefault(director.Document);
            return true;
        }

        public static bool StopPlatePhase(GleniusImplantDirector director, DesignPhase targetPhase)
        {
            return true;
        }

        public static bool StartScaffoldPhase(GleniusImplantDirector director)
        {
            Visibility.ScaffoldDefault(director.Document);
            return true;
        }

        public static bool StopScaffoldPhase(GleniusImplantDirector director, DesignPhase targetPhase)
        {
            return true;
        }

        public static bool StartScrewQCPhase(GleniusImplantDirector director)
        {
            //TODO: Visualization
            return true;
        }

        public static bool StopScrewQCPhase(GleniusImplantDirector director, DesignPhase targetPhase)
        {
            return true;
        }

        public static bool StartScaffoldQCPhase(GleniusImplantDirector director)
        {
            //TODO: Visualization
            return true;
        }

        public static bool StopScaffoldQCPhase(GleniusImplantDirector director, DesignPhase targetPhase)
        {
            return true;
        }

        public static bool ChangePhase(IImplantDirector director, DesignPhase targetPhase, bool askConfirmation)
        {
            return ChangePhase(director, DesignPhases.Phases[targetPhase], askConfirmation);
        }
    }
}