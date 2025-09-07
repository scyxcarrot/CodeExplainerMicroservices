using IDS.Interface.Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDS.Testing
{
    public static class PositionTestUtilities
    {
        public static void AssertIPoint3FAreEqual(IPoint3F expected, IPoint3F actual, string prefix)
        {
            Assert.AreEqual(expected.X, actual.X, 0.0001, $"{prefix} X not match");
            Assert.AreEqual(expected.Y, actual.Y, 0.0001, $"{prefix} Y not match");
            Assert.AreEqual(expected.Z, actual.Z, 0.0001, $"{prefix} Z not match");
        }

        public static void AssertIPoint3DAreEqual(IPoint3D expected, IPoint3D actual, string prefix)
        {
            Assert.AreEqual(expected.X, actual.X, 0.0001, $"{prefix} X not match");
            Assert.AreEqual(expected.Y, actual.Y, 0.0001, $"{prefix} Y not match");
            Assert.AreEqual(expected.Z, actual.Z, 0.0001, $"{prefix} Z not match");
        }

        public static void AssertIVertexAreEqual(IVertex expected, IVertex actual, string prefix)
        {
            Assert.AreEqual(expected.X, actual.X, 0.0001, $"{prefix} X not match");
            Assert.AreEqual(expected.Y, actual.Y, 0.0001, $"{prefix} Y not match");
            Assert.AreEqual(expected.Z, actual.Z, 0.0001, $"{prefix} Z not match");
        }
    }
}
