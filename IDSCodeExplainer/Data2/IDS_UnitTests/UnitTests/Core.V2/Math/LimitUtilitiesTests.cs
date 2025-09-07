using IDS.Core.V2.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace IDS.Testing.UnitTests.V2
{
    [TestClass]
    public class LimitUtilitiesTests
    {
        /// <summary>
        /// Test array to numeric conversion
        /// </summary>
        [TestMethod]
        public void ApplyLimitToDoubleArrayTest()
        {
            var doubleArray = new double[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var constraintedDoubleArray = LimitUtilities.ApplyLimitForDoubleArray(doubleArray, 2.5, 7.5);

            var expectedDoubleArray = new double[] { 2.5, 2.5, 2.5, 3, 4, 5, 6, 7, 7.5, 7.5, 7.5 };

            Assert.AreEqual(expectedDoubleArray.Count(), constraintedDoubleArray.Count());

            for (var i = 0; i < constraintedDoubleArray.Count(); i++)
            {
                Assert.AreEqual(expectedDoubleArray[i], constraintedDoubleArray[i]);
            }
        }

        /// <summary>
        /// Test bound correction happen if lowerBound == upperBound when minUserDefine != maxUserDefine
        /// </summary>
        [TestMethod]
        public void BoundCorrectionWhenMinNotEqualLowerBoundTest()
        {
            var lowerBound = 5.0;
            var upperBound = 5.0;
            LimitUtilities.BoundCorrection(3, 7, ref lowerBound, ref upperBound);

            Assert.AreNotEqual(lowerBound, 5.0, 0.1);
            Assert.AreEqual(lowerBound, 3.0, 0.1);

            Assert.AreEqual(upperBound, 5.0, 0.1);
        }

        /// <summary>
        /// Test bound correction happen if lowerBound == upperBound when minUserDefine != maxUserDefine and lowerBound == minUserDefine
        /// </summary>
        [TestMethod]
        public void BoundCorrectionWhenMinEqualLowerBoundTest()
        {
            var lowerBound = 5.0;
            var upperBound = 5.0;
            LimitUtilities.BoundCorrection(5, 7, ref lowerBound, ref upperBound);
            
            Assert.AreEqual(lowerBound, 5.0, 0.1);

            Assert.AreNotEqual(upperBound, 5.0, 0.1);
            Assert.AreEqual(upperBound, 7.0, 0.1);
        }
    }
}
