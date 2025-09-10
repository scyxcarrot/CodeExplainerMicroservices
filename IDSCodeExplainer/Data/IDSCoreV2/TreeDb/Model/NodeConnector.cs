namespace IDS.Core.V2.TreeDb.Model
{
    /// <summary>
    /// Node connector for reverse disconnect the connection between parent and child node
    /// </summary>
    public sealed class NodeConnector
    {
        /// <summary>
        /// Parent node
        /// </summary>
        public TreeNode Parent { get; private set; }

        /// <summary>
        /// Child node
        /// </summary>
        public TreeNode Child { get; private set; }

        /// <summary>
        /// Is valid connection if parent and child is exist
        /// </summary>
        public bool IsValid => Parent != null && Child != null;

        /// <summary>
        /// Create a connection between parent and child
        /// </summary>
        /// <param name="parent">Parent node</param>
        /// <param name="child">Child node</param>
        public NodeConnector(TreeNode parent, TreeNode child)
        {
            Parent = parent;
            Child = child;
        }

        /// <summary>
        /// Disconnect with parent from child
        /// </summary>
        public void DisconnectFromChild()
        {
            Child = null;
        }

        /// <summary>
        /// Disconnect with child from parent
        /// </summary>
        public void DisconnectFromParent()
        {
            Parent = null;
        }
    }
}
