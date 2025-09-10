using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using System.Collections.Generic;

namespace IDS.CMF.CommandHelpers
{
    //can move to IDSCore
    // Attribute for flagging/tagging test methods.
    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple = false)]
    public class IDSCMFCommandAttributes : System.Attribute
    {
        // ImplantConstants building blocks required to run this command
        public readonly ISet<IBB> RequiredBlocks;

        // Phases in the design phases where this command is available/runnable. This is a bit mask.
        public DesignPhase PhasesWhereRunnable { get; }

        // Constructor
        public IDSCMFCommandAttributes(DesignPhase phaseFlag, params IBB[] blocks)
        {
            RequiredBlocks = new HashSet<IBB>(blocks);
            PhasesWhereRunnable = phaseFlag;
        }

        // Default constructor
        public IDSCMFCommandAttributes()
        {
            RequiredBlocks = new HashSet<IBB>();
            PhasesWhereRunnable = DesignPhase.Any;
        }
    }
}