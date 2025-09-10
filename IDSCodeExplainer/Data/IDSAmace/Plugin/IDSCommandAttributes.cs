using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using System.Collections.Generic;

namespace IDS.Common
{
    // Attribute for flagging/tagging test methods.
    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple = false)]
    public class IDSCommandAttributes : System.Attribute
    {
        // Whether the command requires pre-op data to be loaded
        public bool RequiresInspector;

        // Implant building blocks required to run this command
        public readonly ISet<IBB> RequiredBlocks;

        // Phases in the design phases where this command is available/runnable. This is a bit mask.
        public DesignPhase PhasesWhereRunnable;

        // Constructor
        public IDSCommandAttributes(bool requiresInspector, DesignPhase phaseFlag, params IBB[] blocks)
        {
            this.RequiresInspector = requiresInspector;
            RequiredBlocks = new HashSet<IBB>(blocks);
            PhasesWhereRunnable = phaseFlag;
        }

        // Default constructor
        public IDSCommandAttributes()
        {
            RequiresInspector = false;
            RequiredBlocks = new HashSet<IBB>();
            PhasesWhereRunnable = DesignPhase.Any;
        }
    }
}