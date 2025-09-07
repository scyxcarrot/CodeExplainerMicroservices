using System.Collections.Generic;
using System.Linq;

namespace IDS.Core.Graph
{
    public class NodeBase
    {
        public string Name { get; private set; }
        private readonly List<NodeBase> dependencies;
        public object Tag { get; set; }

        public NodeBase(string name, params NodeBase[] dependencies)
        {
            Name = name;
            this.dependencies = dependencies.ToList();
        }

        public override string ToString()
        {
            return Name;
        }

        public IEnumerable<TNodeType> GetDependencies<TNodeType>() where TNodeType : NodeBase
        {
            return dependencies.ToList().ConvertAll(instance => (TNodeType) instance);
        }

        public void AddDependencyTo<TNodeType>(params TNodeType[] node) where TNodeType : NodeBase
        {
            foreach (var n in node)
            {
                if (!dependencies.Contains(n))
                {
                    dependencies.Add(n);
                }
            }
        }

        public void RemoveDependencyFrom<TNodeType>(params TNodeType[] nodes) where TNodeType : NodeBase
        {
            foreach (var n in nodes)
            {
                if (dependencies.Contains(n))
                {
                    dependencies.Remove(n);
                }
            }
        }
    }

}
