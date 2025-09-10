using IDS.CMF.CasePreferences;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Relations;
using IDS.Core.Graph;
using Rhino;
using Rhino.Commands;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Graph
{
    public class TargetNode
    {
        public List<Guid> Guids { get; set; }
        public IBB IBB { get; set; }
    }

    public class CMFGraph
    {
        public CMFImplantDirector Director { get; }
        public CMFObjectManager ObjectManager { get; }

        //IBBGraph and nodeDictionary should always be inSync. node dictionary is to get the node by IBB enum as the key for ease of access.
        private List<CMFExecutableNode> IBBGraph { get; set; }
        private readonly Dictionary<IBB, CMFExecutableNode> nodeDictionary;

        public bool IsUndoRedoSubscribed { get; private set; }

        public bool IsBuildingBlockNotificationEnabled { get; set; } = true;

        public ICaseData CasePreference { get; private set; }

        public CMFGraph(CMFImplantDirector director, ICaseData casePreference)
        {
            CasePreference = casePreference;
            IsUndoRedoSubscribed = false;
            Director = director;
            ObjectManager = new CMFObjectManager(Director);
            nodeDictionary = new Dictionary<IBB, CMFExecutableNode>();
            IBBGraph = new List<CMFExecutableNode>();
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
            //TODO: Remove graph generation for each command as it no longer valid and overridden by this.
            //TODO: Also unit test extension for actual dependencies and scenario testing.
            InvalidateGraph();
        }

        public List<string> GetGraphIBBInfo()
        {
            return nodeDictionary.Select(x => x.Key.ToString()).ToList();
        }

        //Returns Null if should not exist
        private CMFExecutableNode HandleNodeInvalidation(IBB ibb, params CMFExecutableNode[] dependencies)
        {
            return HandleNodeInvalidation(ibb, null, null, dependencies);
        }

        //Returns Null if should not exist
        private CMFExecutableNode HandleNodeInvalidation(IBB ibb, ICaseData casePreference, params CMFExecutableNode[] dependencies)
        {
            return HandleNodeInvalidation(ibb, null, casePreference, dependencies);
        }

        //Returns Null if should not exist
        private CMFExecutableNode HandleNodeInvalidation(IBB ibb, IExecutableNodeComponent[] component, ICaseData casePreference, params CMFExecutableNode[] dependencies)
        {
            var implantComponent = new ImplantCaseComponent();

            ExtendedImplantBuildingBlock eBlock = null;
            try //TODO dirty!
            {
                if (implantComponent.IsImplantComponent(ibb))
                {
                    eBlock = implantComponent.GetImplantBuildingBlock(ibb, casePreference);
                }
                else
                {
                    var guideComponent = new GuideCaseComponent();
                    eBlock = guideComponent.GetGuideBuildingBlock(ibb, casePreference);
                }
            }
            catch (Exception ex)
            {
                // ignored
            }

            CMFExecutableNode node = null;
            if (ObjectManager.HasBuildingBlock(ibb) || (eBlock != null && ObjectManager.HasBuildingBlock(eBlock)))
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
                            if (casePreference != null)
                            {
                                if (eBlock == null)
                                {
                                    var guideComponent = new GuideCaseComponent();
                                    eBlock = guideComponent.GetGuideBuildingBlock(ibb, casePreference);
                                }

                                if (eBlock != null && ObjectManager.HasBuildingBlock(eBlock))
                                {
                                    node.AddDependencyTo(n);
                                }
                            }
                            else if (ObjectManager.HasBuildingBlock(parentIbb))
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

            var nImplantSupportMesh = HandleNodeInvalidation(IBB.ImplantSupport, CasePreference);
            var nScrew = HandleNodeInvalidation(IBB.Screw, new[] { new DeleteImplantScrewComponent(Director) },
                CasePreference, nImplantSupportMesh);
            var nConnection = HandleNodeInvalidation(IBB.Connection, new[] { new DeleteImplantConnectionComponent(Director) },
                CasePreference, nImplantSupportMesh, nScrew);
            var nLandmark = HandleNodeInvalidation(IBB.Landmark, new[] { new DeleteLandmarkComponent(Director) }, CasePreference, nScrew);
            var nPlanning = HandleNodeInvalidation(IBB.PlanningImplant, CasePreference, nScrew);
            var nConnectionPreview = HandleNodeInvalidation(IBB.ConnectionPreview, new[] { new DeleteConnectionPreviewComponent(Director) },
                CasePreference, nPlanning, nConnection, nScrew);
            var nPastillePreview = HandleNodeInvalidation(IBB.PastillePreview, new[] { new DeletePastillePreviewComponent(Director) },
                CasePreference, nPlanning, nConnection, nScrew, nLandmark);
            HandleNodeInvalidation(IBB.ImplantPreview, new[] { new DeleteImplantPreviewComponent(Director) },
                CasePreference, nPlanning, nConnection, nScrew, nLandmark, nPastillePreview, nConnectionPreview);

            //Guide
            var nRegisteredBarrels = HandleNodeInvalidation(IBB.RegisteredBarrel,
                new[] { new DeleteGuideBarrelComponent(Director) }, CasePreference, nScrew);
            var nGuideSupportMesh = HandleNodeInvalidation(IBB.GuideSupport);
            var nGuideSurfaceWrap = HandleNodeInvalidation(IBB.GuideSurfaceWrap, nGuideSupportMesh);
            var nGuideBridge = HandleNodeInvalidation(IBB.GuideBridge, new[] { new DeleteGuideBridgeComponent(Director) }, CasePreference, nGuideSurfaceWrap);
            var nFlangeGuidingOutline = HandleNodeInvalidation(IBB.GuideFlangeGuidingOutline, nGuideSurfaceWrap);
            var nGuideFlange = HandleNodeInvalidation(IBB.GuideFlange, new[] { new DeleteGuideFlangeComponent(Director) }, CasePreference, nFlangeGuidingOutline);
            var nGuideSurface = HandleNodeInvalidation(IBB.GuideSurface, new[] { new DeleteGuideSurfaceComponent(Director) }, CasePreference, nGuideSurfaceWrap);

            var nGuideFixationScrew = HandleNodeInvalidation(IBB.GuideFixationScrew, new[] { new DeleteGuideFixationScrewComponent(Director) },
                CasePreference, nGuideSupportMesh);
            var nGuideLabelTag = HandleNodeInvalidation(IBB.GuideFixationScrewLabelTag, CasePreference, nGuideFixationScrew);
            var nGuideFixationScrewEye = HandleNodeInvalidation(IBB.GuideFixationScrewEye, CasePreference, nGuideFixationScrew);

            var nSmoothGuideBaseSurface = HandleNodeInvalidation(IBB.SmoothGuideBaseSurface, new[] { new DeleteSmoothGuideBaseSurfaceComponent(Director) }, CasePreference,
                nRegisteredBarrels, nGuideFlange, nGuideBridge, nGuideFixationScrewEye, nGuideLabelTag, nGuideSurface);

            var nTeethBlock = HandleNodeInvalidation(IBB.TeethBlock, CasePreference);
            var nGuidePreviewSmoothen = HandleNodeInvalidation(
                IBB.GuidePreviewSmoothen,
                new[] { new DeleteGuidePreviewSmoothenComponent(Director) },
                CasePreference,
                nSmoothGuideBaseSurface, nTeethBlock);
            HandleNodeInvalidation(IBB.ActualGuide, new[] { new DeleteActualGuideComponent(Director) }, CasePreference,
              nGuidePreviewSmoothen);
        }

        public bool NotifyBuildingBlockHasChanged(IBB[] ibbs, params IBB[] ibbsToSkip)
        {
            return NotifyBuildingBlockHasChanged(ibbs, new List<TargetNode>(), ibbsToSkip);
        }

        public bool NotifyBuildingBlockHasChanged(IBB[] ibbs, List<TargetNode> targetNodes, params IBB[] ibbsToSkip)
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
            var finder = new DescendantsNodesFinder<CMFExecutableNode>(IBBGraph);
            var orderedNodes = finder.CreateDescendantsExecutionSequence(ibbNodes);

            if (orderedNodes.Any())
            {
                foreach (var node in orderedNodes)
                {
                    IBB nodeIbb;
                    Enum.TryParse<IBB>(node.Name, out nodeIbb);

                    if (Director.IsTestingMode && !ibbsToSkip.Contains(nodeIbb))
                    {
                        RhinoApp.WriteLine("[IDS::Log] *DEPENDENCY UPDATE:: {0}", node.Name);
                    }

                    node.CasePreference = CasePreference;
                    var targetNode = targetNodes.FirstOrDefault(n => n.IBB == nodeIbb);
                    if (targetNode != null)
                    {
                        node.Guids = targetNode.Guids;
                    }

                    if (!ibbsToSkip.Contains(nodeIbb) && !node.Execute())
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

        public CMFExecutableNode AddNode(IBB ibb, params CMFExecutableNode[] dependencies)
        {
            return AddNode(ibb, null, dependencies);
        }

        public CMFExecutableNode AddNode(IBB ibb, IExecutableNodeComponent component, params CMFExecutableNode[] dependencies)
        {
            if (HasNode(ibb))
            {
                throw new ArgumentException("Node " + ibb.ToString() + " already exist!");
            }

            var node = new CMFExecutableNode(ibb.ToString(), dependencies);

            if (component != null)
            {
                node.Components.Add(component);
            }

            nodeDictionary.Add(ibb, node);
            IBBGraph.Add(node);

            //Rebuild Graph
            var sorter = new NodeTopologySort();
            IBBGraph = sorter.Sort(IBBGraph, x => x.GetDependencies<CMFExecutableNode>()).ToList();

            return node;
        }

        public CMFExecutableNode GetNode(IBB ibb)
        {
            return nodeDictionary.ContainsKey(ibb) ? nodeDictionary[ibb] : null;
        }

        public bool HasNode(params IBB[] ibbs)
        {
            return ibbs.All(ibb => nodeDictionary.ContainsKey(ibb));
        }

        public bool AddNodeDependencies(IBB ibb, params CMFExecutableNode[] dependencies)
        {
            var node = GetNode(ibb);
            if (node != null)
            {
                node.AddDependencyTo(dependencies);
                return true;
            }

            //Rebuild Graph
            var sorter = new NodeTopologySort();
            IBBGraph = sorter.Sort(IBBGraph, x => x.GetDependencies<CMFExecutableNode>()).ToList();

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

            var finder = new DescendantsNodesFinder<CMFExecutableNode>(IBBGraph);
            var descendants = finder.Find(node);

            foreach (var n in descendants)
            {
                n.RemoveDependencyFrom(node);
            }

            IBBGraph.Remove(node);
            nodeDictionary.Remove(ibb);

            //Rebuild Graph
            var sorter = new NodeTopologySort();
            IBBGraph = sorter.Sort(IBBGraph, x => x.GetDependencies<CMFExecutableNode>()).ToList();
        }

    }
}
