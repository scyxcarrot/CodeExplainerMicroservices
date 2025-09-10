using IDS.CMF.CasePreferences;
using IDS.Core.Graph;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Relations
{
    public class CMFExecutableNode : ExecutableNode
    {
        public ICaseData CasePreference { get; set; }
        public List<Guid> Guids { get; set; }

        public CMFExecutableNode(string name, params NodeBase[] dependencies) : base(name, dependencies)
        {
        }

        public CMFExecutableNode(string name, IExecutableNodeComponent[] components, params NodeBase[] dependencies) : base(name, components, dependencies)
        {
        }

        public new bool Execute()
        {
            if (SkipExecution)
            {
                return true;
            }

            if (Components != null)
            {
                return !Components.Any() || Components.All(c =>
                {
                    var component = (IExecutableImplantNodeComponent) c;
                    component.CaseData = CasePreference;
                    component.Guids = Guids;
                    return c.Execute();
                });
            }

            return true;
        }
    }
}
