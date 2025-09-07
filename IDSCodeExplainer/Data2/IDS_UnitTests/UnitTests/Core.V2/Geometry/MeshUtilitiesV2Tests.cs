using IDS.Core.V2.Geometries;
using IDS.Core.V2.Utilities;
using IDS.Interface.Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;

namespace IDS.Testing.UnitTests.V2
{
    [TestClass]
    public class MeshUtilitiesV2Tests
    {
        [TestMethod]
        public void CreateUnsharedVerticesMeshTest()
        {
            var mockMesh = new Mock<IMesh>();
            mockMesh.SetupGet(m => m.Vertices).Returns(
                new List<IVertex> 
            {
                new IDSVertex(1.0, 2.0, 3.0), 
                new IDSVertex(4.0, 5.0, 6.0), 
                new IDSVertex(7.0, 8.0, 9.0),
                new IDSVertex(10.0, 11.0, 12.0),
            });
            mockMesh.SetupGet(m => m.Faces).Returns(
                new List<IFace>()
                {
                    new IDSFace(0, 1, 2),
                    new IDSFace(2, 1, 3)
                });
            var sourceMesh = mockMesh.Object;

            var unsharedVerticesMesh = MeshUtilitiesV2.CreateUnsharedVerticesMesh(sourceMesh);

            Assert.AreEqual(6, unsharedVerticesMesh.Vertices.Count);
            Assert.AreEqual(2, unsharedVerticesMesh.Faces.Count);

            for (var row = 0; row < sourceMesh.Faces.Count; row++)
            {
                var actualVertexA = unsharedVerticesMesh.Vertices[Convert.ToInt32(unsharedVerticesMesh.Faces[row].A)];
                var expectedVertexA = sourceMesh.Vertices[Convert.ToInt32(sourceMesh.Faces[row].A)];
                
                Assert.AreEqual(expectedVertexA, actualVertexA);

                var actualVertexB = unsharedVerticesMesh.Vertices[Convert.ToInt32(unsharedVerticesMesh.Faces[row].B)];
                var expectedVertexB = sourceMesh.Vertices[Convert.ToInt32(sourceMesh.Faces[row].B)];

                Assert.AreEqual(expectedVertexB, actualVertexB);

                var actualVertexC = unsharedVerticesMesh.Vertices[Convert.ToInt32(unsharedVerticesMesh.Faces[row].C)];
                var expectedVertexC = sourceMesh.Vertices[Convert.ToInt32(sourceMesh.Faces[row].C)];

                Assert.AreEqual(expectedVertexC, actualVertexC);
            }
        }
    }
}
