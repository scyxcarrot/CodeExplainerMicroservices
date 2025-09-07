using IDS.CMF.TestLib.Components;
using IDS.Interface.Implant;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace IDS.Testing.UnitTests
{
    public static class ConnectionTestUtilities
    {
        public static void AssertConnectionAreEqual(IConnection expected, List<IDot> expectedDotList, IConnection actual, List<IDot> actualDotList)
        {
            Assert.AreEqual(expected.GetType(), actual.GetType(), "Connection type is not match");

            Assert.AreEqual(ConnectionComponent.GetDotIndex(expected.A, expectedDotList),
                ConnectionComponent.GetDotIndex(actual.A, actualDotList), "Connection dot A is not match");

            Assert.AreEqual(ConnectionComponent.GetDotIndex(expected.B, expectedDotList),
                ConnectionComponent.GetDotIndex(actual.B, actualDotList), "Connection dot B is not match");

            Assert.AreEqual(expected.Thickness, actual.Thickness, 0.001, "Connection thickness is not match");

            Assert.AreEqual(expected.Width, actual.Width, 0.001, "Connection width is not match");
        }
    }
}
