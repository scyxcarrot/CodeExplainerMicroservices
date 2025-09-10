using IDS.CMF.Relations;
using IDS.Core.Enumerators;
using IDS.Core.ImplantDirector;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Enumerators
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
            { DesignPhase.Planning, new DesignPhaseProperty
                                    {
                                        Name = DesignPhase.Planning.ToString(),
                                        Value = (int)DesignPhase.Planning,
                                        StartActionBoth = StartAction(PhaseChanger.StartPlanningPhase),
                                        StopAction = StopAction(PhaseChanger.StopPlanningPhase)
                                    } },
            { DesignPhase.PlanningQC, new DesignPhaseProperty
                                    {
                                        Name = DesignPhase.PlanningQC.ToString(),
                                        Value = (int)DesignPhase.PlanningQC,
                                        StartActionBoth = StartAction(PhaseChanger.StartPlanningQCPhase),
                                        StopAction = StopAction(PhaseChanger.StopPlanningQCPhase)
                                    } },
             { DesignPhase.Implant, new DesignPhaseProperty
                                    {
                                        Name = DesignPhase.Implant.ToString(),
                                        Value = (int)DesignPhase.Implant,
                                        StartActionBoth = StartAction(PhaseChanger.StartImplantPhase),
                                        StopAction = StopAction(PhaseChanger.StopImplantPhase)
                                    } },
             { DesignPhase.Guide, new DesignPhaseProperty
                                    {
                                        Name = DesignPhase.Guide.ToString(),
                                        Value = (int)DesignPhase.Guide,
                                        StartActionBoth = StartAction(PhaseChanger.StartGuidePhase),
                                        StopAction = StopAction(PhaseChanger.StopGuidePhase)
                                    } },
             { DesignPhase.TeethBlock , new DesignPhaseProperty
                                    {
                                        Name = DesignPhase.TeethBlock.ToString(),
                                        Value = (int)DesignPhase.TeethBlock,
                                        StartActionBoth = StartAction(PhaseChanger.StartTeethBlockPhase),
                                        StopAction = StopAction(PhaseChanger.StopTeethBlockPhase)
                                    } },
            { DesignPhase.MetalQC, new DesignPhaseProperty
                                    {
                                        Name = DesignPhase.MetalQC.ToString(),
                                        Value = (int)DesignPhase.MetalQC,
                                        StartActionBoth = StartAction(PhaseChanger.StartMetalQCPhase),
                                        StopAction = StopAction(PhaseChanger.StopMetalQCPhase)
                                    } },
            { DesignPhase.Draft, new DesignPhaseProperty
            {
                Name = DesignPhase.Draft.ToString(),
                Value = (int)DesignPhase.Draft
            } }
        };

        #endregion

        public static Func<IImplantDirector, bool> StartAction(Func<CMFImplantDirector, bool> func)
        {
            return idirector => 
                {
                    var director = idirector as CMFImplantDirector;
                    return director != null && func(director);
                };
        }

        public static Func<IImplantDirector, DesignPhaseProperty, bool> StopAction(Func<CMFImplantDirector, DesignPhase, bool> func)
        {
            return (idirector, designPhase) =>
                {
                    var director = idirector as CMFImplantDirector;
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