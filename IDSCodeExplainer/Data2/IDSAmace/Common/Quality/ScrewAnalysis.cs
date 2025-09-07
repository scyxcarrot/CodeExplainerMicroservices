using IDS.Amace.ImplantBuildingBlocks;
using System.Collections.Generic;

using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace IDS.Common.Quality
{
    /// <summary>
    /// ScrewAnalysis manages the calculations for the screw qc checks
    /// </summary>
    public abstract class ScrewAnalysis
    {
        /// <summary>
        /// Performs the screw intersection check.
        /// - shaft-shaft
        /// - shaft-head
        /// - head-head
        /// </summary>
        /// <param name="screws">The screws.</param>
        /// <returns></returns>
        public Dictionary<int, List<int>> PerformScrewIntersectionCheck(List<Screw> screws, double margin)
        {
            // Init dict, key = sourceScrew, value = list of problematic targetScrews
            Dictionary<int, List<int>> intersections = new Dictionary<int, List<int>>();

            // Loop over sourceScrews and targetScrews
            for (int i = 0; i < screws.Count; i++)
            {
                for (int j = i + 1; j < screws.Count; j++)
                {
                    Screw sourceScrew = screws[i];
                    Screw targetScrew = screws[j];

                    bool intersection = PerformScrewIntersectionCheck(sourceScrew, targetScrew, margin);
                    if (intersection)
                    {
                        // Add intersection for source screw
                        if (intersections.ContainsKey(sourceScrew.Index))
                            intersections[sourceScrew.Index].Add(targetScrew.Index);
                        else
                            intersections.Add(sourceScrew.Index, new List<int>() { targetScrew.Index });
                        // Add intersection for target screw
                        if (intersections.ContainsKey(targetScrew.Index))
                            intersections[targetScrew.Index].Add(sourceScrew.Index);
                        else
                            intersections.Add(targetScrew.Index, new List<int>() { sourceScrew.Index });
                    }
                }
            }

            // return the dict
            return intersections;
        }

        /// <summary>
        /// Performs the screw intersection check.
        /// </summary>
        /// <param name="sourceScrew">The source screw.</param>
        /// <param name="targetScrew">The target screw.</param>
        /// <returns></returns>
        protected abstract bool PerformScrewIntersectionCheck(Screw sourceScrew, Screw targetScrew, double margin);
    }
}