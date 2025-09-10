using IDS.CMF.DataModel;
using IDS.CMF.RhinoFree.Utilities;
using IDS.CMF.V2.DataModel;
using IDS.CMF.V2.Utilities;
using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using IDS.Interface.Implant;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace IDS.Testing.UnitTests
{
    public class DotPastilleRhinoFree : DotPastille
    {
        public new IPoint3D Location
        {
            get { return location; }
            set { location = value; }
        }
    }

    [TestClass]
    public class ImplantCreationUtilitiesTests
    {
        [TestMethod]
        public void TestFindClusterHubPoints()
        {
             var p1 = new DotPastilleRhinoFree
             {
                 Location = new IDSPoint3D(0,0,0),
                 Direction = IDSVector3D.ZAxis,
             };
            var c2 = new DotControlPoint
            {
                Location = new IDSPoint3D(0, 1, 0),
                Direction = IDSVector3D.ZAxis,
            };
            var c3 = new DotControlPoint
            {
                Location = new IDSPoint3D(0, 2, 0),
                Direction = IDSVector3D.ZAxis,
            };
            var c4 = new DotControlPoint
            {
                Location = new IDSPoint3D(0, 3, 0),
                Direction = IDSVector3D.ZAxis,
            };
            var c5 = new DotControlPoint
            {
                Location = new IDSPoint3D(0, 4, 0),
                Direction = IDSVector3D.ZAxis,
            };
            var c6 = new DotControlPoint
            {
                Location = new IDSPoint3D(0, 5, 0),
                Direction = IDSVector3D.ZAxis,
            };
            var c7 = new DotControlPoint
            {
                Location = new IDSPoint3D(0, 6, 0),
                Direction = IDSVector3D.ZAxis,
            };
            var p8 = new DotPastilleRhinoFree
            {
                Location = new IDSPoint3D(0, 7, 0),
                Direction = IDSVector3D.ZAxis,
            };
            var c9 = new DotControlPoint
            {
                Location = new IDSPoint3D(1, 3, 0),
                Direction = IDSVector3D.ZAxis,
            };
            var p10 = new DotPastilleRhinoFree
            {
                Location = new IDSPoint3D(2, 3, 0),
                Direction = IDSVector3D.ZAxis,
            };
            var p11 = new DotPastilleRhinoFree
            {
                Location = new IDSPoint3D(3, 3, 0),
                Direction = IDSVector3D.ZAxis,
            };

            var connections = new List<IConnection>()
            {
                new ConnectionPlate{A = p1, B = c2},
                new ConnectionPlate{A = c2, B = c3},
                new ConnectionPlate{A = c3, B = c4},
                new ConnectionPlate{A = c4, B = c5},
                new ConnectionPlate{A = c5, B = c6},
                new ConnectionPlate{A = c6, B = c7},
                new ConnectionPlate{A = c7, B = p8},
                new ConnectionPlate{A = c4, B = c9},
                new ConnectionPlate{A = c9, B = p10},
                new ConnectionPlate{A = p10, B = p11},
            };

            var hubs = ConnectionUtilities.FindClusterHubPoints(connections);

            Assert.IsTrue(hubs.Contains(p1));
            Assert.IsTrue(hubs.Contains(p8));
            Assert.IsTrue(hubs.Contains(p10));
            Assert.IsTrue(hubs.Contains(p11));
            Assert.IsTrue(hubs.Contains(c4));
        }

        [TestMethod]
        public void TestCreateDotCluster()
        {
            var p1 = new DotPastilleRhinoFree
            {
                Location = new IDSPoint3D(0, 0, 0),
                Direction = IDSVector3D.ZAxis,
            };
            var c2 = new DotControlPoint
            {
                Location = new IDSPoint3D(0, 1, 0),
                Direction = IDSVector3D.ZAxis,
            };
            var c3 = new DotControlPoint
            {
                Location = new IDSPoint3D(0, 2, 0),
                Direction = IDSVector3D.ZAxis,
            };
            var c4 = new DotControlPoint 
            { 
                Location = new IDSPoint3D(0, 3, 0), 
                Direction = IDSVector3D.ZAxis,
            };
            var c5 = new DotControlPoint
            {
                Location = new IDSPoint3D(0, 4, 0),
                Direction = IDSVector3D.ZAxis,
            };
            var c6 = new DotControlPoint
            {
                Location = new IDSPoint3D(0, 5, 0),
                Direction = IDSVector3D.ZAxis,
            };
            var c7 = new DotControlPoint
            {
                Location = new IDSPoint3D(0, 6, 0),
                Direction = IDSVector3D.ZAxis,
            };
            var p8 = new DotPastilleRhinoFree
            {
                Location = new IDSPoint3D(0, 7, 0),
                Direction = IDSVector3D.ZAxis,
            };
            var c9 = new DotControlPoint
            {
                Location = new IDSPoint3D(1, 3, 0),
                Direction = IDSVector3D.ZAxis,
            };
            var p10 = new DotPastilleRhinoFree
            {
                Location = new IDSPoint3D(2, 3, 0),
                Direction = IDSVector3D.ZAxis,
            };
            var p11 = new DotPastilleRhinoFree
            {
                Location = new IDSPoint3D(3, 3, 0),
                Direction = IDSVector3D.ZAxis,
            };

            var connections = new List<IConnection>()
            {
                new ConnectionPlate{A = p1, B = c2},
                new ConnectionPlate{A = c2, B = c3},
                new ConnectionPlate{A = c3, B = c4},
                new ConnectionPlate{A = c4, B = c5},
                new ConnectionPlate{A = c5, B = c6},
                new ConnectionPlate{A = c6, B = c7},
                new ConnectionPlate{A = c7, B = p8},
                new ConnectionPlate{A = c4, B = c9},
                new ConnectionPlate{A = c9, B = p10},
                new ConnectionPlate{A = p10, B = p11},
            };

            var clusters = ConnectionUtilities.CreateDotCluster(connections);

            Assert.IsTrue(CheckList(clusters, new List<IDot> { p1, c2, c3, c4 }));
            Assert.IsTrue(CheckList(clusters, new List<IDot> { c4, c5, c6, c7, p8 }));
            Assert.IsTrue(CheckList(clusters, new List<IDot> { c4, c9, p10 }));
            Assert.IsTrue(CheckList(clusters, new List<IDot> { p10, p11 }));
        }

        private bool CheckList(List<List<IDot>> clusters, List<IDot> reference)
        {
            foreach (var c in clusters)
            {
                if (c.Count != reference.Count)
                {
                    continue;
                }

                var success = true;

                for (var i = 0; i < c.Count; i++)
                {
                    success &= c[i] == reference[i];
                }

                if (success)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
