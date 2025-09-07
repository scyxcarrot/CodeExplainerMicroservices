using IDS.Core.V2.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class MathUtilitiesV2Tests
    {
        /// <summary>
        /// Get Nth Percentile in sequence array
        /// </summary>
        [TestMethod]
        public void GetNthPercentileTest()
        {
            var doubleArray = new double[100];
            for (var i = 0; i < 100; i++)
            {
                doubleArray[i] = Convert.ToDouble(i);
            }

            var value = MathUtilitiesV2.GetNthPercentile(97, doubleArray);
            // Accept tolerant
            Assert.IsTrue((value == doubleArray[96]) || 
                          (value == doubleArray[97]) || 
                          (value == doubleArray[98]));
        }

        [TestMethod]
        public void ToRadiansFromDegreesTest()
        {
            var expected = 0.087266462599716474;
            var value = MathUtilitiesV2.ToRadians(5);
            Assert.AreEqual(value, expected);
        }

        [TestMethod]
        public void ToDegreesFromRadiansTest()
        {
            var expected = 5;
            var value = MathUtilitiesV2.ToDegrees(0.087266462599716474);
            Assert.AreEqual(value, expected);
        }

        [TestMethod]
        public void FindRightTriangleATest()
        {
            var expected = 12.855752193730785;
            var value = MathUtilitiesV2.FindRightTriangleA(20, 40);
            Assert.AreEqual(value, expected);
        }

        [TestMethod]
        public void FindRightTriangleBTest()
        {
            var expected = 13.228756555322953;
            var value = MathUtilitiesV2.FindRightTriangleB(15, 20);
            Assert.AreEqual(value, expected);
        }

        [TestMethod]
        public void FindRightTriangleCTest()
        {
            var expected = 23.335857402906189;
            var value = MathUtilitiesV2.FindRightTriangleC(15, 40);
            Assert.AreEqual(value, expected);
        }
    }
}