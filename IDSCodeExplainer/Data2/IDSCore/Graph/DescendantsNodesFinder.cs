using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Core.Graph
{
    public class DescendantsNodesFinder<TNodeType> where TNodeType : NodeBase
    {
        private readonly List<TNodeType> graph;

        public DescendantsNodesFinder(List<TNodeType> graph)
        {
            this.graph = graph;
        }

        public List<TNodeType> Find(TNodeType startingNode)
        {
            return Find(startingNode, startingNode, new List<TNodeType>());
        }

        private List<TNodeType> Find(TNodeType startingNode, TNodeType alphaNode, List<TNodeType> inputExistingDescendants)
        {
            var descendants = new List<TNodeType>();
            var existingDescendants = inputExistingDescendants;

            foreach (var currNode in graph)
            {
                //#1. Skip if it is checking its own node.
                //#2. Skip if the parent of current node are not starting node AND descendants should 
                //    not already contain current node (prevent same node to be added).
                //#3. Skip if existing descendants found already contain current node.
                if (currNode == startingNode ||
                    !currNode.GetDependencies<TNodeType>().Any(cnParent => cnParent == startingNode && !descendants.Contains(currNode)) || 
                    existingDescendants.Contains(currNode))
                {
                    continue;
                }

                descendants.Add(currNode);
                existingDescendants = descendants;

                //Circular Dependency Check!
                if (descendants.Contains(alphaNode))
                {
                    throw new ArgumentException("ERROR! Circular Dependency Detected!");
                }

                var resDep = Find(currNode, alphaNode, existingDescendants);
                descendants.AddRange(resDep);
            }

            return descendants;
        }

        public List<TNodeType> CreateDescendantsExecutionSequence(params TNodeType[] startingNodes)
        {
            var descendants = new List<TNodeType>();

            List<TNodeType> ibbNodes = new List<TNodeType>();

            foreach (var startingNode in startingNodes)
            {
                ibbNodes.Add(startingNode);

                var desc = Find(startingNode);

                foreach (var n in desc)
                {
                    if (!descendants.Contains(n))
                    {
                        descendants.Add(n);
                    }
                }
            }

            //This will actually create a sorted nodes inclusive with their parents
            var sorter = new NodeTopologySort();
            var sortedDescendants = sorter.Sort(descendants, x => x.GetDependencies<TNodeType>()).ToList();

            //Remove the parents that are not required to be processed.
            return sortedDescendants.Where(n => descendants.Contains(n) && !ibbNodes.Contains(n)).ToList();
        }
    }
}
