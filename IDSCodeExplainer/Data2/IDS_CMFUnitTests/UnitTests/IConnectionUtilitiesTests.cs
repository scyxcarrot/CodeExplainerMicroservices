using IDS.CMF.DataModel;
using IDS.CMF.RhinoFree.Utilities;
using IDS.CMF.V2.DataModel;
using IDS.Core.V2.Geometries;
using IDS.Interface.Implant;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class IConnectionUtilitiesTests
    {
        [TestMethod]
        public void Get_Connection_In_Between_2_DotPastille_Cluster_Test()
        {
            //arrange
            /*   Capital letter is DotPastille
             *   Small letter is DotControlPoint
             *
             *    A - B - c - D - E
             *        |       |
             *        f - g - h
             *            
             */
            var a = new DotPastille() {Location = new IDSPoint3D() {X = 0, Y = 0, Z = 0}};
            var b = new DotPastille() {Location = new IDSPoint3D() {X = 0, Y = 0, Z = 1}};
            var c = new DotControlPoint() {Location = new IDSPoint3D() {X = 0, Y = 1, Z = 0}};
            var d = new DotPastille() {Location = new IDSPoint3D() {X = 0, Y = 1, Z = 1}};
            var e = new DotControlPoint() {Location = new IDSPoint3D() {X = 1, Y = 0, Z = 0}};
            var f = new DotControlPoint() {Location = new IDSPoint3D() {X = 1, Y = 0, Z = 1}};
            var g = new DotControlPoint() {Location = new IDSPoint3D() {X = 1, Y = 1, Z = 0}};
            var h = new DotControlPoint() {Location = new IDSPoint3D() {X = 1, Y = 1, Z = 1}};

            var con1 = new ConnectionPlate() {A = a, B = b};
            var con2 = new ConnectionPlate() {A = b, B = c};
            var con3 = new ConnectionPlate() {A = c, B = d};
            var con4 = new ConnectionPlate() {A = d, B = e};
            var con5 = new ConnectionPlate() {A = b, B = f};
            var con6 = new ConnectionPlate() {A = f, B = g};
            var con7 = new ConnectionPlate() {A = g, B = h};
            var con8 = new ConnectionPlate() {A = h, B = d};

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
            var actualConnections =
                ImplantCreationUtilitiesRhinoFree.FindConnectionsBelongToTwoDotPastille(connections, b, d);

            //assert
            var expectedConnections = new List<List<IConnection>>()
            {
                new List<IConnection>() {con2, con3},
                new List<IConnection>() {con5, con6, con7, con8}
            };

            Assert.IsTrue(Comparison.AreNestedListsEquivalent(expectedConnections, actualConnections));
        }

        [TestMethod]
        public void Check_Connection_In_Same_Properties()
        {
            //arrange
            var a = new DotPastille() { Location = new IDSPoint3D() { X = 0, Y = 0, Z = 0 } };
            var b = new DotPastille() { Location = new IDSPoint3D() { X = 0, Y = 0, Z = 1 } };
            var c = new DotControlPoint() { Location = new IDSPoint3D() { X = 0, Y = 1, Z = 0 } };

            var con1 = new ConnectionPlate() { A = a, B = b, Thickness = 1.2, Width = 3.3 };
            var con2 = new ConnectionPlate() { A = b, B = c, Thickness = 1.2, Width = 3.3 };

            var connections = new List<IConnection>()
            {
                con1,
                con2
            };

            //act
            //assert
            Assert.IsTrue(ImplantCreationUtilitiesRhinoFree.CheckConnectionsPropertiesIsEqual(connections));
        }

        [TestMethod]
        public void Check_Connection_In_Diff_Thickness()
        {
            //arrange
            var a = new DotPastille() { Location = new IDSPoint3D() { X = 0, Y = 0, Z = 0 } };
            var b = new DotPastille() { Location = new IDSPoint3D() { X = 0, Y = 0, Z = 1 } };
            var c = new DotControlPoint() { Location = new IDSPoint3D() { X = 0, Y = 1, Z = 0 } };

            var con1 = new ConnectionPlate() { A = a, B = b, Thickness = 1.2, Width = 3.3 };
            var con2 = new ConnectionPlate() { A = b, B = c, Thickness = 2.4, Width = 3.3 };

            var connections = new List<IConnection>()
            {
                con1,
                con2
            };

            //act
            //assert
            Assert.IsFalse(ImplantCreationUtilitiesRhinoFree.CheckConnectionsPropertiesIsEqual(connections));
        }

        [TestMethod]
        public void Check_Connection_In_Diff_Width()
        {
            //arrange
            var a = new DotPastille() { Location = new IDSPoint3D() { X = 0, Y = 0, Z = 0 } };
            var b = new DotPastille() { Location = new IDSPoint3D() { X = 0, Y = 0, Z = 1 } };
            var c = new DotControlPoint() { Location = new IDSPoint3D() { X = 0, Y = 1, Z = 0 } };

            var con1 = new ConnectionPlate() { A = a, B = b, Thickness = 1.2, Width = 3.3 };
            var con2 = new ConnectionPlate() { A = b, B = c, Thickness = 1.2, Width = 4.2 };

            var connections = new List<IConnection>()
            {
                con1,
                con2
            };

            //act
            //assert
            Assert.IsFalse(ImplantCreationUtilitiesRhinoFree.CheckConnectionsPropertiesIsEqual(connections));
        }
    }
}
