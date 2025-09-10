using IDS.Amace.Relations;
using IDS.Core.Enumerators;
using IDS.Core.ImplantDirector;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Amace.Enumerators
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
            { DesignPhase.Cup, new DesignPhaseProperty
                                    {
                                        Name = DesignPhase.Cup.ToString(),
                                        Value = (int)DesignPhase.Cup,
                                        StartActionFromDown = StartAction(PhaseChanger.StartCupPhaseFromLowerPhase),
                                        StartActionBoth = StartAction(PhaseChanger.StartCupPhase),
                                        StopAction = StopAction(PhaseChanger.StopCupPhase)
                                    } },
            { DesignPhase.Reaming, new DesignPhaseProperty
                                    {
                                        Name = DesignPhase.Reaming.ToString(),
                                        Value = (int)DesignPhase.Reaming,
                                        StartActionBoth = StartAction(PhaseChanger.StartReamingPhase),
                                        StopAction = StopAction(PhaseChanger.StopReamingPhase)
                                    } },
            { DesignPhase.Undercut, new DesignPhaseProperty
                                    {
                                        Name = DesignPhase.Undercut.ToString(),
                                        Value = (int)DesignPhase.Undercut
                                    } },
            { DesignPhase.Skirt, new DesignPhaseProperty
                                    {
                                        Name = DesignPhase.Skirt.ToString(),
                                        Value = (int)DesignPhase.Skirt,
                                        StartActionFromDown = StartAction(PhaseChanger.StartSkirtPhaseFromLowerPhase),
                                        StartActionBoth = StartAction(PhaseChanger.StartSkirtPhase)
                                    } },
            { DesignPhase.Scaffold, new DesignPhaseProperty
                                    {
                                        Name = DesignPhase.Scaffold.ToString(),
                                        Value = (int)DesignPhase.Scaffold,
                                        StartActionFromDown = StartAction(PhaseChanger.StartScaffoldPhaseFromLowerPhase),
                                        StartActionBoth = StartAction(PhaseChanger.StartScaffoldPhase),
                                        StopAction = StopAction(PhaseChanger.StopScaffoldPhase)
                                    } },
            { DesignPhase.CupQC, new DesignPhaseProperty
                                    {
                                        Name = DesignPhase.CupQC.ToString(),
                                        Value = (int)DesignPhase.CupQC,
                                        StartActionBoth = StartAction(PhaseChanger.StartCupQcPhase)
                                    } },
            { DesignPhase.Screws, new DesignPhaseProperty
                                    {
                                        Name = DesignPhase.Screws.ToString(),
                                        Value = (int)DesignPhase.Screws,
                                        StartActionFromUp = StartAction(PhaseChanger.StartScrewsPhaseFromHigherPhase),
                                        StartActionFromDown = StartAction(PhaseChanger.StartScrewsPhaseFromLowerPhase),
                                        StartActionBoth = StartAction(PhaseChanger.StartScrewsPhase),
                                        StopAction = StopAction(PhaseChanger.StopScrewsPhase)
                                    } },
            { DesignPhase.Plate, new DesignPhaseProperty
                                    {
                                        Name = DesignPhase.Plate.ToString(),
                                        Value = (int)DesignPhase.Plate,
                                        StartActionBoth = StartAction(PhaseChanger.StartPlatePhase),
                                        StopAction = StopAction(PhaseChanger.StopPlatePhase)
                                    } },
            { DesignPhase.ImplantQC, new DesignPhaseProperty
                                    {
                                        Name = DesignPhase.ImplantQC.ToString(),
                                        Value = (int)DesignPhase.ImplantQC,
                                        StartActionBoth = StartAction(PhaseChanger.StartImplantQcPhase),
                                        StopAction = StopAction(PhaseChanger.StopImplantQcPhase)
                                    } },
            { DesignPhase.Draft, new DesignPhaseProperty
                                    {
                                        Name = DesignPhase.Draft.ToString(),
                                        Value = (int)DesignPhase.Draft
                                    } },
            { DesignPhase.Export, new DesignPhaseProperty
                                    {
                                        Name = DesignPhase.Export.ToString(),
                                        Value = (int)DesignPhase.Export,
                                        StartActionBoth = StartAction(PhaseChanger.StartExportPhase)
                                    } },
            { DesignPhase.QC, new DesignPhaseProperty
                                    {
                                        Name = DesignPhase.QC.ToString(),
                                        Value = (int)DesignPhase.QC
                                    } }
        };

        #endregion

        public static Func<IImplantDirector, bool> StartAction(Func<ImplantDirector, bool> func)
        {
            return idirector => 
                {
                    ImplantDirector director = idirector as ImplantDirector;
                    return director != null && func(director);
                };
        }

        public static Func<IImplantDirector, DesignPhaseProperty, bool> StopAction(Func<ImplantDirector, DesignPhase, bool> func)
        {
            return (idirector, designPhase) =>
                {
                    var director = idirector as ImplantDirector;
                    var phase = Phases.Where(p => p.Value == designPhase);
                    if (director == null || !phase.Any())
                    {
                        return false;
                    }
                    return func(director, phase.First().Key);
                };
        }
    }
}