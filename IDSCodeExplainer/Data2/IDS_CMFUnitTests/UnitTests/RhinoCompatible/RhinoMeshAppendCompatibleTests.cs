using IDS.Core.Utilities;
using IDS.Core.V2.Utilities;
using IDS.Interface.Geometry;
using IDS.RhinoInterface.Converter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Geometry;
using System.Collections.Generic;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class RhinoMeshAppendCompatibleTests
    {
        private Mesh CreateBoxMesh(Point3d min, Point3d max)
        {
            var mesh = Mesh.CreateFromBox(new BoundingBox(new Point3d(1, 1, 1), new Point3d(3, 3, 3)), 1, 1, 1);
            mesh.Faces.ConvertQuadsToTriangles();
            mesh.Faces.CullDegenerateFaces();
            mesh.Compact();
            return mesh;
        }

        [TestMethod]
        public void Rhino_Mesh_Append_Test()
        {
            // Arrange
            var boxMeshA = CreateBoxMesh(new Point3d(1, 1, 1), new Point3d(3, 3, 3));
            var boxMeshB = CreateBoxMesh(new Point3d(0, 0, 0), new Point3d(2, 2, 2));
            var boxMeshC = CreateBoxMesh(new Point3d(-1, -1, -1), new Point3d(1, 1, 1));

            var expectedMesh = MeshUtilities.AppendMeshes(new List<Mesh>()
            {
                boxMeshA,
                boxMeshB,
                boxMeshC
            });

            // Act
            var actualMesh = MeshUtilitiesV2.AppendMeshes(new List<IMesh>()
            {
                RhinoMeshConverter.ToIDSMesh(boxMeshA),
                RhinoMeshConverter.ToIDSMesh(boxMeshB),
                RhinoMeshConverter.ToIDSMesh(boxMeshC)
            });

            // Assert
            MeshTestUtilities.AssertMeshAreEqual(RhinoMeshConverter.ToIDSMesh(expectedMesh), actualMesh);
        }
    }
}
