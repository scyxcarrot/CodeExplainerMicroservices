using IDS.Core.Enumerators;
using IDS.Core.ImplantDirector;
using IDS.Glenius.Relations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Glenius.Enumerators
{
    public static class DesignPhases
    {
        #region List of phases

        public static readonly Dictionary<DesignPhase, DesignPhaseProperty> Phases = new Dictionary<DesignPhase, DesignPhaseProperty>
        {
            { DesignPhase.Any, new DesignPhaseProperty
                                    {
                                        Name = DesignPhase.Any.ToString(),
                                        Value = (int)DesignPhase.Any
                                    } },
            { DesignPhase.None, new DesignPhaseProperty
                                    {
                                        Name = DesignPhase.None.ToString(),
                                        Value = (int)DesignPhase.None
                                    } },
            { DesignPhase.Initialization, new DesignPhaseProperty
                                    {
                                        Name = DesignPhase.Initialization.ToString(),
                                        Value = (int)DesignPhase.Initialization
                                    } },
            { DesignPhase.Reconstruction, new DesignPhaseProperty
                                    {
                                        Name = DesignPhase.Reconstruction.ToString(),
                                        Value = (int)DesignPhase.Reconstruction,
                                        StartActionBoth = StartAction(PhaseChanger.StartReconstructionPhase),
                                        StopAction = StopAction(PhaseChanger.StopReconstructionPhase)
                                    } },
            { DesignPhase.Head, new DesignPhaseProperty
                                    {
                                        Name = DesignPhase.Head.ToString(),
                                        Value = (int)DesignPhase.Head,
                                        StartActionFromDown = StartAction(PhaseChanger.StartHeadPhaseFromLowerPhase),
                                        StartActionBoth = StartAction(PhaseChanger.StartHeadPhase),
                                        StopAction = StopAction(PhaseChanger.StopHeadPhase)
                                    } },
            { DesignPhase.Screws, new DesignPhaseProperty
                                    {
                                        Name = DesignPhase.Screws.ToString(),
                                        Value = (int)DesignPhase.Screws,
                                        StartActionFromDown = StartAction(PhaseChanger.StartScrewsPhaseFromLowerPhase),
                                        StartActionBoth = StartAction(PhaseChanger.StartScrewsPhase),
                                        StopAction = StopAction(PhaseChanger.StopScrewsPhase)
                                    } },
            { DesignPhase.ScrewQC, new DesignPhaseProperty
                                    {
                                        Name = DesignPhase.ScrewQC.ToString(),
                                        Value = (int)DesignPhase.ScrewQC,
                                        StartActionBoth = StartAction(PhaseChanger.StartScrewQCPhase),
                                        StopAction = StopAction(PhaseChanger.StopScrewQCPhase)
                                    } },
            { DesignPhase.ScaffoldQC, new DesignPhaseProperty
                                    {
                                        Name = DesignPhase.ScaffoldQC.ToString(),
                                        Value = (int)DesignPhase.ScaffoldQC,
                                        StartActionBoth = StartAction(PhaseChanger.StartScaffoldQCPhase),
                                        StopAction = StopAction(PhaseChanger.StopScaffoldQCPhase)
                                    } },
            { DesignPhase.Plate, new DesignPhaseProperty
                                    {
                                        Name = DesignPhase.Plate.ToString(),
                                        Value = (int)DesignPhase.Plate,
                                        StartActionBoth = StartAction(PhaseChanger.StartPlatePhase),
                                        StopAction = StopAction(PhaseChanger.StopPlatePhase)
                                    } },
            { DesignPhase.Scaffold, new DesignPhaseProperty
                                    {
                                        Name = DesignPhase.Scaffold.ToString(),
                                        Value = (int)DesignPhase.Scaffold,
                                        StartActionBoth = StartAction(PhaseChanger.StartScaffoldPhase),
                                        StopAction = StopAction(PhaseChanger.StopScaffoldPhase)
                                    } },
            { DesignPhase.Draft, new DesignPhaseProperty
                                    {
                                        Name = DesignPhase.Draft.ToString(),
                                        Value = (int)DesignPhase.Draft
                                    } },
        };

        #endregion

        public static Func<IImplantDirector, bool> StartAction(Func<GleniusImplantDirector, bool> func)
        {
            return new Func<IImplantDirector, bool>(idirector =>
                {
                    GleniusImplantDirector director = idirector as GleniusImplantDirector;
                    return director != null && func(director);
                }
            );
        }

        public static Func<IImplantDirector, DesignPhaseProperty, bool> StopAction(Func<GleniusImplantDirector, DesignPhase, bool> func)
        {
            return new Func<IImplantDirector, DesignPhaseProperty, bool>((idirector, designPhase) =>
                {
                    var director = idirector as GleniusImplantDirector;
                    var phase = Phases.Where(p => p.Value == designPhase);
                    if (director == null || !phase.Any())
                    {
                        return false;
                    }
                    else
                    {
                        return func(director, phase.First().Key);
                    }
                }
            );
        }
    }
}