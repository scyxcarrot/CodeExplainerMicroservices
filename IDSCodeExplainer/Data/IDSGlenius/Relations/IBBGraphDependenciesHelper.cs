using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IDS.Glenius;
using IDS.Glenius.Graph;
using IDS.Glenius.ImplantBuildingBlocks;

namespace IDS.Glenius.Relations
{
    public class IBBGraphDependenciesHelper
    {
        public bool HandleDependencyManagementScaffoldPhaseAddReamingEntity(GleniusImplantDirector director)
        {
            var graph = director.Graph;
            graph.InvalidateGraph();
            return graph.NotifyBuildingBlockHasChanged(IBB.ScaffoldReamingEntity, IBB.RbvScaffold, IBB.RbvScaffoldDesign, IBB.ScapulaReamed, IBB.ScapulaDesignReamed);
        }

        public bool HandleDependencyManagementHeadPhaseAddReamingEntity(GleniusImplantDirector director)
        {
            var graph = director.Graph;
            graph.InvalidateGraph();
            return graph.NotifyBuildingBlockHasChanged(IBB.ReamingEntity, IBB.RBVHead, IBB.RbvHeadDesign, IBB.ScapulaReamed, IBB.ScapulaDesignReamed);
        }
    }
}
