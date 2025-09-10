using IDS.Core.V2.Geometries;
using IDS.Core.V2.MTLS.Operation;
using IDS.Interface.Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMFImplantCreation.UnitTests
{
    [TestClass]
    public class IsInsideMeshTests
    {
        [TestMethod]
        public void Is_Inside_Test()
        {
            // Arrange
            var testConsole = new TestConsole();
            var sphere = Primitives.GenerateSphere(testConsole, IDSPoint3D.Zero, 3);

            // Act
            var isInside = InsideMeshDiagnostic.PointIsInsideMesh(testConsole, sphere, IDSPoint3D.Zero);

            // Assert
            Assert.IsTrue(isInside);
        }

        [TestMethod]
        public void Are_Inside_Test()
        {
            // Arrange
            var testConsole = new TestConsole();
            var sphere = Primitives.GenerateSphere(testConsole, IDSPoint3D.Zero, 3);

            // Act
            var areInside = InsideMeshDiagnostic.PointsAreInsideMesh(testConsole, sphere, 
                new List<IPoint3D>(){ IDSPoint3D.Zero , IDSPoint3D.Zero });

            // Assert
            Assert.IsTrue(areInside.All(i => i));
        }

        [TestMethod]
        public void Is_Outside_Test()
        {
            // Arrange
            var testConsole = new TestConsole();
            var sphere = Primitives.GenerateSphere(testConsole, IDSPoint3D.Zero, 3);

            // Act
            var isInside = InsideMeshDiagnostic.PointIsInsideMesh(testConsole, sphere, new IDSPoint3D(3,3,3));

            // Assert
            Assert.IsFalse(isInside);
        }

        [TestMethod]
        public void Are_Outside_Test()
        {
            // Arrange
            var testConsole = new TestConsole();
            var sphere = Primitives.GenerateSphere(testConsole, IDSPoint3D.Zero, 3);

            // Act
            var areInside = InsideMeshDiagnostic.PointsAreInsideMesh(testConsole, sphere,
                new List<IPoint3D>()
                {
                    new IDSPoint3D(3,3,3),
                    new IDSPoint3D(3,3,3)

                });

            // Assert
            Assert.IsFalse(areInside.Any(i => i));
        }
    }
}
