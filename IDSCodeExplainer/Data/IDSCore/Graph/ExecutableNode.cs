using System.Collections.Generic;
using System.Linq;

namespace IDS.Core.Graph
{

    public class ExecutableNode : NodeBase
    {
        public bool SkipExecution { get; set; }

        public List<IExecutableNodeComponent> Components { get; set; }

        public ExecutableNode(string name, params NodeBase[] dependencies) : base(name, dependencies)
        {
            Components = new List<IExecutableNodeComponent>();
            SkipExecution = false;
        }

        public ExecutableNode(string name, IExecutableNodeComponent[] components, params NodeBase[] dependencies) : base(name, dependencies)
        {
            Components = new List<IExecutableNodeComponent>();
            Components = components.ToList();
        }

        public bool Execute()
        {
            if (SkipExecution)
            {
                return true;
            }

            if (Components != null)
            {
                return !Components.Any() || Components.All(c => c.Execute());
            }

            return true;
        }
    }
}
