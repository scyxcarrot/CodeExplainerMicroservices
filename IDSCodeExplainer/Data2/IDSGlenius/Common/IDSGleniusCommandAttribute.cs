using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using System.Collections.Generic;

namespace IDS.Common
{
    // Attribute for flagging/tagging test methods.
    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple = false)]
    public class IDSGleniusCommandAttribute : System.Attribute
    {
        // Implant building blocks required to run this command
        public readonly ISet<IBB> requiredBlocks;

        // Phases in the design phases where this command is available/runnable. This is a bit mask.
        public DesignPhase phasesWhereRunnable;

        // Constructor
        public IDSGleniusCommandAttribute(DesignPhase phaseFlag, params IBB[] blocks)
        {
            requiredBlocks = new HashSet<IBB>(blocks);
            phasesWhereRunnable = phaseFlag;
        }

        // Default constructor
        public IDSGleniusCommandAttribute()
        {
            requiredBlocks = new HashSet<IBB>();
            phasesWhereRunnable = DesignPhase.Any;
        }
    }
}