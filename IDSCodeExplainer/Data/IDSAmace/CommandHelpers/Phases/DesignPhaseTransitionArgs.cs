using IDS.Amace.Enumerators;

namespace IDS.Common.Relations
{
    /// <summary>
    /// Class that encapsulates information about a transition from
    /// one phase in the design process to another phase.
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    public class DesignPhaseTransitionArgs : System.EventArgs
    {
        /// <summary>
        /// From phase
        /// </summary>
        public readonly DesignPhase fromPhase;

        /// <summary>
        /// To phase
        /// </summary>
        public readonly DesignPhase toPhase;

        /// <summary>
        /// The document
        /// </summary>
        public readonly Rhino.RhinoDoc document;

        /// <summary>
        /// Initializes a new instance of the <see cref="DesignPhaseTransitionArgs"/> class.
        /// </summary>
        /// <param name="fromPhase">From phase.</param>
        /// <param name="toPhase">To phase.</param>
        /// <param name="document">The document.</param>
        public DesignPhaseTransitionArgs(DesignPhase fromPhase, DesignPhase toPhase, Rhino.RhinoDoc document)
        {
            this.fromPhase = fromPhase;
            this.toPhase = toPhase;
            this.document = document;
        }
    }
}