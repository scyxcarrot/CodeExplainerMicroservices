using IDS.CMFImplantCreation.DataModel;
using IDS.Core.V2.MTLS.Operation;
using IDS.RhinoInterface.Converter;
using IDS.RhinoInterfaces.Converter;
using IDS.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Geometry;
using System.Linq;

namespace IDS.CMFImplantCreation.UnitTests
{
    [TestClass]
    public class NormalCompatibleTests
    {
        [TestMethod]
        public void Face_Normal_Compatible_Test()
        {
            // Arrange
            var rhinoBoxMesh = Mesh.CreateFromBox(new BoundingBox(
                new Point3d(-2, -2, -2),
                new Point3d(2, 2, 2)), 
                10, 10, 10);
            var idsBoxMesh = RhinoMeshConverter.ToIDSMesh(rhinoBoxMesh);
            // Act
            // mtls call triangle normal
            var triangleNormalsMtls = MeshNormal.PerformNormal(new TestConsole(), idsBoxMesh).TriangleNormals;
            rhinoBoxMesh.FaceNormals.ComputeFaceNormals();
            var faceNormals = rhinoBoxMesh.FaceNormals.Select(
                n => RhinoVector3dConverter.ToIVector3D(n)).ToList();
            // Assert
            Assert.AreEqual(faceNormals.Count, triangleNormalsMtls.Count);
            for (var i = 0; i < faceNormals.Count; i++)
            {
                var rhinoNormal = faceNormals[i];
                var mtlsNormal = triangleNormalsMtls[i];
                Assert.IsTrue(rhinoNormal.EpsilonEquals(mtlsNormal, 0.001), 
                    $"Rhino FaceNormal: {rhinoNormal}, MTLS TriangleNormal: {mtlsNormal}");
            }
        }

        [TestMethod]
        public void Vertex_Normal_Compatible_Test()
        {
            // Arrange
            var rhinoBoxMesh = Mesh.CreateFromBox(new BoundingBox(
                    new Point3d(-2, -2, -2),
                    new Point3d(2, 2, 2)),
                10, 10, 10);
            var idsBoxMesh = RhinoMeshConverter.ToIDSMesh(rhinoBoxMesh);
            // Act
            var vertexNormalsMtls = MeshNormal.PerformNormal(new TestConsole(), idsBoxMesh).VertexNormals;
            rhinoBoxMesh.Normals.ComputeNormals();
            var vertexNormalsRhino = rhinoBoxMesh.Normals.Select(
                n => RhinoVector3dConverter.ToIVector3D(n)).ToList();
            // Assert
            Assert.AreEqual(vertexNormalsMtls.Count, vertexNormalsRhino.Count);
            for (var i = 0; i < vertexNormalsMtls.Count; i++)
            {
                var rhinoNormal = vertexNormalsRhino[i];
                var mtlsNormal = vertexNormalsMtls[i];
                Assert.IsTrue(rhinoNormal.EpsilonEquals(mtlsNormal, 0.001),
                    $"Rhino Vertex Normal: {rhinoNormal}, MTLS Vertex Normal: {mtlsNormal}");
            }
        }

        [TestMethod]
        public void IDS_Mesh_Normal_Compatible_With_Rhino_Mesh_Normal_Test()
        {
            // Arrange
            var rhinoBoxMesh = Mesh.CreateFromBox(new BoundingBox(
                    new Point3d(-2, -2, -2),
                    new Point3d(2, 2, 2)),
                10, 10, 10);
            var idsBoxMesh = RhinoMeshConverter.ToIDSMesh(rhinoBoxMesh);
            var console = new TestConsole();

            // Act
            var idsBoxMeshWithNormal = IDSMeshWithNormal.GetMeshWithNormal(console, idsBoxMesh);
            var vertexNormals = idsBoxMeshWithNormal.VerticesNormal;
            var triangleNormals = idsBoxMeshWithNormal.FacesNormal;

            rhinoBoxMesh.Normals.ComputeNormals();
            var vertexNormalsRhino = rhinoBoxMesh.Normals.Select(
                n => RhinoVector3dConverter.ToIVector3D(n)).ToList();

            rhinoBoxMesh.FaceNormals.ComputeFaceNormals();
            var faceNormals = rhinoBoxMesh.FaceNormals.Select(
                n => RhinoVector3dConverter.ToIVector3D(n)).ToList();

            // Assert
            Assert.AreEqual(faceNormals.Count, triangleNormals.Count);
            for (var i = 0; i < faceNormals.Count; i++)
            {
                var rhinoNormal = faceNormals[i];
                var normal = triangleNormals[i];
                Assert.IsTrue(rhinoNormal.EpsilonEquals(normal, 0.001),
                    $"Rhino FaceNormal: {rhinoNormal}, IDS TriangleNormal: {normal}");
            }

            Assert.AreEqual(vertexNormals.Count, vertexNormalsRhino.Count);
            for (var i = 0; i < vertexNormals.Count; i++)
            {
                var rhinoNormal = vertexNormalsRhino[i];
                var normal = vertexNormals[i];
                Assert.IsTrue(rhinoNormal.EpsilonEquals(normal, 0.001),
                    $"Rhino Vertex Normal: {rhinoNormal}, IDS Vertex Normal: {normal}");
            }
        }
    }
}
