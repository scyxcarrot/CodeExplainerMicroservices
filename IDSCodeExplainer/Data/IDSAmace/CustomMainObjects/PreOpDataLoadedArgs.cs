using System;

namespace IDS.Amace
{
    /// <summary>
    /// Event arguments for pre-op data loaded event
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    public class PreOpDataLoadedArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the inspector.
        /// </summary>
        /// <value>
        /// The inspector.
        /// </value>
        public PreOpInspector Inspector
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PreOpDataLoadedArgs"/> class.
        /// </summary>
        /// <param name="inspector">The inspector.</param>
        public PreOpDataLoadedArgs(PreOpInspector inspector)
        {
            Inspector = inspector;
        }
    }
}