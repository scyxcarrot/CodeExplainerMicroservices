using IDS.Interface.Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDS.Testing
{
    public static class MeshTestUtilities
    {
        public static void AssertMeshAreEqual(IMesh expected, IMesh actual)
        {
            Assert.AreEqual(expected.Vertices.Count, actual.Vertices.Count, "Vertices count not match");
            Assert.AreEqual(expected.Faces.Count, actual.Faces.Count, "Faces count not match");

            for (var i = 0; i < expected.Vertices.Count; i++)
            {
                PositionTestUtilities.AssertIVertexAreEqual(expected.Vertices[i], actual.Vertices[i], "Vertex");
            }

            for (var i = 0; i < expected.Faces.Count; i++)
            {
                Assert.AreEqual(expected.Faces[i].A, actual.Faces[i].A, "Face A not match");
                Assert.AreEqual(expected.Faces[i].B, actual.Faces[i].B, "Face B not match");
                Assert.AreEqual(expected.Faces[i].C, actual.Faces[i].C, "Face C not match");
            }
        }
    }
}
