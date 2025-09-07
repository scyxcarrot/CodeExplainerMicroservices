using System;
using System.Collections.Generic;
using System.Linq;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Core.Graph;
using Rhino;
using Rhino.Commands;

namespace IDS.Glenius.Graph
{
    public class GleniusGraph
    {
        public GleniusImplantDirector Director { get; }
        public GleniusObjectManager ObjectManager { get; }
        
        //IBBGraph and nodeDictionary should always be inSync. node dictionary is to get the node by IBB enum as the key for ease of access.
        private List<ExecutableNode> IBBGraph { get; set; }
        private readonly Dictionary<IBB, ExecutableNode> nodeDictionary;

        public bool IsUndoRedoSubscribed { get; private set; }

        public bool IsBuildingBlockNotificationEnabled { get; set; } = true;

        public GleniusGraph(GleniusImplantDirector director)
        {
            IsUndoRedoSubscribed = false;
            Director = director;
            ObjectManager = new GleniusObjectManager(Director);
            nodeDictionary = new Dictionary<IBB, ExecutableNode>();
            IBBGraph = new List<ExecutableNode>();
            InvalidateGraph();
        }

        public void SubscribeForGraphInvalidation()
        {
            if (!IsUndoRedoSubscribed)
            {
                Command.EndCommand += CommandOnEndCommand;
                IsUndoRedoSubscribed = true;
            }
        }

        public void UnsubscribeForGraphInvalidation()
        {
            if (IsUndoRedoSubscribed)
            {
                Command.EndCommand -= CommandOnEndCommand;
                IsUndoRedoSubscribed = false;
            }
        }

        private void CommandOnEndCommand(object sender, CommandEventArgs commandEventArgs)
        {
            //Since user can do many things, Undo Redo via commands and all other commands, invalidate the graph for any changes.
            //TODO: Remove graph generation for each command as it no longer valid and overriden by this.
            //TODO: Also unit test extension for actual dependencies and scenario testing.
            InvalidateGraph();
        }

        public List<string> GetGraphIBBInfo()
        {
            return nodeDictionary.Select(x => x.Key.ToString()).ToList();
        }

        //Returns Null if should not exist
        private ExecutableNode HandleNodeInvalidation(IBB ibb, params ExecutableNode[] dependencies)
        {
            return HandleNodeInvalidation(ibb, null, dependencies);
        }

        //Returns Null if should not exist
        private ExecutableNode HandleNodeInvalidation(IBB ibb, IExecutableNodeComponent[] component, params ExecutableNode[] dependencies)
        {
            ExecutableNode node = null;
            if (ObjectManager.HasBuildingBlock(ibb))
            {
                node = GetNode(ibb);

                if (node != null)
                {
                    return node;
                }

                node = AddNode(ibb);

                if (dependencies != null && dependencies.Any())
                {
                    foreach (var n in dependencies)
                    {
                        if (n == null)
                        {
                            continue;
                        }

                        IBB parentIbb;
                        if (Enum.TryParse(n.Name, out parentIbb))
                        {
                            if (ObjectManager.HasBuildingBlock(parentIbb))
                            {
                                node.AddDependencyTo(n);
                            }
                            //TODO Should throw exception here?
                        }
                        else
                        {
                            throw new ArgumentException("HandleNodeInvalidation, Enum.TryParse(n.Name, out parentIbb) is wrong!");
                        }
                    }
                }

                if (component != null && component.Any())
                {
                    node.Components = component.ToList();
                }
            }
            else
            {
                if (HasNode(ibb))
                {
                    DeleteNode(ibb);
                }
            }

            return node;
        }

        //Probably there's a better way
        //HOW & WHEN TO USE IT
        //NEW IBB IS ADDED -> INVALIDATE FIRST BEFORE NOTIFY 
        //IBB IS DELETED -> NOTIFY FIRST BEFORE INVALIDATE
        public void InvalidateGraph()
        {
            IBBGraph.Clear();
            nodeDictionary.Clear();

            //Reamings
            var nScapula = HandleNodeInvalidation(IBB.Scapula); //Independent Node
            var nScapulaDesign = HandleNodeInvalidation(IBB.ScapulaDesign, nScapula); //Independent Node
            var nReamingEntity = HandleNodeInvalidation(IBB.ReamingEntity); //Independent Node
            var nScaffoldReamingEntities = HandleNodeInvalidation(IBB.ScaffoldReamingEntity, nReamingEntity); //Independent Node

            //Reaming RBV
            HandleNodeInvalidation(IBB.RBVHead, new[] { new UpdateRbvHeadComponent(Director, ObjectManager) },
                nScapula, nReamingEntity);
            HandleNodeInvalidation(IBB.RbvHeadDesign, new[] { new UpdateRbvHeadDesignComponent(Director, ObjectManager) },
                nScapulaDesign, nReamingEntity);
            HandleNodeInvalidation(IBB.RbvScaffold, new[] { new UpdateRbvScaffoldComponent(Director, ObjectManager) },
                nScapula, nScaffoldReamingEntities, nReamingEntity);
            HandleNodeInvalidation(IBB.RbvScaffoldDesign, new[] { new UpdateRbvScaffoldDesignComponent(Director, ObjectManager) },
                nScapulaDesign, nScaffoldReamingEntities, nReamingEntity);

            //Reaming Scapula Reams
            HandleNodeInvalidation(IBB.ScapulaReamed, new[] { new UpdateScapulaReamedComponent(Director, ObjectManager) },
                nScapula, nReamingEntity, nScaffoldReamingEntities);
            var nScapulaDesignReamed = HandleNodeInvalidation(IBB.ScapulaDesignReamed, new[] { new UpdateScapulaDesignReamedComponent(Director, ObjectManager) },
                nScapulaDesign, nReamingEntity, nScaffoldReamingEntities);

            //BasePlate
            var nBasePlateTopContour = HandleNodeInvalidation(IBB.BasePlateTopContour); //Independent Node
            var nBasePlateBottomContour = HandleNodeInvalidation(IBB.BasePlateBottomContour); //Independent Node

            HandleNodeInvalidation(IBB.PlateBasePlate, 
                new[] { new UpdatePlateBasePlateComponent(Director, ObjectManager) },
                nBasePlateTopContour, nBasePlateBottomContour);

            //Scaffold
            var nScaffoldPrimaryBorder = HandleNodeInvalidation(IBB.ScaffoldPrimaryBorder); //Independent Node
            var nScaffoldSecondaryBorder = HandleNodeInvalidation(IBB.ScaffoldSecondaryBorder); //Independent Node

            var nScaffoldGuides = HandleNodeInvalidation(IBB.ScaffoldGuides,
                new [] { new UpdateScaffoldGuidesComponent(Director, ObjectManager) },
                nBasePlateBottomContour, nScaffoldPrimaryBorder);

            var nScaffoldSupport = HandleNodeInvalidation(IBB.ScaffoldSupport,
                new[] { new UpdateScaffoldSupportComponent(Director, ObjectManager) },
                nScaffoldPrimaryBorder, nScaffoldSecondaryBorder, nScapulaDesignReamed);

            var nScaffoldTop = HandleNodeInvalidation(IBB.ScaffoldTop,
                new[] { new UpdateScaffoldTopComponent(Director, ObjectManager) },
                nBasePlateBottomContour);

            var nScaffoldSide = HandleNodeInvalidation(IBB.ScaffoldSide,
                new[] { new UpdateScaffoldSideComponent(Director, ObjectManager) },
                nBasePlateBottomContour, nScaffoldTop, nScaffoldPrimaryBorder, nScaffoldGuides);

            HandleNodeInvalidation(IBB.ScaffoldBottom,
                new[] { new UpdateScaffoldBottomComponent(Director, ObjectManager) }, nScaffoldSupport); //Used to have nScaffoldTop, nScaffoldSide

            //Solid Wall
            var nSolidWallCurve = HandleNodeInvalidation(IBB.SolidWallCurve,
                new[] { new UpdateSolidWallCurveComponent(Director, ObjectManager) },
                nBasePlateBottomContour, nScaffoldSide);

            HandleNodeInvalidation(IBB.SolidWallWrap,
                new[] { new UpdateSolidWallWrapComponent(Director, ObjectManager) },
                nScaffoldSide, nSolidWallCurve);
        }

        public bool NotifyBuildingBlockHasChanged(params IBB[] ibbs)
        {
            if (!IsBuildingBlockNotificationEnabled)
            {
                return true;
            }

            if (Director.IsTestingMode)
            {
                RhinoApp.WriteLine("[IDS::Log] *BuildingBlocksNotification({0})", string.Join(",", ibbs));
            }

            var ibbNodes = (from ibb in ibbs where nodeDictionary.ContainsKey(ibb) select GetNode(ibb)).ToArray();
            var finder = new DescendantsNodesFinder<ExecutableNode>(IBBGraph);
            var orderedNodes = finder.CreateDescendantsExecutionSequence(ibbNodes);

            if (orderedNodes.Any())
            {
                foreach (var node in orderedNodes)
                {
                    if (Director.IsTestingMode)
                    {
                        RhinoApp.WriteLine("[IDS::Log] *DEPENDENCY UPDATE:: {0}", node.Name);
                    }

                    if (!node.Execute())
                    {
                        RhinoApp.WriteLine("[IDS::Error] ** DEPENDENCY UPDATE FAILED:: {0}", node.Name);
                        return false;
                    }
                }
            }

            return true;
        }

        //For User
        ////////////////////////////////////////////////////////////////////////////////////////////////

        public ExecutableNode AddNode(IBB ibb, params ExecutableNode[] dependencies)
        {
            return AddNode(ibb, null, dependencies);
        }

        public ExecutableNode AddNode(IBB ibb, IExecutableNodeComponent component, params ExecutableNode[] dependencies)
        {
            if (HasNode(ibb))
            {
                throw new ArgumentException("Node " + ibb.ToString() + " already exist!");
            }

            var node = new ExecutableNode(ibb.ToString(), dependencies);

            if (component != null)
            {
                node.Components.Add(component);
            }

            nodeDictionary.Add(ibb, node);
            IBBGraph.Add(node);

            //Rebuild Graph
            var sorter = new NodeTopologySort();
            IBBGraph = sorter.Sort(IBBGraph, x => x.GetDependencies<ExecutableNode>()).ToList();

            return node;
        }

        public ExecutableNode GetNode(IBB ibb)
        {
            return nodeDictionary.ContainsKey(ibb) ? nodeDictionary[ibb] : null;
        }

        public bool HasNode(params IBB[] ibbs)
        {
            return ibbs.All(ibb => nodeDictionary.ContainsKey(ibb));
        }

        public bool AddNodeDependencies(IBB ibb, params ExecutableNode[] dependencies)
        {
            var node = GetNode(ibb);
            if (node != null)
            {
                node.AddDependencyTo(dependencies);
                return true;
            }

            //Rebuild Graph
            var sorter = new NodeTopologySort();
            IBBGraph = sorter.Sort(IBBGraph, x => x.GetDependencies<ExecutableNode>()).ToList();

            return false;
        }

        public void SkipNodeExecution(IBB ibb, bool isSkip)
        {
            if (HasNode(ibb))
            {
                var node = GetNode(ibb);
                node.SkipExecution = isSkip;
            }
        }

        //It handles removal of other nodes that depends on this
        public void DeleteNode(IBB ibb)
        {
            var node = GetNode(ibb);

            var finder = new DescendantsNodesFinder<ExecutableNode>(IBBGraph);
            var descendants = finder.Find(node);

            foreach (var n in descendants)
            {
                n.RemoveDependencyFrom(node);
            }

            IBBGraph.Remove(node);
            nodeDictionary.Remove(ibb);
            node = null;

            //Rebuild Graph
            var sorter = new NodeTopologySort();
            IBBGraph = sorter.Sort(IBBGraph, x => x.GetDependencies<ExecutableNode>()).ToList();
        }

    }
}
