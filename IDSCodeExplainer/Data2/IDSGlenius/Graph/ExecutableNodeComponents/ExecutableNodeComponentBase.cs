using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IDS.Glenius;
using IDS.Glenius.Graph;
using IDS.Core.Graph;

namespace IDS.Glenius.Graph
{
    public class ExecutableNodeComponentBase : IExecutableNodeComponent
    {
        protected GleniusObjectManager objectManager;
        protected GleniusImplantDirector director;

        public ExecutableNodeComponentBase(GleniusImplantDirector director, GleniusObjectManager objectManager)
        {
            this.director = director;
            this.objectManager = objectManager;
        }

        public virtual bool Execute()
        {
            return true;
        }
    }
}
