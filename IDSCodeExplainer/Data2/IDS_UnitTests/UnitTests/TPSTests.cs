using IDS.Core.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class TPSTests
    {
        /// <summary>
        /// Test array to numeric conversion
        /// </summary>
        [TestMethod]
        public void TestTPS()
        {
            var stringArray = new[] { "0.2", "1.5689", "0.0", "-698.321" };

            var expectedDoubleArray = new[] { 0.2, 1.5689, 0.0, -698.321 };
            var actualDoubleArray = stringArray.ToNumeric<double>();
            for(var i = 0; i < expectedDoubleArray.Length; i++)
            {
                Assert.AreEqual(expectedDoubleArray[i], actualDoubleArray[i], $"Value {i:D}");
            }


            var expectedIntArray = new[] { 0, 2, 0, -698 };
            var actualIntArray = stringArray.ToNumeric<int>();
            for (var i = 0; i < expectedDoubleArray.Length; i++)
            {
                Assert.AreEqual(expectedIntArray[i], actualIntArray[i], $"Value {i:D}");
            }
        }
    }
}
