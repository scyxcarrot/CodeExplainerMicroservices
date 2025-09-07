using IDS.Core.Graph;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Testing.UnitTests
{
    /// <summary>
    /// Summary description for IBBDependencyTests
    /// </summary>
    [TestClass]
    public class IBBDependencyTests
    {
        //Utility Functions
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// 
        private static void TestGraphGetChildNodes<TNodeType>(List<TNodeType> graph, TNodeType startNode, string expected) where TNodeType:NodeBase
        {
            if (!graph.Any()) return;
            var finder = new DescendantsNodesFinder<TNodeType>(graph);
            var descendantNodes = finder.Find(startNode);

            Assert.IsNotNull(descendantNodes);

            var sequence = "";
            foreach (var n in descendantNodes)
            {
                sequence += n.Name;
            }

            Assert.AreEqual(expected, sequence);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //Base Node Graph Tests
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //Base Node Graph Generators
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private static List<NodeBase> GenerateTestGraphPerfect1()
        {
            var A = new NodeBase("A");
            var B = new NodeBase("B");
            var C = new NodeBase("C", A, B);
            var D = new NodeBase("D", A);
            var E = new NodeBase("E", C);
            var F = new NodeBase("F", B, E);
            var G = new NodeBase("G", F);

            var graph = new List<NodeBase>()
            {
                A,B,C,D,E,F,G
            };

            return graph;
        }

        private static List<NodeBase> GenerateTestGraphPerfect2()
        {
            var D = new NodeBase("D");
            var A = new NodeBase("A", D);
            var B = new NodeBase("B", A, D);
            var C = new NodeBase("C", B, D);

            var graph = new List<NodeBase>()
            {
                A,B,C,D
            };

            return graph;
        }

        private static List<NodeBase> GenerateTestGraphCircular()
        {
            var A = new NodeBase("A");
            var B = new NodeBase("B");
            var C = new NodeBase("C");

            A.AddDependencyTo(B);
            B.AddDependencyTo(C);
            C.AddDependencyTo(A);

            var graph = new List<NodeBase>()
            {
                A,B,C
            };

            return graph;
        }

        //Test Node Topology Sort
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private static void TestTopologySort<TNodeType>(List<TNodeType> graph, string expected) where TNodeType : NodeBase
        {
            var sorter = new NodeTopologySort();
            var sortedNodes = sorter.Sort(graph, x => x.GetDependencies<TNodeType>());

            Assert.IsNotNull(sortedNodes);

            var sequence = "";

            foreach (var n in sortedNodes)
            {
                sequence += n.Name;
            }

            Assert.AreEqual(expected, sequence);
        }

        [TestMethod]
        public void TestTopologySort1()
        {
            var testGraph = GenerateTestGraphPerfect1();
            TestTopologySort(testGraph, "ABCDEFG");
        }

        [TestMethod]
        public void TestTopologySort2()
        {
            var testGraph = GenerateTestGraphPerfect2();
            TestTopologySort(testGraph, "DABC");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestTopologySortCyclicException()
        {
            var A = new NodeBase("A");
            var B = new NodeBase("B");
            var C = new NodeBase("C");

            A.AddDependencyTo(C);
            B.AddDependencyTo(A);
            C.AddDependencyTo(B);

            var testGraph = new List<NodeBase>() {A,B,C};

            var sorter = new NodeTopologySort();

            sorter.Sort(testGraph, x => x.GetDependencies<NodeBase>());
        }

        //Test Find Node Decendants
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        [TestMethod]
        public void TestGraphGetChildNodes1()
        {
            var testGraph = GenerateTestGraphPerfect1();
            TestGraphGetChildNodes(testGraph, testGraph[2], "EFG");
        }

        [TestMethod]
        public void TestGraphGetChildNodes2()
        {
            var testGraph = GenerateTestGraphPerfect2();
            TestGraphGetChildNodes(testGraph, testGraph[0], "BC");
        }

        [TestMethod]
        public void TestGraphGetChildNodes3()
        {
            var testGraph = GenerateTestGraphPerfect2();
            TestGraphGetChildNodes(testGraph, testGraph[3], "ABC"); //It should not be ABCBCC
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestGraphGetChildNodesCircular()
        {
            var testGraph = GenerateTestGraphCircular();

            var finder = new DescendantsNodesFinder<NodeBase>(testGraph);
            finder.Find(testGraph[0]); //A

        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //Executable Node Test
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //Stubs
        private class FunctionNodeStub : IExecutableNodeComponent
        {
            public string Name { get; private set; }
            public bool Executed { get; private set; }

            public FunctionNodeStub(string Name)
            {
                Executed = false;
                this.Name = Name;
            }

            public bool Execute()
            {
                Executed = true;
                return true;
            }
        }
        
        //Graph Generators
        private static List<ExecutableNode> GenerateTestExecutableNodeGraph1()
        {
            var fStub1 = new FunctionNodeStub("F1");
            var fStub2 = new FunctionNodeStub("F2");
            var fStub3 = new FunctionNodeStub("F3");
            var fStub4 = new FunctionNodeStub("F4");

            var fNode1 = new ExecutableNode("D", new[] { fStub1 });
            var fNode2 = new ExecutableNode("A", new[] { fStub2 }, fNode1);
            var fNode3 = new ExecutableNode("B", new[] { fStub3 }, fNode1, fNode2);
            var fNode4 = new ExecutableNode("C", new[] { fStub4 }, fNode1, fNode3);

            var graph = new List<ExecutableNode>()
            {
                fNode1, fNode2, fNode3, fNode4
            };

            return graph;
        }

        private static List<ExecutableNode> GenerateTestExecutableNodeGraph2()
        {
            var fStub1 = new FunctionNodeStub("F1");
            var fStub2 = new FunctionNodeStub("F2");
            var fStub3 = new FunctionNodeStub("F3");
            var fStub4 = new FunctionNodeStub("F4");
            var fStub5 = new FunctionNodeStub("F5");
            var fStub6 = new FunctionNodeStub("F6");
            var fStub7 = new FunctionNodeStub("F7");

            var fNodeA = new ExecutableNode("A", new[] { fStub2 });
            var fNodeB = new ExecutableNode("B", new[] { fStub3 });
            var fNodeD = new ExecutableNode("D", new[] { fStub1 }, fNodeA);
            var fNodeC = new ExecutableNode("C", new[] { fStub4 }, fNodeA, fNodeB);
            var fNodeE = new ExecutableNode("E", new[] { fStub5 }, fNodeC);
            var fNodeF = new ExecutableNode("F", new[] { fStub6 }, fNodeB, fNodeE);
            var fNodeG = new ExecutableNode("G", new[] { fStub7 }, fNodeF);

            var graph = new List<ExecutableNode>()
            {
                fNodeA, fNodeB, fNodeC, fNodeD, fNodeE, fNodeF, fNodeG
            };

            return graph;
        }

        //Test Methods
        [TestMethod]
        public void TestExecutableNode1()
        {
            var testGraph = GenerateTestExecutableNodeGraph1();

            var sorter = new NodeTopologySort();
            var sortedNodes = sorter.Sort(testGraph, x => x.GetDependencies<ExecutableNode>());

            Assert.IsNotNull(sortedNodes);

            var sequence = "";

            foreach (var n in sortedNodes)
            {
                sequence += n.Name;
                n.Execute();
            }

            Assert.AreEqual("DABC", sequence);

            foreach (var n in sortedNodes)
            {
                Assert.IsTrue(n.Components.All(x => (x as FunctionNodeStub).Executed));
            }
        }

        [TestMethod]
        public void TestGraphGetChildExecutableNodes()
        {
            var testGraph = GenerateTestExecutableNodeGraph2();
            TestGraphGetChildNodes(testGraph, testGraph[1], "CEFG");
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //Executable Node Test: Dynamic Graph Changes
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        [TestMethod]
        public void TestGraphFindChildWithDynamicNodeChangesUnordered()
        {
            //NOTE: It is unordered so expected sequence can
            //be reversed depending on element sequence in graph!
            var graph = new List<ExecutableNode>();

            var fNodeA = new ExecutableNode("A", new[] { new FunctionNodeStub("FA") });
            graph.Add(fNodeA);

            var fNodeB = new ExecutableNode("B", new[] { new FunctionNodeStub("FB") });
            graph.Add(fNodeB);

            var fNodeC = new ExecutableNode("C", new[] { new FunctionNodeStub("FC") });
            graph.Add(fNodeC);

            TestGraphGetChildNodes(graph, fNodeA, "");

            fNodeC.AddDependencyTo(fNodeA);

            TestGraphGetChildNodes(graph, fNodeA, "C");

            fNodeB.AddDependencyTo(fNodeA,fNodeC);

            TestGraphGetChildNodes(graph, fNodeA, "BC");
            TestGraphGetChildNodes(graph, fNodeC, "B");

            var fNodeD = new ExecutableNode("D", new[] { new FunctionNodeStub("FD") });
            graph.Add(fNodeD);

            fNodeD.AddDependencyTo(fNodeC,fNodeA);
            TestGraphGetChildNodes(graph, fNodeA, "BCD");
            TestGraphGetChildNodes(graph, fNodeC, "BD");
            TestGraphGetChildNodes(graph, fNodeB, "");
            TestGraphGetChildNodes(graph, fNodeD, "");
        }

        [TestMethod]
        public void TestGraphNodeDependencyRemoval()
        {
            var nodeA = new ExecutableNode("A");
            var nodeB = new ExecutableNode("B");
            var nodeC = new ExecutableNode("C");
            var nodeD = new ExecutableNode("D");

            nodeB.AddDependencyTo(nodeA, nodeC);
            nodeC.AddDependencyTo(nodeA);
            nodeD.AddDependencyTo(nodeB, nodeC);

            var graph = new List<ExecutableNode>() { nodeD, nodeC, nodeA, nodeB };

            var sorter = new NodeTopologySort();
            graph = sorter.Sort(graph, x => x.GetDependencies<ExecutableNode>()).ToList();
            TestTopologySort(graph, "ACBD");

            nodeB.RemoveDependencyFrom(nodeC);

            graph = new List<ExecutableNode>() { nodeD , nodeC, nodeA , nodeB };
            graph = sorter.Sort(graph, x => x.GetDependencies<ExecutableNode>()).ToList();
            TestTopologySort(graph, "ABCD");
        }

        [TestMethod]
        public void TestGraphFindChildWithDynamicNodeChangesOrdered()
        {
            var graph = new List<ExecutableNode>();

            var fNodeA = new ExecutableNode("A", new[] { new FunctionNodeStub("FA") });
            graph.Add(fNodeA);

            var fNodeB = new ExecutableNode("B", new[] { new FunctionNodeStub("FB") });
            graph.Add(fNodeB);

            var fNodeC = new ExecutableNode("C", new[] { new FunctionNodeStub("FC") });
            graph.Add(fNodeC);

            TestGraphGetChildNodes(graph, fNodeA, "");

            fNodeC.AddDependencyTo(fNodeA);

            var sorter = new NodeTopologySort();
            graph = sorter.Sort(graph, x => x.GetDependencies<ExecutableNode>()).ToList();
            TestGraphGetChildNodes(graph.ToList(), fNodeA, "C");

            fNodeB.AddDependencyTo(fNodeA, fNodeC);
            graph = sorter.Sort(graph, x => x.GetDependencies<ExecutableNode>()).ToList();

            TestGraphGetChildNodes(graph, fNodeA, "CB");
            TestGraphGetChildNodes(graph, fNodeC, "B");

            var fNodeD = new ExecutableNode("D", new[] { new FunctionNodeStub("FD") });
            graph.Add(fNodeD);

            fNodeD.AddDependencyTo(fNodeC, fNodeA);
            graph = sorter.Sort(graph, x => x.GetDependencies<ExecutableNode>()).ToList();

            TestGraphGetChildNodes(graph, fNodeA, "CBD");
            TestGraphGetChildNodes(graph, fNodeC, "BD");
            TestGraphGetChildNodes(graph, fNodeB, "");
            TestGraphGetChildNodes(graph, fNodeD, "");
        }

        private static void TestGleniusGraphCreateDescendantsExecutionSequenceHelper(List<ExecutableNode> graph, string expected, params ExecutableNode[] startingNodes)
        {
            var finder = new DescendantsNodesFinder<ExecutableNode>(graph);
            var result = finder.CreateDescendantsExecutionSequence(startingNodes);

            var sequence = "";
            foreach (var n in result)
            {
                sequence += n.Name;
            }

            Assert.AreEqual(expected, sequence);
        }

        [TestMethod]
        public void TestGleniusGraphCreateDescendantsExecutionSequence()
        {
            var fNodeA = new ExecutableNode("A", new[] { new FunctionNodeStub("FA") });
            var fNodeB = new ExecutableNode("B", new[] { new FunctionNodeStub("FB") });
            var fNodeC = new ExecutableNode("C", new[] { new FunctionNodeStub("FC") });
            var fNodeD = new ExecutableNode("D", new[] { new FunctionNodeStub("FD") });
            var fNodeE = new ExecutableNode("E", new[] { new FunctionNodeStub("FE") });

            fNodeB.AddDependencyTo(fNodeA);
            fNodeC.AddDependencyTo(fNodeA, fNodeB, fNodeD);
            fNodeD.AddDependencyTo(fNodeA, fNodeB);
            fNodeE.AddDependencyTo(fNodeD);

            var graph = new List<ExecutableNode>() {fNodeA, fNodeB, fNodeC, fNodeD, fNodeE};

            TestGleniusGraphCreateDescendantsExecutionSequenceHelper(graph, "BDCE", fNodeA);
            TestGleniusGraphCreateDescendantsExecutionSequenceHelper(graph, "CE", fNodeD);
            TestGleniusGraphCreateDescendantsExecutionSequenceHelper(graph, "DCE", fNodeA, fNodeB);
            TestGleniusGraphCreateDescendantsExecutionSequenceHelper(graph, "BCE", fNodeA, fNodeD);
        }

        [TestMethod]
        public void TestGleniusGraphCreateDescendantsExecutionSequence2()
        {
            var A = new ExecutableNode("A");
            var B = new ExecutableNode("B", A);
            var C = new ExecutableNode("C", B);
            var D = new ExecutableNode("D", B);
            var E = new ExecutableNode("E", B);
            var F = new ExecutableNode("F", E);

            var graph = new List<ExecutableNode>()
            {
                A,B,C,D,E,F
            };


            TestGleniusGraphCreateDescendantsExecutionSequenceHelper(graph, "BCDEF", A);
            TestGleniusGraphCreateDescendantsExecutionSequenceHelper(graph, "CDEF", B);
        }
    }
}
