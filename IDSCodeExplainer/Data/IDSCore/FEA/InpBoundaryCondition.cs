namespace IDS.Core.Fea
{
    public class InpBoundaryCondition
    {
        /// <summary>
        /// Gets the boundary from axis.
        /// </summary>
        /// <value>
        /// The boundary from axis.
        /// </value>
        public int BoundaryFromAxis { get; }

        /// <summary>
        /// Gets the name of the boundary n set.
        /// </summary>
        /// <value>
        /// The name of the boundary n set.
        /// </value>
        public string BoundaryNSetName { get; }

        /// <summary>
        /// Gets the boundary to axis.
        /// </summary>
        /// <value>
        /// The boundary to axis.
        /// </value>
        public int BoundaryToAxis { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InpBoundaryCondition"/> class.
        /// </summary>
        /// <param name="boundaryNSetName">Name of the boundary n set.</param>
        /// <param name="boundaryFromAxis">The boundary from axis.</param>
        /// <param name="boundaryToAxis">The boundary to axis.</param>
        public InpBoundaryCondition(string boundaryNSetName, int boundaryFromAxis, int boundaryToAxis)
        {
            BoundaryFromAxis = boundaryFromAxis;
            BoundaryToAxis = boundaryToAxis;
            BoundaryNSetName = boundaryNSetName;
        }
    }
}