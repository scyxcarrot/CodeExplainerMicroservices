using IDS.Core.V2.TreeDb.Interface;
using IDS.Core.V2.TreeDb.Model;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace IDS.Core.V2.TreeDb.Utilities
{
    public static class TreeNodeUtilities
    {
        public static ImmutableList<TreeNode> GetConnectedTreeNodes(IEnumerable<IData> dataList)
        {
            var nodes = dataList.Select(data => new TreeNode(data)).ToList();
            return GetConnectedTreeNodes(nodes);
        }

        public static ImmutableList<TreeNode> GetConnectedTreeNodes(IEnumerable<TreeNode> nodes)
        {
            var nodeDictionary = nodes.ToDictionary(n => n.Id, n => n);

            foreach (var node in nodeDictionary.Values)
            {
                foreach (var parent in node.Parents)
                {
                    if (nodeDictionary.TryGetValue(parent, out var parentNode))
                    {
                        parentNode.AddNode(node);
                    }
                }
            }

            return nodeDictionary.Values.ToImmutableList();
        }
    }
}
