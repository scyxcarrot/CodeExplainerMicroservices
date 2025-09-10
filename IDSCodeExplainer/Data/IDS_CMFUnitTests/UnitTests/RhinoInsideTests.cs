using IDS.CMF.DataModel;
using IDS.CMF.Utilities;
using IDS.CMF.V2.DataModel;
using IDS.Core.V2.Geometries;
using IDS.Interface.Implant;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Geometry;
using System.Collections.Generic;

namespace IDS.Testing.UnitTests
{
#if (Rhino7Installed)
    [TestClass]
    public class RhinoInsideTests
    {
        [TestMethod]
        public void ImplantCreationUtilitiesUnitTest()
        {
            var connections = new List<IConnection>()
            {
                new ConnectionPlate()
                {
                    A = new DotPastille()
                    {
                        Direction = new IDSVector3D(1, 1, 1),
                        Location = new IDSPoint3D(1, 2, 3)
                    },
                    B = new DotPastille()
                    {
                        Direction = new IDSVector3D(1, 1, 1),
                        Location = new IDSPoint3D(2, 3, 4)
                    },
                    Thickness = 2,
                    Width = 2
                },
                new ConnectionPlate()
                {
                    A = new DotPastille()
                    {
                        Direction = new IDSVector3D(1, 1, 1),
                        Location = new IDSPoint3D(200, 300, 400)
                    },
                    B = new DotPastille()
                    {
                        Direction = new IDSVector3D(1, 1, 1),
                        Location = new IDSPoint3D(201, 301, 401)
                    },
                    Thickness = 2,
                    Width = 2
                }
            };

            var connection = ImplantCreationUtilities.FindClosestConnection(connections,
                new Point3d()
                {
                    X = 1,
                    Y = 2,
                    Z = 3
                });

            Assert.IsTrue(connection == connections[0], "The closest connection is not match");
        }

        [TestMethod]
        public void CreateCylinderMesh_Returns_Solid_Mesh()
        {
            //Bug 1115439: C: Deformed pastille generated due to incorrect smarties result
            var mesh = ImplantPastilleCreationUtilities.CreateCylinderMesh(Plane.WorldXY, 2.5, 6.0);            

            Assert.IsTrue(mesh.IsSolid, "CylinderMesh created is not solid");
        }
    }
#endif
}
