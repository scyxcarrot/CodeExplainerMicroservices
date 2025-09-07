using IDS.Core.V2.Common;
using IDS.Core.V2.TreeDb.Interface;
using IDS.Core.V2.TreeDb.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace IDS.Core.V2.TreeDb.Model
{
    /// <summary>
    /// Tree for manage all the node
    /// </summary>
    public sealed class Tree
    {
        /// <summary>
        /// All node in dictionary form, for query the node easier
        /// </summary>
        private readonly Dictionary<Guid, TreeNode> _nodesDictionary;

        // TODO: Handle the tree construct not from root

        /// <summary>
        /// Create tree from database, it been private so it can only call by 'CreateTreeFromDatabase'
        /// </summary>
        /// <param name="database">The database</param>
        public Tree(IDatabase database)
        {
            var allData = database.ReadAll();

            var nodes = TreeNodeUtilities.GetConnectedTreeNodes(allData);

            _nodesDictionary = nodes.ToDictionary(n => n.Id, n => n);
        }

        /// <summary>
        /// Create a tree structure from scratch
        /// </summary>
        /// <param name="rootData">Data for root node</param>
        public Tree(IData rootData)
        {
            _nodesDictionary = new Dictionary<Guid, TreeNode>()
            {
                { rootData.Id, new TreeNode(rootData) } 
            };
        }

        /// <summary>
        /// Check the given node ID is exist in the tree
        /// </summary>
        /// <param name="nodeId">ID of the node</param>
        /// <returns>True if it exist</returns>
        public bool IsNodeExist(Guid nodeId)
        {
            return _nodesDictionary.ContainsKey(nodeId);
        }

        /// <summary>
        /// Get childen node IDs given parent node ID
        /// </summary>
        /// <param name="parentNodeId">ID of parent node</param>
        /// <returns>List of children node IDs</returns>
        public List<Guid> GetChildrenNodeIds(Guid parentNodeId)
        {
            var parentNode = _nodesDictionary[parentNodeId];

            var childrenNodes = parentNode.GetChildNodes();

            return childrenNodes.Select(node => node.Id).ToList();
        }

        /// <summary>
        /// Add new node to the tree
        /// </summary>
        /// <param name="data"></param>
        /// <returns>True if successfully added into to the tree</returns>
        public bool AddNewNode(IData data)
        {
            if (IsNodeExist(data.Id))
            {
                return false;
            }

            // make sure found all parents node, then only add as their child
            var parentsNode = new List<TreeNode>();
            foreach (var parentId in data.Parents)
            {
                if (!IsNodeExist(parentId))
                {
                    return false;
                }
                parentsNode.Add(_nodesDictionary[parentId]);
            }

            var newNode = new TreeNode(data);
            _nodesDictionary.Add(newNode.Id, newNode);
            foreach (var parentNode in parentsNode)
            {
                parentNode.AddNode(newNode);
            }

            return true;
        }

        /// <summary>
        /// Add batch of new nodes to the tree,
        /// make sure no missing node cause the tree connection break 
        /// </summary>
        /// <param name="batchData">Batch of data</param>
        /// <returns>True if successfully added into to the tree</returns>
        public bool BatchAddNewNode(ImmutableList<IData> batchData)
        {
            var newDataDictionary = batchData.ToDictionary(d => d.Id, d => d);
            var allAddedNodesId = new List<Guid>();
            while (newDataDictionary.Any())
            {
                var addedNodesId = new List<Guid>();
                foreach (var data in newDataDictionary.Values)
                {
                    if (AddNewNode(data))
                    {
                        addedNodesId.Add(data.Id);
                    }
                }

                if (!addedNodesId.Any())
                {
                    foreach (var addedNodeId in allAddedNodesId)
                    {
                        RemoveNode(addedNodeId);
                    }

                    return false;
                }

                allAddedNodesId.AddRange(addedNodesId);

                foreach (var addedNodeId in addedNodesId)
                {
                    newDataDictionary.Remove(addedNodeId);
                }
            }

            return true;
        }

        /// <summary>
        /// Remove node from tree and parent tree node
        /// </summary>
        /// <param name="targetedNodeId">Targeted Node ID</param>
        /// <returns>all removed nodes ID, null or throw exception if not successfully delete the node</returns>
        public ImmutableList<Guid> RemoveNode(Guid targetedNodeId)
        {
            if (!IsNodeExist(targetedNodeId))
            {
                return null;
            }

            var targetedNode = _nodesDictionary[targetedNodeId];

            if (!targetedNode.Parents.Any())
            {
                throw new IDSExceptionV2("Shouldn't remove the root node!");
            }

            var deletedNodesId = targetedNode.CascadeDeleteFromTheNode();
            if (deletedNodesId == null || !deletedNodesId.Any())
            {
                // This method is critical if not remove node properly, so it will have more check compare to other
                throw new IDSExceptionV2("The nodes should return list of deleted nodes and minimum node count is one");
            }

            var allRemoveSuccessfully = true;
            foreach (var deletedNodeId in deletedNodesId)
            {
                allRemoveSuccessfully &= _nodesDictionary.Remove(deletedNodeId);
            }

            if(!allRemoveSuccessfully)
            {
                // This method is critical if not remove node properly, so it will have more check compare to other
                throw new IDSExceptionV2("The nodes should return list of deleted nodes and minimum node count is one");
            }

            return deletedNodesId;
        }
    }
}
