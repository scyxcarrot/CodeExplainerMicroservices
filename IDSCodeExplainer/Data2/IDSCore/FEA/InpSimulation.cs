using System.Collections.Generic;

namespace IDS.Core.Fea
{
    public class InpSimulation
    {
        /// <summary>
        /// Gets the boundary conditions.
        /// </summary>
        /// <value>
        /// The boundary conditions.
        /// </value>
        public List<InpBoundaryCondition> BoundaryConditions { get; set; }

        /// <summary>
        /// Gets the loads.
        /// </summary>
        /// <value>
        /// The loads.
        /// </value>
        public List<InpLoad> Loads { get; set; }

        /// <summary>
        /// Gets or sets the material.
        /// </summary>
        /// <value>
        /// The material.
        /// </value>
        public Material Material { get; set; }

        /// <summary>
        /// Gets or sets the simulation n sets for the boundary conditions.
        /// </summary>
        /// <value>
        /// The simulation n sets.
        /// </value>
        public Dictionary<string, List<int>> NSetsBoundaryConditions { get; set; }

        /// <summary>
        /// Gets or sets the simulation n sets for the loads.
        /// </summary>
        /// <value>
        /// The simulation n sets.
        /// </value>
        public Dictionary<string, List<int>> NSetsLoad { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InpSimulation"/> class.
        /// </summary>
        public InpSimulation()
        {
            BoundaryConditions = new List<InpBoundaryCondition>();
            Loads = new List<InpLoad>();
            Material = new Material();
            NSetsBoundaryConditions = new Dictionary<string, List<int>>();
            NSetsLoad = new Dictionary<string, List<int>>();
        }
    }
}