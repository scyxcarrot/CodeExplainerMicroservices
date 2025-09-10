using IDS.Amace.Enumerators;
using System;
using System.Collections.Generic;
using IDS.Enumerators;

namespace IDS.Amace.Relations
{
    /**
     * Properties of the phases in the implant design process.
     */

    public static class DesignPhaseProperties
    {
        /**
         * Check if an item with the given phase flag is allowed
         * in the current phase.
         */

        public static bool IsAllowedInPhase(DesignPhase currentPhase, DesignPhase phaseFlags)
        {
            // Allowable when ALL flags in currentPhase are set to 1 in phaseFlags
            // - when the current phase is None (all zeros), this is always true
            // - when the phaseFlags are Any (all ones) this is always true
            return (currentPhase & phaseFlags) == currentPhase;
        }

        // TODO: remove this
        /**
         * Stop phase function linked to every design phase
         **/

        public static readonly Dictionary<DesignPhase, Func<ImplantDirector, bool>> phaseStopEvents = new Dictionary<DesignPhase, Func<ImplantDirector, bool>>
        {
        };

        // FromUp : Start action from up They take an ImplantDirector and return a bool
        public static readonly Dictionary<DesignPhase, Func<ImplantDirector, bool>> startActionFromUp = new Dictionary<DesignPhase, Func<ImplantDirector, bool>>
        {
            {DesignPhase.Screws, PhaseChanger.StartScrewsPhaseFromHigherPhase},
        };

        // FromDown : Start action from down They take an ImplantDirector and return a bool
        public static readonly Dictionary<DesignPhase, Func<ImplantDirector, bool>> startActionFromDown = new Dictionary<DesignPhase, Func<ImplantDirector, bool>>
        {
            {DesignPhase.Cup, PhaseChanger.StartCupPhaseFromLowerPhase},
            {DesignPhase.Skirt, PhaseChanger.StartSkirtPhaseFromLowerPhase},
            {DesignPhase.Scaffold, PhaseChanger.StartScaffoldPhaseFromLowerPhase},
            {DesignPhase.Screws, PhaseChanger.StartScrewsPhaseFromLowerPhase},
        };

        // Start action for both from up and from down (done after up/down action, after design phase
        // enter) They take an ImplantDirector and return a bool
        public static readonly Dictionary<DesignPhase, Func<ImplantDirector, bool>> startActionBoth = new Dictionary<DesignPhase, Func<ImplantDirector, bool>>
        {
            {DesignPhase.Cup, PhaseChanger.StartCupPhase},
            {DesignPhase.Reaming, PhaseChanger.StartReamingPhase},
            {DesignPhase.Skirt, PhaseChanger.StartSkirtPhase},
            {DesignPhase.Scaffold, PhaseChanger.StartScaffoldPhase},
            {DesignPhase.CupQC, PhaseChanger.StartCupQcPhase},
            {DesignPhase.Screws, PhaseChanger.StartScrewsPhase},
            {DesignPhase.Plate, PhaseChanger.StartPlatePhase},
            {DesignPhase.ImplantQC, PhaseChanger.StartImplantQcPhase},
            {DesignPhase.Export, PhaseChanger.StartExportPhase},
            {DesignPhase.Development, PhaseChanger.StartDevelopmentPhase},
        };

        // stop actions They take an ImplantDirector and a DesignPhase and return a bool
        public static readonly Dictionary<DesignPhase, Func<ImplantDirector, DesignPhase, bool>> stopAction = new Dictionary<DesignPhase, Func<ImplantDirector, DesignPhase, bool>>
        {
            {DesignPhase.Cup, PhaseChanger.StopCupPhase},
            {DesignPhase.Reaming, PhaseChanger.StopReamingPhase},
            {DesignPhase.Scaffold, PhaseChanger.StopScaffoldPhase},
            {DesignPhase.Screws, PhaseChanger.StopScrewsPhase},
            {DesignPhase.Plate, PhaseChanger.StopPlatePhase},
            {DesignPhase.ImplantQC, PhaseChanger.StopImplantQcPhase},
            {DesignPhase.Development, PhaseChanger.StopDevelopmentPhase},
        };
    }
}