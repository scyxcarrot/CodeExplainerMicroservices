using IDS.Core.V2.Extensions;
using IDS.Core.V2.TreeDb.Helper;
using IDS.Core.V2.TreeDb.Interface;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace IDS.Core.V2.TreeDb.Model
{
    /// <summary>
    /// Node is use for manage cascading effect
    /// </summary>
    public sealed class TreeNode
    {
        /// <summary>
        /// Child connection for cascading effect
        /// </summary>
        private readonly List<NodeConnector> _childNodesConnector = new List<NodeConnector>();

        /// <summary>
        /// Parent connection for reverse disconnect with parent node
        /// </summary>
        private readonly List<NodeConnector> _parentsNodesConnector = new List<NodeConnector>();

        /// <summary>
        /// Node Id for Identification
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Parents Id
        /// </summary>
        public ImmutableList<Guid> Parents { get; }

        /// <summary>
        /// Create node from the data
        /// </summary>
        /// <param name="data"></param>
        public TreeNode(IData data)
        {
            Id = data.Id;
            Parents = data.Parents.ToImmutableList();
        }

        /// <summary>
        /// Check whether the node is child for current node
        /// </summary>
        /// <param name="node">Potential child node</param>
        /// <returns>True if the node is the child</returns>
        public bool IsChild(TreeNode node)
        {
            return node.Parents.Any(parentId => Id == parentId);
        }

        /// <summary>
        /// Purge invalid connector and get the child nodes
        /// </summary>
        /// <returns>Child nodes</returns>
        public ImmutableList<TreeNode> GetChildNodes()
        {
            _childNodesConnector
                .RemoveIf(c => !c.IsValid);
            return _childNodesConnector
                .Select(c => c.Child)
                .ToImmutableList();
        }

        /// <summary>
        /// Add node to tree
        /// </summary>
        /// <param name="node">The tree node going to add into the tree</param>
        public void AddNode(TreeNode node)
        {
            if (IsChild(node))
            {
                var childNodes = GetChildNodes();
                if (childNodes.All(n => n.Id != node.Id))
                {
                    var connector = new NodeConnector(this, node);
                    _childNodesConnector.Add(connector);
                    node._parentsNodesConnector.Add(connector);
                }
            }
        }

        /// <summary>
        /// Found node that match the node ID
        /// </summary>
        /// <param name="nodeId">The node Id going to search</param>
        /// <returns>The node match the node ID</returns>
        public TreeNode FoundNode(Guid nodeId)
        {
            return FoundNodeRecursively(nodeId, new List<Guid>());
        }

        /// <summary>
        /// Marked the nodes going to deleted and invalidate the node data from deepest of the tree
        /// </summary>
        /// <param name="nodeId">The node Id going to search</param>
        /// <param name="trackedHistoryId">The tracked node Id for break the circular dependency</param>
        /// <returns>The node match the node ID</returns>
        private TreeNode FoundNodeRecursively(Guid nodeId, List<Guid> trackedHistoryId)
        {
            using (var circularDependencyDetector =
                   new CircularDependencyDetector<Guid>(trackedHistoryId, Id))
            {
                if (circularDependencyDetector.HasCircularDependency())
                {
                    return null;
                }

                if (Id == nodeId)
                {
                    return this;
                }

                var childNodes = GetChildNodes();
                foreach (var childNode in childNodes)
                {
                    var nodeFound = childNode.FoundNodeRecursively(nodeId, trackedHistoryId);
                    if (nodeFound != null)
                    {
                        return nodeFound;
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Disconnect connection with parent node
        /// </summary>
        private void DisconnectParentNode()
        {
            foreach (var connector in _parentsNodesConnector)
            {
                connector.DisconnectFromChild();
            }

            _parentsNodesConnector.Clear();
        }

        /// <summary>
        /// Cascade delete from this node 
        /// </summary>
        /// <returns>The list of deleted node in correct sequence (It should be in deep first)</returns>
        public ImmutableList<Guid> CascadeDeleteFromTheNode()
        {
            var deletedIdInSequence = new List<Guid>();
            CascadeDeleteRecursively(new List<Guid>(), deletedIdInSequence);
            return deletedIdInSequence.Distinct().ToImmutableList();
        }

        /// <summary>
        /// Delete all the node
        /// </summary>
        /// <param name="trackedHistoryId"></param>
        /// <param name="deletedIdInSequence">The list of deleted item in sequence so invalidation can be done in sequence</param>
        private void CascadeDeleteRecursively(List<Guid> trackedHistoryId, List<Guid> deletedIdInSequence)
        {
            using (var circularDependencyDetector =
                   new CircularDependencyDetector<Guid>(trackedHistoryId, Id))
            {
                if (circularDependencyDetector.HasCircularDependency())
                {
                    return;
                }

                var childNodes = GetChildNodes();
                foreach (var childNode in childNodes)
                {
                    childNode.CascadeDeleteRecursively(trackedHistoryId, deletedIdInSequence);
                }

                deletedIdInSequence.Add(Id);

                DisconnectParentNode();
                _childNodesConnector.Clear();
            }
        }
    }
}
