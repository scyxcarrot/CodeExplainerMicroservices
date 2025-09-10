using IDS.CMFImplantCreation.DataModel;
using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Core.V2.MTLS.Operation;
using IDS.Interface.Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace IDS.CMFImplantCreation.UnitTests
{
    [TestClass]
    public class StitchMtlsTests
    {
        private void VertexAdd(IVertex vertex, IVector3D vector)
        {
            vertex.X += vector.X;
            vertex.Y += vector.Y;
            vertex.Z += vector.Z;
        }

        private IMesh CreateOffsetMesh(IMeshWithNormal mesh, double offset)
        {
            var newMesh = new IDSMesh();
            for (var i = 0; i < mesh.Faces.Count; i++)
            {
                var face = mesh.Faces[i];
                var faceNormal = mesh.FacesNormal[i];
                faceNormal.Unitize();
                var vector = faceNormal.Mul(offset);

                var vertexA = new IDSVertex(mesh.Vertices[Convert.ToInt32(face.A)]);
                var vertexB = new IDSVertex(mesh.Vertices[Convert.ToInt32(face.B)]);
                var vertexC = new IDSVertex(mesh.Vertices[Convert.ToInt32(face.C)]);

                VertexAdd(vertexA, vector);
                VertexAdd(vertexB, vector);
                VertexAdd(vertexC, vector);

                var vertexIndex = Convert.ToUInt64(newMesh.Vertices.Count);
                newMesh.Vertices.Add(vertexA);
                newMesh.Vertices.Add(vertexB);
                newMesh.Vertices.Add(vertexC);

                newMesh.Faces.Add(new IDSFace(vertexIndex, vertexIndex + 1, vertexIndex + 2));
            }

            return newMesh;
        }

        [TestMethod]
        public void Stitch_Vertices_Test()
        {
            // Arrange
            var testConsole = new TestConsole();
            var oriCylinderMesh =
                Primitives.GenerateCylinder(testConsole, new IDSPoint3D(0, 0, 0), new IDSVector3D(0, 0, 1), 2, 2);

            var newCylinderMesh = new IDSMesh();
            foreach (var face in oriCylinderMesh.Faces)
            {
                var vertexA = new IDSVertex(oriCylinderMesh.Vertices[Convert.ToInt32(face.A)]);
                var vertexB = new IDSVertex(oriCylinderMesh.Vertices[Convert.ToInt32(face.B)]);
                var vertexC = new IDSVertex(oriCylinderMesh.Vertices[Convert.ToInt32(face.C)]);

                var i = Convert.ToUInt64(newCylinderMesh.Vertices.Count);
                newCylinderMesh.Vertices.Add(vertexA);
                newCylinderMesh.Vertices.Add(vertexB);
                newCylinderMesh.Vertices.Add(vertexC);

                newCylinderMesh.Faces.Add(new IDSFace(i, i + 1, i + 2));
            }
            // Act
            var stitchedCylinderMesh = StitchV2.PerformStitchVertices(testConsole, newCylinderMesh);
            // Assert
            Assert.AreEqual(oriCylinderMesh.Faces.Count * 3, newCylinderMesh.Vertices.Count, 
                $"The number of vertices for new cylinder mesh({newCylinderMesh.Vertices.Count}) should be 3x number of triangle of original mesh({oriCylinderMesh.Faces.Count})");
            Assert.AreEqual(oriCylinderMesh.Faces.Count, newCylinderMesh.Faces.Count,
                $"The number of faces for new cylinder mesh({newCylinderMesh.Faces.Count}) should be same as original mesh({oriCylinderMesh.Faces.Count})");

            Assert.AreEqual(oriCylinderMesh.Vertices.Count, stitchedCylinderMesh.Vertices.Count,
                $"The number of vertices for stitched cylinder mesh({stitchedCylinderMesh.Vertices.Count}) should be same as original mesh({oriCylinderMesh.Vertices.Count})");
            Assert.AreEqual(oriCylinderMesh.Faces.Count, stitchedCylinderMesh.Faces.Count,
                $"The number of faces for stitched cylinder mesh({stitchedCylinderMesh.Faces.Count}) should be same as  original mesh({oriCylinderMesh.Faces.Count})");
        }

        [TestMethod]
        public void Stitch_Vertices_With_Offset_And_With_Negative_Tolerant_Test()
        {
            // Arrange
            var testConsole = new TestConsole();
            var oriSphereMesh =
                Primitives.GenerateSphere(testConsole, new IDSPoint3D(0, 0, 0), 2);
            var oriSphereMeshWithNormal = IDSMeshWithNormal.GetMeshWithNormal(testConsole, oriSphereMesh);
            var newSphereMesh = CreateOffsetMesh(oriSphereMeshWithNormal, 0.000001);

            // Act
            var stitchedCylinderMesh = StitchV2.PerformStitchVertices(testConsole, newSphereMesh);
            // Assert;
            Assert.AreEqual(newSphereMesh.Faces.Count, stitchedCylinderMesh.Faces.Count,
                $"The number of faces for stitched cylinder mesh({newSphereMesh.Faces.Count}) should be same as original mesh({newSphereMesh.Faces.Count})");
            Assert.AreEqual(newSphereMesh.Vertices.Count, stitchedCylinderMesh.Vertices.Count,
                $"The number of vertices for stitched cylinder mesh({stitchedCylinderMesh.Vertices.Count}) should be same as original mesh({newSphereMesh.Vertices.Count})");
        }

        [TestMethod]
        public void Stitch_Vertices_With_Offset_And_With_Zero_Tolerant_Test()
        {
            // Arrange
            var testConsole = new TestConsole();
            var oriSphereMesh =
                Primitives.GenerateSphere(testConsole, new IDSPoint3D(0, 0, 0), 2);
            var oriSphereMeshWithNormal = IDSMeshWithNormal.GetMeshWithNormal(testConsole, oriSphereMesh);
            var newSphereMesh = CreateOffsetMesh(oriSphereMeshWithNormal, 0.000001);

            // Act
            var stitchedCylinderMesh = StitchV2.PerformStitchVertices(testConsole, newSphereMesh, 0);
            // Assert;
            Assert.AreEqual(newSphereMesh.Faces.Count, stitchedCylinderMesh.Faces.Count,
                $"The number of faces for stitched cylinder mesh({newSphereMesh.Faces.Count}) should be same as original mesh({newSphereMesh.Faces.Count})");
            Assert.AreEqual(newSphereMesh.Vertices.Count, stitchedCylinderMesh.Vertices.Count,
                $"The number of vertices for stitched cylinder mesh({stitchedCylinderMesh.Vertices.Count}) should be same as original mesh({newSphereMesh.Vertices.Count})");
        }

        [TestMethod]
        public void Stitch_Vertices_With_Offset_And_With_Tolerant_Test()
        {
            // Arrange
            var testConsole = new TestConsole();
            var oriSphereMesh =
                Primitives.GenerateSphere(testConsole, new IDSPoint3D(0, 0, 0), 2);
            var oriSphereMeshWithNormal = IDSMeshWithNormal.GetMeshWithNormal(testConsole, oriSphereMesh);
            const double tolerant = 0.000001;
            var newSphereMesh = CreateOffsetMesh(oriSphereMeshWithNormal, tolerant);

            // Act
            var stitchedCylinderMesh = StitchV2.PerformStitchVertices(testConsole, newSphereMesh, tolerant);
            // Assert;
            Assert.AreEqual(newSphereMesh.Faces.Count, stitchedCylinderMesh.Faces.Count,
                $"The number of faces for stitched cylinder mesh({newSphereMesh.Faces.Count}) should be same as original mesh({newSphereMesh.Faces.Count})");
            Assert.AreNotEqual(newSphereMesh.Vertices.Count, stitchedCylinderMesh.Vertices.Count,
                $"The number of vertices for stitched cylinder mesh({stitchedCylinderMesh.Vertices.Count}) shouldn't be same as original mesh({newSphereMesh.Vertices.Count})");
        }
    }
}
