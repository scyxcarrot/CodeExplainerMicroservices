using IDS.CMF.DataModel;
using IDS.CMF.V2.DataModel;
using IDS.CMF.V2.Utilities;
using IDS.Core.V2.Geometries;
using IDS.Interface.Implant;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class IDotUtilitiesTests
    {
        [TestMethod]
        public void Create_DotPlastille_Cluster_Test()
        {
            //arrange
            /*   Capital letter is DotPastille
             *   Small letter is DotControlPoint
             *
             *    A - B - c - D - E
             */
            
            var direction = IDSVector3D.ZAxis;
            var a = new DotPastille()
            {
                Location = new IDSPoint3D() { X = 0, Y = 0, Z = 0 },
                Direction = IDSVector3D.ZAxis,
            };
            var b = new DotPastille()
            {
                Location = new IDSPoint3D() { X = 0, Y = 0, Z = 1 },
                Direction = IDSVector3D.ZAxis,
            };
            var c = new DotControlPoint()
            {
                Location = new IDSPoint3D() { X = 0, Y = 1, Z = 0 },
                Direction = IDSVector3D.ZAxis,
            };
            var d = new DotPastille()
            {
                Location = new IDSPoint3D() { X = 0, Y = 1, Z = 1 },
                Direction = IDSVector3D.ZAxis,
            };
            var e = new DotPastille()
            {
                Location = new IDSPoint3D() { X = 1, Y = 0, Z = 0 },
                Direction = IDSVector3D.ZAxis,
            };

            var con1 = new ConnectionPlate() { A = a, B = b };
            var con2 = new ConnectionPlate() { A = b, B = c };
            var con3 = new ConnectionPlate() { A = c, B = d };
            var con4 = new ConnectionPlate() { A = d, B = e };

            var connections = new List<IConnection>()
            {
                con1,
                con2,
                con3,
                con4
            };

            //act
            var actualClusters = ConnectionUtilities.CreateDotCluster(connections);

            //assert
            var expectedClusters = new List<List<IDot>>()
            {
                new List<IDot>(){ a, b},
                new List<IDot>(){ b, c, d},
                new List<IDot>(){ d, e}
            };

            Assert.IsTrue(Comparison.AreNestedListsEquivalent(expectedClusters, actualClusters));
        }

        [TestMethod]
        public void Create_DotPlastille_Interconnected_Cluster_Test()
        {
            //arrange
            /*   Capital letter is DotPastille
             *   Small letter is DotControlPoint
             *
             *    A - B - c - D - E
             *        |       |
             *        F - g - H
             *            
             */
            var a = new DotPastille()
            {
                Location = new IDSPoint3D() { X = 0, Y = 0, Z = 0 },
                Direction = IDSVector3D.ZAxis,
            };
            var b = new DotPastille()
            {
                Location = new IDSPoint3D() { X = 0, Y = 0, Z = 1 },
                Direction = IDSVector3D.ZAxis,
            };
            var c = new DotControlPoint() 
            { 
                Location = new IDSPoint3D() { X = 0, Y = 1, Z = 0 },
                Direction = IDSVector3D.ZAxis,
            };
            var d = new DotPastille()
            {
                Location = new IDSPoint3D() { X = 0, Y = 1, Z = 1 },
                Direction = IDSVector3D.ZAxis,
            };
            var e = new DotPastille()
            {
                Location = new IDSPoint3D() { X = 1, Y = 0, Z = 0 },
                Direction = IDSVector3D.ZAxis,
            };
            var f = new DotPastille()
            {
                Location = new IDSPoint3D() { X = 1, Y = 0, Z = 1 },
                Direction = IDSVector3D.ZAxis,
            };
            var g = new DotControlPoint()
            {
                Location = new IDSPoint3D() { X = 1, Y = 1, Z = 0 },
                Direction = IDSVector3D.ZAxis,
            };
            var h = new DotPastille()
            {
                Location = new IDSPoint3D() { X = 1, Y = 1, Z = 1 },
                Direction = IDSVector3D.ZAxis,
            };

            var con1 = new ConnectionPlate() { A = a, B = b };
            var con2 = new ConnectionPlate() { A = b, B = c };
            var con3 = new ConnectionPlate() { A = c, B = d };
            var con4 = new ConnectionPlate() { A = d, B = e };
            var con5 = new ConnectionPlate() { A = b, B = f };
            var con6 = new ConnectionPlate() { A = f, B = g };
            var con7 = new ConnectionPlate() { A = g, B = h };
            var con8 = new ConnectionPlate() { A = h, B = d };

            var connections = new List<IConnection>()
            {
                con1,
                con2,
                con3,
                con4,
                con5,
                con6,
                con7,
                con8
            };

            //act
            var actualClusters = ConnectionUtilities.CreateDotCluster(connections);

            //assert
            var expectedClusters = new List<List<IDot>>()
            {
                new List<IDot>(){ a, b},
                new List<IDot>(){ b, c, d},
                new List<IDot>(){ d, e},
                new List<IDot>(){ b, f},
                new List<IDot>(){ f, g, h},
                new List<IDot>(){ h, d}
            };

            Assert.IsTrue(Comparison.AreNestedListsEquivalent(expectedClusters, actualClusters));
        }
    }
}
