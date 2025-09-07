using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino;

namespace IDS.Testing
{
    public static class TestAssert
    {
        public static bool AreEqual(bool invokeAssertion, double expected, double actual, double delta, string message)
        {
            if (invokeAssertion)
            {
                Assert.AreEqual(expected, actual, delta, message);
            }
            var condition = (expected <= actual + delta && expected >= actual - delta); 
            if (!condition)
            {
                RhinoApp.WriteLine(message);
            }
            return condition;
        }

        public static bool IsTrue(bool invokeAssertion, bool condition, string message)
        {
            if (invokeAssertion)
            {
                Assert.IsTrue(condition, message);
            }
            if (!condition)
            {
                RhinoApp.WriteLine(message);
            }
            return condition;
        }
    }
}